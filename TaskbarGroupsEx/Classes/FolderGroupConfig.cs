using Shell32;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using TaskbarGroupsEx.GroupItems;
using TaskbarGroupsEx.Handlers;

namespace TaskbarGroupsEx.Classes
{
    #region ConfigFile
    public class ConfigFile
    {
        public class ConfigPropertyContainer
        {
            public List<KeyValuePair<string, string>> mProperties;

            public ConfigPropertyContainer()
            {
                mProperties = new List<KeyValuePair<string, string>>();
            }

            public string? GetProperty(string PropID)
            {
                for (int i = 0; i < mProperties.Count; i++)
                {
                    if (mProperties[i].Key == PropID)
                    {
                        return mProperties[i].Value;
                    }
                }
                return null;
            }

            private dynamic? GetDynamicProperty<T>(string PropID, T? prop)
            {
                string? value = GetProperty(PropID);
                if (value == null)
                    return null;

                try
                {
                    switch (typeof(T).Name)
                    {
                        case "String":
                            return value;
                        case "Color":
                            uint _colorVal = Convert.ToUInt32(value, 16);
                            return System.Windows.Media.Color.FromArgb((byte)(_colorVal >> 24), (byte)(_colorVal << 8 >> 24), (byte)(_colorVal << 16 >> 24), (byte)(_colorVal << 24 >> 24));

                        case "Int32":
                            return Int32.Parse(value);

                        case "Boolean":
                            return Boolean.Parse(value);
                    }
                }
                catch { }

                return null;
            }

            public void GetProperty<T>(string PropID, ref T prop)
            {
                dynamic? result = GetDynamicProperty(PropID, prop);
                if (result != null)
                    prop = result;
            }

            public void WriteProperty(string PropID, string? prop)
            {
                if (prop != null && prop != "")
                {
                    for (int i = 0; i < mProperties.Count; i++)
                    {
                        if (mProperties[i].Key == PropID)
                        {
                            mProperties[i] = new KeyValuePair<string, string>(PropID, prop);
                            return;
                        }
                    }

                    mProperties.Add(new KeyValuePair<string, string>(PropID, prop));
                }
            }
        }

        public class GroupItemConfig
        {
            public string ID;
            public ConfigPropertyContainer mProperties;

            public GroupItemConfig(string id)
            {
                ID = id;
                mProperties = new ConfigPropertyContainer();
            }

            public void GetProperty(string PropID, ref string? prop)
            {
                prop = mProperties.GetProperty(PropID);
            }

            public void WriteProperty(string PropID, string? prop)
            {
                mProperties.WriteProperty(PropID, prop);
            }
        }

        public List<GroupItemConfig> mGroupItems;
        public ConfigPropertyContainer mProperties;

        public ConfigFile()
        {
            mGroupItems = new List<GroupItemConfig>();
            mProperties = new ConfigPropertyContainer();
        }

        public ConfigFile(string Filename)
        {
            mGroupItems = new List<GroupItemConfig>();
            mProperties = new ConfigPropertyContainer();
            ReadConfigData(Filename);
        }

        void ReadConfigData(string fileName)
        {
            foreach (string line in File.ReadLines(fileName))
            {
                var lineData = line.Split('=');
                if (lineData[0].Contains('.'))
                {
                    var ItemProp = lineData[0].Split('.');
                    GroupItemConfig itemConfig = GetGroupItem(ItemProp[0]);
                    itemConfig.WriteProperty(ItemProp[1], lineData[1]);
                }
                else
                {
                    WriteProperty(lineData[0], lineData[1]);
                }
            }
        }

        public void SaveConfigFile(string configFilePath)
        {
            FileHandler configFile = new FileHandler(configFilePath);
            configFile.Write(mProperties);
            foreach (GroupItemConfig groupItem in mGroupItems)
            {
                configFile.Write(groupItem.mProperties, groupItem.ID);
            }
            configFile.Close();
        }

        public GroupItemConfig GetGroupItem(string GroupID)
        {
            for (int i = 0; i < mGroupItems.Count; i++)
            {
                if (mGroupItems[i].ID == GroupID)
                    return mGroupItems[i];
            }

            return AddGroupItem(GroupID);
        }

        GroupItemConfig AddGroupItem(string GroupID)
        {
            GroupItemConfig groupItem = new GroupItemConfig(GroupID);
            mGroupItems.Add(groupItem);
            return groupItem;
        }

        public void GetProperty<T>(string PropID, ref T prop)
        {
            mProperties.GetProperty(PropID, ref prop);
        }

        public void WriteProperty(string PropID, string? prop)
        {
            mProperties.WriteProperty(PropID, prop);
        }
    }
    #endregion

    public class FolderGroupConfig
    {
        public string Name = "";
        public ConfigFile? mConfigFile;
        public List<DynamicGroupItem> GroupItemList = new List<DynamicGroupItem>();
        public int CollumnCount = -1;
        public Color CatagoryBGColor = Color.FromArgb(255, 31, 31, 31);
        public bool allowOpenAll = false;

        public string? ConfigurationPath = null;
        public string configurationFilePath = "";
        public string ConfigurationFilePath
        {
            get { return this.configurationFilePath; }
            set { 
                this.configurationFilePath = value;
                ConfigurationPath = Path.GetDirectoryName(value); 
            }
        }

        public static FolderGroupConfig ParseConfiguration(string path)
        {
            string legacyConfigFilePath = Path.GetFullPath(path) + @"\ObjectData.xml";
            if (System.IO.File.Exists(legacyConfigFilePath))
            {
                return LegacyCategoryFormat.ConvertToNewFormat(legacyConfigFilePath);
            }

            return new FolderGroupConfig(path);
        }

