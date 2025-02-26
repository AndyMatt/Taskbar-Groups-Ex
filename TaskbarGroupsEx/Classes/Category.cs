using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace TaskbarGroupsEx.Classes
{
    public class Category
    {
        public string? Name;
        public List<ProgramShortcut> ShortcutList = new List<ProgramShortcut>();
        public int CollumnCount;
        public System.Windows.Media.Color CatagoryBGColor = System.Windows.Media.Color.FromArgb(255, 31, 31, 31);
        public bool allowOpenAll = false;

        public string ConfigurationPath = "";
        Regex specialCharRegex = new Regex("[*'\",_&#^@]");

        public static Category ParseConfiguration(string path)
        {
            string legacyConfigFilePath = Path.GetFullPath(path) + @"\ObjectData.xml";
            if (System.IO.File.Exists(legacyConfigFilePath))
            {
                return LegacyCategoryFormat.ConvertToNewFormat(legacyConfigFilePath);
            }

            return new Category(path);
        }

        public Category(){}

        public Category(string path)
        {
            ConfigurationPath = Path.GetFullPath(path) + @"\FolderGroupConfig.ini";

            if (System.IO.File.Exists(ConfigurationPath))
            {
                ReadConfigData(ConfigurationPath);
            }
        }

        void ReadConfigData(string fileName)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            foreach (string line in File.ReadLines(fileName))
            {
                var lineData = line.Split('=');
                data.Add(lineData[0], lineData[1]);
            }

            //Name
            SetName(data["Name"]);

            //Collumn Count
            if(!Int32.TryParse(data["CollumnCount"], out CollumnCount)) { throw new Exception("Error While Reading 'CollumnCount' from Configuration File."); }

            //Background Color
            uint bgColor = uint.Parse(data["CatagoryBGColor"], NumberStyles.HexNumber);
            CatagoryBGColor = ColorFromUnsignedInt(bgColor);

            //AllowOpenAll
            if (!Boolean.TryParse(data["allowOpenAll"], out allowOpenAll)) { throw new Exception("Error While Reading 'allowOpenAll' from Configuration File."); }

            //Shortcut Count
            int ShortcutCount = 0;
            if (!Int32.TryParse(data["ShortcutCount"], out ShortcutCount)) { throw new Exception("Error While Reading 'ShortcutCount' from Configuration File."); }

            for (int i = 0; i < ShortcutCount; i++)
            {
                string shortcutKey = "Shortcut" + i.ToString("D3");
                ProgramShortcut newShortcut = new ProgramShortcut();
                newShortcut.name = data[shortcutKey + ".Name"];
                newShortcut.Arguments = data[shortcutKey + ".Arguments"];
                newShortcut.WorkingDirectory = data[shortcutKey + ".WorkingDirectory"];
                newShortcut.FilePath = data[shortcutKey + ".FilePath"];

                bool isWindowsApp = false;
                if (Boolean.TryParse(data[shortcutKey + ".isWindowsApp"], out isWindowsApp))
                {
                    newShortcut.isWindowsApp = isWindowsApp;
                }
                else
                { 
                    throw new Exception($"Error While Reading 'isWindowsApp' in {shortcutKey} from Configuration File."); 
                }
                ShortcutList.Add(newShortcut);
            }

        }

        void WriteConfigData(string path)
        {
            string configFilePath = path + @"\FolderGroupConfig.ini";
            Dictionary<string, string> data = new Dictionary<string, string>();
            data["Name"] = GetName();
            data["CollumnCount"] = CollumnCount.ToString();
            data["CatagoryBGColor"] = ColorToUnsignedInt(CatagoryBGColor).ToString("X4");
            data["allowOpenAll"] = allowOpenAll.ToString();

            data["ShortcutCount"] = ShortcutList.Count.ToString();
            for(int i = 0; i < ShortcutList.Count; i++)
            {
                data["Shortcut" + i.ToString("D3")+ ".Name"] = ShortcutList[i].name;
                data["Shortcut" + i.ToString("D3")+ ".Arguments"] = ShortcutList[i].Arguments;
                data["Shortcut" + i.ToString("D3")+ ".WorkingDirectory"] = ShortcutList[i].WorkingDirectory;
                data["Shortcut" + i.ToString("D3")+ ".FilePath"] = ShortcutList[i].FilePath;
                data["Shortcut" + i.ToString("D3")+ ".isWindowsApp"] = ShortcutList[i].isWindowsApp.ToString();
            }

            File.WriteAllLines(configFilePath, data.Select(z => $"{z.Key}={z.Value}"));
        }

        private uint ColorToUnsignedInt(System.Windows.Media.Color color)
        {
            uint parsedColor = (uint)(color.A << 24) + (uint)(color.R << 16) + (uint)(color.G << 8) + color.B;
            return parsedColor;
        }

        private System.Windows.Media.Color ColorFromUnsignedInt(uint color)
        {
            return System.Windows.Media.Color.FromArgb((byte)(color >> 24), (byte)(color << 8 >> 24), (byte)(color << 16 >> 24), (byte)(color << 24 >> 24));
        }

        public string GetName() { return Name ?? ""; }
        public void SetName(string name) { Name = name; }

        public void CreateConfig(BitmapSource groupImage)
        {
            string path = @"config\" + this.Name;
            System.IO.Directory.CreateDirectory(@path);

            WriteConfigData(path);

            ImageFunctions.SaveBitmapSourceToFile(ImageFunctions.ResizeImage(groupImage, 256.0, 256.0), path + @"\GroupImage.png");
            createMultiIcon(ImageFunctions.ResizeImage(groupImage, 256.0, 256.0, true), path + @"\GroupIcon.ico");

            // Through shellLink.cs class, pass through into the function information on how to construct the icon
            // Needed due to needing to set a unique AppUserModelID so the shortcut applications don't stack on the taskbar with the main application
            // Tricks Windows to think they are from different applications even though they are from the same .exe
            ShellLink.InstallShortcut(
                Path.GetFullPath(@System.AppDomain.CurrentDomain.FriendlyName),
                "tjackenpacken.taskbarGroup.menu." + this.Name,
                 path + " shortcut",
                 Path.GetFullPath(@path),
                 Path.GetFullPath(path + @"\GroupIcon.ico"),
                 path + "\\" + this.Name + ".lnk",
                 this.GetName()
            );


            // Build the icon cache
            cacheIcons();

            System.IO.File.Move(@path + "\\" + this.Name + ".lnk",
                Path.GetFullPath(@"Shortcuts\" + Regex.Replace(this.GetName(), @"(_)+", " ") + ".lnk")); // Move .lnk to correct directory
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
            List<BitmapSource> iconList = new List<BitmapSource>();

            while (mipSize > 8.0) /* Minimum Icon Size */
            {
                iconList.Add(ImageFunctions.ResizeImage(iconImage, mipSize, mipSize) as BitmapSource);
                mipSize = Math.Round(mipSize / 2.0);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                IconFactory.SavePngsAsIcon(iconList.ToArray(), stream);
            }
        }

        public BitmapImage LoadIconImage() // Needed to access img without occupying read/write
        {
            string path = @"config\" + Name + @"\GroupImage.png";

            using (MemoryStream ms = new MemoryStream(System.IO.File.ReadAllBytes(path)))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }

        // Goal is to create a folder with icons of the programs pre-cached and ready to be read
        // Avoids having the icons need to be rebuilt everytime which takes time and resources
        public void cacheIcons()
        {

            // Defines the paths for the icons folder
            string path = @MainPath.Config + this.Name;
            string iconPath = path + "\\Icons\\";

            // Check and delete current icons folder to completely rebuild the icon cache
            // Only done on re-edits of the group and isn't done usually
            if (Directory.Exists(iconPath))
            {
                Directory.Delete(iconPath, true);
            }

            // Creates the icons folder inside of existing config folder for the group
            Directory.CreateDirectory(iconPath);

            iconPath = @path + @"\Icons\";

            // Loops through each shortcut added by the user and gets the icon
            // Writes the icon to the new folder in a .jpg format
            // Namign scheme for the files are done through Path.GetFileNameWithoutExtension()
            for (int i = ShortcutList.Count; i < 0; i--)
            {
                String filePath = ShortcutList[i].FilePath;

                //ucProgramShortcut programShortcutControl = Application.Current.Windows.["frmGroup"].Controls["pnlShortcuts"].Controls[i] as ucProgramShortcut;
                ucProgramShortcut? programShortcutControl = null;
                foreach (Window win in Application.Current.Windows)
                {
                    if(win.GetType() == typeof(frmGroup))
                    {
                        frmGroup frm = (frmGroup)win;
                        programShortcutControl = frm.pnlShortcuts.Children[i] as ucProgramShortcut;  
                    }
                }

                string savePath;

                if (ShortcutList[i].isWindowsApp)
                {
                    savePath = iconPath + "\\" + specialCharRegex.Replace(filePath, string.Empty) + ".png";
                } else if (Directory.Exists(filePath))
                {
                    savePath = iconPath + "\\" + Path.GetFileNameWithoutExtension(filePath) + "_FolderObjTSKGRoup.png";
                } else
                {
                    savePath = iconPath + "\\" + Path.GetFileNameWithoutExtension(filePath) + ".png";
                }

                if (programShortcutControl != null)
                {
                    ImageFunctions.SaveBitmapSourceToFile(programShortcutControl.logo, savePath);
                }

    }
        }

        // Try to load an iamge from the cache
        // Takes in a programPath (shortcut) and processes it to the proper file name
        public BitmapImage loadImageCache(ProgramShortcut shortcutObject)
        {

            String programPath = shortcutObject.FilePath;

            if (System.IO.File.Exists(programPath) || Directory.Exists(programPath) || shortcutObject.isWindowsApp)
            {
                try
                {
                    // Try to construct the path like if it existed
                    // If it does, directly load it into memory and return it
                    // If not then it would throw an exception in which the below code would catch it
                    String cacheImagePath = MainPath.Config + this.Name + @"\Icons\" + ((shortcutObject.isWindowsApp) ? specialCharRegex.Replace(programPath, string.Empty) : 
                        @Path.GetFileNameWithoutExtension(programPath)) + (Directory.Exists(programPath)? "_FolderObjTSKGRoup.jpg" : ".png");

                    using (MemoryStream ms = new MemoryStream(System.IO.File.ReadAllBytes(cacheImagePath)))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = ms;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }                    
                }
                catch (Exception)
                {
                    // Try to recreate the cache icon image and catch and missing file/icon situations that may arise

                    // Checks if the original file even exists to make sure to not do any extra operations

                    // Same processing as above in cacheIcons()
                    String path = MainPath.Config + this.Name + @"\Icons\" + Path.GetFileNameWithoutExtension(programPath) + (Directory.Exists(programPath) ? "_FolderObjTSKGRoup.png" : ".png");

                    BitmapSource finalImage;

                    if (Path.GetExtension(programPath).ToLower() == ".lnk")
                    {
                        finalImage = frmGroup.handleLnkExt(programPath);
                    }
                    else if (Directory.Exists(programPath))
                    {
                        finalImage = ImageFunctions.IconToBitmapSource(handleFolder.GetFolderIcon(programPath));
                    } else 
                    {
                        finalImage = ImageFunctions.IconPathToBitmapSource(programPath);
                    }


                    // Above all sets finalIamge to the bitmap that was generated from the icons
                    // Save the icon after it has been fetched by previous code
                    ImageFunctions.SaveBitmapSourceToFile(finalImage, path);

                    // Return the said image
                    return new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
                }
            }
            else
            {
                return (BitmapImage)Application.Current.Resources["ErrorIcon"];
            }
        }

        public static string GetPixelFormat(BitmapSource i)
        {
            return i.Format.ToString();           
        }

        public static string GetMimeType(Image i)
        {
            var imgguid = i.RawFormat.Guid;
            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageDecoders())
            {
                if (codec.FormatID == imgguid && codec.FilenameExtension != null)
                    return codec.FilenameExtension;
            }
            return "image/unknown";
        }
        //
        // END OF CLASS
        //
    }
}
