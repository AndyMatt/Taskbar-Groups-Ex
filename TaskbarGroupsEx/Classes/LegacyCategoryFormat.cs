using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using TaskbarGroupsEx.GroupItems;
using TaskbarGroupsEx.Handlers;

namespace TaskbarGroupsEx.Classes
{
    public class LegacyCategoryFormat
    {
        public class ProgramShortcut
        {
            public string FilePath { get; set; } = "";
            public bool isWindowsApp { get; set; }
            public string name { get; set; } = "";
            public string Arguments = "";
            public string WorkingDirectory = "";
        }
        public class Category
        {
            public string Name = "";
            public string ColorString = System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(31, 31, 31));
            public bool allowOpenAll = false;
            public List<ProgramShortcut> ShortcutList = new List<ProgramShortcut>();
            public int Width = 5;
            public double Opacity = 10;
        }

        public LegacyCategoryFormat() { } // needed for XML serialization

        public static Classes.FolderGroupConfig ConvertToNewFormat(string legacyConfigFile)
        {
            bool bSuccess = false;
            Classes.FolderGroupConfig newFormatCategory = new Classes.FolderGroupConfig();

            System.Xml.Serialization.XmlSerializer? reader =
                new System.Xml.Serialization.XmlSerializer(typeof(LegacyCategoryFormat.Category));

            if (reader != null)
            {
                using (StreamReader file = new StreamReader(legacyConfigFile))
                {
                    LegacyCategoryFormat.Category? oldConfig = reader.Deserialize(file) as LegacyCategoryFormat.Category;
                    if (oldConfig != null)
                    {
                        newFormatCategory.Name = oldConfig.Name;
                        newFormatCategory.CollumnCount = oldConfig.Width;
                        newFormatCategory.allowOpenAll = oldConfig.allowOpenAll;  
                        newFormatCategory.CatagoryBGColor = ConvertColorStringToBGColor(oldConfig);
                        newFormatCategory.GroupItemList = ParseShortcuts(oldConfig);                 
                        bSuccess = true;
                    } 
                }
            }

            //Replace old version config if conversion was successful
            if (bSuccess)
            {
                newFormatCategory.OnFinishConversion(Path.GetDirectoryName(legacyConfigFile));
            }
            
            return newFormatCategory;

        }

        private static List<DynamicGroupItem> ParseShortcuts(LegacyCategoryFormat.Category? oldConfig)
        {
            List<DynamicGroupItem> GroupItems = new List<DynamicGroupItem>();

            if (oldConfig == null)
                return GroupItems;

            foreach (ProgramShortcut shortcut in oldConfig.ShortcutList)
            {
                DynamicGroupItem? groupItem = null;
                if (shortcut.isWindowsApp && IsValidUWP(shortcut.FilePath))
                { 
                    groupItem = new UWPGroupItem(shortcut.FilePath);        
                }
                else if (IsLnkFile(shortcut.FilePath))
                {
                    groupItem = lnkFileHandler.GetGroupItem(shortcut.FilePath);
                }
                else if (IsExe(shortcut.FilePath))
                {
                    groupItem = new ApplicationGroupItem(shortcut.FilePath, shortcut.Arguments, shortcut.WorkingDirectory);
                }

                if (groupItem != null)
                {
                    ParseProperties(shortcut, groupItem);
                    GroupItems.Add(groupItem);
                }
            }
            return GroupItems;
        }

        private static void ParseProperties(ProgramShortcut shortcut, DynamicGroupItem? groupItem)
        {
            if (groupItem == null)
                return;

            //Name
            groupItem.mName = shortcut.name;

            //Icon
            string IconPath = MainPath.GetConfigPath() + shortcut.name + "\\Icons\\" + Path.GetFileNameWithoutExtension(shortcut.FilePath);
            if (!File.Exists(IconPath + ".png"))
            {
                if (File.Exists(IconPath + "_FolderObjTSKGRoup.png"))
                {
                    IconPath += "_FolderObjTSKGRoup";
                }
            }

            IconPath += ".png";

            if (File.Exists(IconPath))
            {
                groupItem.mIconPath = IconPath;
                groupItem.LoadIconFromFile(IconPath);
            }
        }

        private static bool IsValidUWP(string appID)
        {
            if (ShellApplicationHelper.IsAppUserModelID(appID))
            {
                dynamic? shellItem = ShellApplicationHelper.GetShellItem(appID);
                return ShellApplicationHelper.IsUWP(shellItem);
            }
            return false;
        }

        private static bool IsLnkFile(string filePath)
        {
            if (Path.GetExtension(filePath) == ".lnk")
            {
                return lnkFileHandler.Islnk(filePath);
            }
            return false;
        }

        private static bool IsExe(string filePath)
        {
            if(!File.Exists(filePath))
                return false;

            return Path.GetExtension(filePath) == ".exe";
        }       

        private static Color ConvertColorStringToBGColor(LegacyCategoryFormat.Category oldConfig)
        {
            System.Drawing.Color _color = ImageFunctions.FromString(oldConfig.ColorString);
            Byte _opacity = Convert.ToByte(oldConfig.Opacity * 255.0 / 100.0);
            return System.Windows.Media.Color.FromArgb(_opacity, _color.R, _color.G, _color.B);
        }
    }
}