        public FolderGroupConfig() { }

        public FolderGroupConfig(string path)
        {
            ConfigurationFilePath = Path.GetFullPath(path) + @"\FolderGroupConfig.ini";

            if (System.IO.File.Exists(ConfigurationFilePath))
            {
                ReadConfigData(ConfigurationFilePath);
            }
        }

        void ReadConfigData(string fileName)
        {
            mConfigFile = new ConfigFile(fileName);
            
            mConfigFile.GetProperty("Name", ref Name);
            mConfigFile.GetProperty("CollumnCount", ref CollumnCount);
            if (CollumnCount == -1)
                throw new Exception("Error While Reading 'CollumnCount' from Configuration File.");

            mConfigFile.GetProperty("CatagoryBGColor", ref CatagoryBGColor);
            mConfigFile.GetProperty("allowOpenAll", ref allowOpenAll);

            foreach (ConfigFile.GroupItemConfig itemConfig in mConfigFile.mGroupItems)
            {
                string ItemType = "";
                itemConfig.GetProperty("Type", ref ItemType);
                switch (ItemType)
                {
                    case "Application": GroupItemList.Add(new ApplicationGroupItem(itemConfig)); break;
                    case "UWP": GroupItemList.Add(new UWPGroupItem(itemConfig)); break;
                    case "URI": GroupItemList.Add(new URIGroupItem(itemConfig)); break;
                    case "URL": GroupItemList.Add(new URLGroupItem(itemConfig)); break;
                    case "File": GroupItemList.Add(new FileGroupItem(itemConfig)); break;
                    case "Folder": GroupItemList.Add(new FolderGroupItem(itemConfig)); break;
                }
            }
        }

        void WriteConfigData()
        {
            mConfigFile = new ConfigFile();

            mConfigFile.WriteProperty("Name", GetName());
            mConfigFile.WriteProperty("CollumnCount", CollumnCount.ToString());
            mConfigFile.WriteProperty("CatagoryBGColor", ColorToUnsignedInt(CatagoryBGColor).ToString("X4"));
            mConfigFile.WriteProperty("allowOpenAll", allowOpenAll.ToString());

            for (int i = 0; i < GroupItemList.Count; i++)
            {
                string shortcutKey = "Shortcut" + i.ToString("D3");
                ConfigFile.GroupItemConfig itemConfig = mConfigFile.GetGroupItem(shortcutKey);
                GroupItemList[i].OnWrite(itemConfig);
            }

            mConfigFile.SaveConfigFile(ConfigurationFilePath);
        }

        private uint ColorToUnsignedInt(System.Windows.Media.Color color)
        {
            uint parsedColor = (uint)(color.A << 24) + (uint)(color.R << 16) + (uint)(color.G << 8) + color.B;
            return parsedColor;
        }

        public string GetName() { return Name; }
        public void SetName(string name) { Name = name; }

        public void CreateConfig(BitmapSource groupImage)
        {
            MainPath.CreateNewFolder(ConfigurationPath);
            SaveIcons();
            SaveGroupBanner(groupImage);
            WriteConfigData();
            CreateTaskbarGroupShortcut();
        }

        public void OnFinishConversion(string? path)
        {
            if (path != null)
            {
                ConfigurationFilePath = $"{(Path.GetFullPath(path))}\\FolderGroupConfig.ini";
                CreateConfig(LoadIconImage());
            }
        }

        void CreateTaskbarGroupShortcut()
        {
            if (ConfigurationPath != null)
            {
                NativeMethods.InstallShortcut(ConfigurationPath, $"TaskbarGroupEx.Menu.{this.Name}", "GroupIcon.ico", this.GetName());

                string FolderGroupLnkPath = Path.GetFullPath(@"Shortcuts\" + Regex.Replace(this.GetName(), @"(_)+", " ") + ".lnk");
                if (File.Exists(FolderGroupLnkPath))
                    File.Delete(FolderGroupLnkPath);

                System.IO.File.Move($"{ConfigurationPath}\\{this.Name}.lnk", FolderGroupLnkPath); // Move .lnk to correct directory
            }
        }

        private static void createMultiIcon(BitmapSource iconImage, string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                IconFactory.SavePngAsIcon(iconImage, stream);
            }
        }

        public BitmapImage LoadIconImage()
        {
            BitmapImage? bitmapImage = FileHandler.OpenPNG($"{ConfigurationPath}\\GroupImage.png");
            return bitmapImage != null ? bitmapImage : ImageFunctions.GetErrorImage();
        }

        public string GetRelativeDir(string dir)
        {
            return Path.GetRelativePath(MainPath.GetConfigPath(), dir);
        }

        private void SaveGroupBanner(BitmapSource groupImage)
        {
            ImageFunctions.SaveBitmapSourceToFile(ImageFunctions.ResizeImage(groupImage, 256.0, 256.0), ConfigurationPath + @"\GroupImage.png");
            createMultiIcon(groupImage, ConfigurationPath + @"\GroupIcon.ico");
        }

        public void SaveIcons()
        {
            MainPath.CreateNewFolder($"{ConfigurationPath}\\Icons\\");

            for (int i = 0; i < GroupItemList.Count; i++)
            {
                GroupItemList[i].SetIconPath(this.Name!);
                GroupItemList[i].WriteIcon();
            }
        }
    }
}
