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
    public class FolderGroupConfig
    {
        public string? Name;
        public ConfigFile? mConfigFile;
        public List<DynamicGroupItem> GroupItemList = new List<DynamicGroupItem>();
        public int CollumnCount = -1;
        public Color CatagoryBGColor = Color.FromArgb(255, 31, 31, 31);
        public bool allowOpenAll = false;

        public string ConfigurationPath = "";

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
            ConfigurationPath = Path.GetFullPath(path) + @"\FolderGroupConfig.ini";

            if (System.IO.File.Exists(ConfigurationPath))
            {
                ReadConfigData(ConfigurationPath);
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

        void WriteConfigData(string path)
        {
            if (mConfigFile == null)
                mConfigFile = new ConfigFile();

            mConfigFile.WriteProperty("Name", GetName());
            mConfigFile.WriteProperty("CollumnCount", CollumnCount.ToString());
            mConfigFile.WriteProperty("CatagoryBGColor", ColorToUnsignedInt(CatagoryBGColor).ToString("X4"));
            mConfigFile.WriteProperty("allowOpenAll", allowOpenAll.ToString());
            mConfigFile.WriteProperty("allowOpenAll", allowOpenAll.ToString());

            for (int i = 0; i < GroupItemList.Count; i++)
            {
                string shortcutKey = "Shortcut" + i.ToString("D3");
                ConfigFile.GroupItemConfig itemConfig = mConfigFile.GetGroupItem(shortcutKey);
                GroupItemList[i].OnWrite(itemConfig);
            }

            mConfigFile.SaveConfigFile(path);
        }

        private uint ColorToUnsignedInt(System.Windows.Media.Color color)
        {
            uint parsedColor = (uint)(color.A << 24) + (uint)(color.R << 16) + (uint)(color.G << 8) + color.B;
            return parsedColor;
        }

        public string GetName() { return Name ?? ""; }
        public void SetName(string name) { Name = name; }

        public void CreateConfig(BitmapSource groupImage)
        {
            string path = @"config\" + this.Name;
            System.IO.Directory.CreateDirectory(@path);

            SaveIcons();

            WriteConfigData(path);

            ImageFunctions.SaveBitmapSourceToFile(ImageFunctions.ResizeImage(groupImage, 256.0, 256.0), path + @"\GroupImage.png");
            createMultiIcon(groupImage, path + @"\GroupIcon.ico");

            NativeMethods.InstallShortcut(path, $"TaskbarGroupEx.Menu.{this.Name}", "GroupIcon.ico", this.GetName());

            string FolderGroupLnkPath = Path.GetFullPath(@"Shortcuts\" + Regex.Replace(this.GetName(), @"(_)+", " ") + ".lnk");
            System.IO.File.Move($"{path}\\{this.Name}.lnk", FolderGroupLnkPath); // Move .lnk to correct directory
        }

        public void OnFinishConversion(string path)
        {
            File.Delete(path);
            string? ConfigDirectory = Path.GetDirectoryName(path);
            if (ConfigDirectory != null)
            {
                WriteConfigData(ConfigDirectory);
            }
        }

        private static void createMultiIcon(BitmapSource iconImage, string filePath)
        {
            double mipSize = 256.0;
            Stack<BitmapSource> iconList = new Stack<BitmapSource>();

            while (mipSize > 8.0) /* Minimum Icon Size */
            {
                iconList.Push(ImageFunctions.ResizeImage(iconImage, mipSize, mipSize) as BitmapSource);
                mipSize = Math.Round(mipSize / 2.0);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                IconFactory.SavePngsAsIcon(iconList.ToArray(), stream);
            }
        }

        public BitmapImage LoadIconImage() // Needed to access img without occupying read/write
        {
            BitmapImage? bitmapImage = FileHandler.OpenPNG(@"config\" + Name + @"\GroupImage.png");
            return bitmapImage != null ? bitmapImage : (BitmapImage)ImageFunctions.GetErrorImageSource();
        }

        public string GetRelativeDir(string dir)
        {
            return Path.GetRelativePath(MainPath.GetConfigPath(), dir);
        }

        public void SaveIcons()
        {
            MainPath.CreateNewFolder(@MainPath.Config + this.Name + "\\Icons\\");

            for (int i = 0; i < GroupItemList.Count; i++)
            {
                GroupItemList[i].SetIconPath(this.Name);
                GroupItemList[i].WriteIcon();
            }
        }
        //
        // END OF CLASS
        //
    }

    public class ConfigFile
    {
        public class GroupItemConfig
        {
            public string ID;
            public List<KeyValuePair<string, string>> mProperties;

            public GroupItemConfig(string id) { 
                ID = id;
                mProperties = new List<KeyValuePair<string, string>>(); 
            }

            public void GetProperty(string PropID, ref string prop)
            {
                for (int i = 0; i < mProperties.Count; i++)
                {
                    if (mProperties[i].Key == PropID)
                        prop = mProperties[i].Value;
                }
            }

            public void WriteProperty(string PropID, string? prop)
            {
                if (prop != null && prop != "")
                {
                    for (int i = 0; i < mProperties.Count; i++)
                    {
                        if (mProperties[i].Key == PropID)
                        {
                            prop = mProperties[i].Value;
                            return;
                        }
                    }

                    mProperties.Add(new KeyValuePair<string, string>(PropID, prop));
                }
            }
        }

        public List<GroupItemConfig> mGroupItems;
        public List<KeyValuePair<string, string>> mProperties;

        public ConfigFile()
        {
            mGroupItems = new List<GroupItemConfig>();
            mProperties = new List<KeyValuePair<string, string>>();
        }

        public ConfigFile(string Filename)
        {
            mGroupItems = new List<GroupItemConfig>();
            mProperties = new List<KeyValuePair<string, string>>();
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

        public void SaveConfigFile(string ConfigDir)
        {
            string configFilePath = ConfigDir + @"\FolderGroupConfig.ini";
            FileHandler configFile = new FileHandler(configFilePath);
            configFile.Write(mProperties);
            foreach(GroupItemConfig groupItem in mGroupItems)
            {
                configFile.Write(groupItem.mProperties, groupItem.ID);
            }
            configFile.Close();
        }

        public GroupItemConfig GetGroupItem(string GroupID)
        {
            for(int i = 0; i < mGroupItems.Count; i++)
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

        private string? GetProperty(string PropID)
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

        public void GetProperty(string PropID, ref string? prop)
        {
            string? value = GetProperty(PropID);
            if(value != null)
            {
                prop = value;
            }
            for (int i = 0; i < mProperties.Count; i++)
            {
                if (mProperties[i].Key == PropID)
                    prop = mProperties[i].Value;
            }
        }

        public void GetProperty(string PropID, ref int prop)
        {
            string? value = GetProperty(PropID);
            if (value != null)
            {
                int _value;
                if (Int32.TryParse(value, out _value))
                    prop = _value;
            }
        }

        public void GetProperty(string PropID, ref bool prop)
        {
            string? value = GetProperty(PropID);
            if (value != null)
            {
                bool _value;
                if (Boolean.TryParse(value, out _value))
                    prop = _value;
            }
        }

        public void GetProperty(string PropID, ref Color prop)
        {
            string? value = GetProperty(PropID);
            if (value != null)
            {
                uint _value;
                if (UInt32.TryParse(value, out _value))
                {
                    prop = System.Windows.Media.Color.FromArgb((byte)(_value >> 24), (byte)(_value << 8 >> 24), (byte)(_value << 16 >> 24), (byte)(_value << 24 >> 24));
                }
            }
        }

        public void WriteProperty(string PropID, string? prop)
        {
            if (prop != null && prop != "")
            {
                for (int i = 0; i < mProperties.Count; i++)
                {
                    if (mProperties[i].Key == PropID)
                    {
                        prop = mProperties[i].Value;
                        return;
                    }
                }

                mProperties.Add(new KeyValuePair<string, string>(PropID, prop));
            }
        }
    }
}
