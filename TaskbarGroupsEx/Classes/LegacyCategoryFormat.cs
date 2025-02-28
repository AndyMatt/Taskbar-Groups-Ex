using System.IO;
using System.Windows.Media;

namespace TaskbarGroupsEx.Classes
{
    public class LegacyCategoryFormat
    {
        public class Category
        {
            public string Name = "";
            public string ColorString = System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(31, 31, 31));
            public bool allowOpenAll = false;
            public List<ProgramShortcut> ShortcutList = new List<ProgramShortcut>();
            public int Width = 5; // not used aon
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
                        newFormatCategory.ShortcutList = oldConfig.ShortcutList;
                        newFormatCategory.CollumnCount = oldConfig.Width;
                        newFormatCategory.allowOpenAll = oldConfig.allowOpenAll;  
                        newFormatCategory.CatagoryBGColor = ConvertColorStringToBGColor(oldConfig);
                        bSuccess = true;
                    } 
                }
            }

            //Replace old version config if conversion was successful
            if (bSuccess)
            {
                newFormatCategory.OnFinishConversion(legacyConfigFile);
            }
            
            return newFormatCategory;

        }

        private static Color ConvertColorStringToBGColor(LegacyCategoryFormat.Category oldConfig)
        {
            System.Drawing.Color _color = ImageFunctions.FromString(oldConfig.ColorString);
            Byte _opacity = Convert.ToByte(oldConfig.Opacity * 255.0 / 100.0);
            return System.Windows.Media.Color.FromArgb(_opacity, _color.R, _color.G, _color.B);
        }
    }
}
