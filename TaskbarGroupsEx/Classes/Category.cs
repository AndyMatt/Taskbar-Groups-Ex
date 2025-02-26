using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TaskbarGroupsEx.Classes
{
    public class Category
    {
        public string Name;
        public string ColorString = "Depriciated";
        public bool allowOpenAll = false;
        public List<ProgramShortcut> ShortcutList;
        public int Width; // not used aon
        public double Opacity = 10;
        Regex specialCharRegex = new Regex("[*'\",_&#^@]");

        private static int[] iconSizes = new int[] {16,32,64,128,256,512};

        public System.Windows.Media.Color CatagoryBGColor = System.Windows.Media.Color.FromArgb(255, 31, 31, 31);


        public Category(string path)
        {
            // Use application's absolute path; (grabs the .exe)
            // Gets the parent folder of the exe and concats the rest of the path
            string fullPath;

            // Check if path is a full directory or part of a file name
            // Passed from the main shortcut client and the config client

            if (System.IO.File.Exists(@MainPath.path + @"\" + path + @"\ObjectData.xml"))
            {
                fullPath = @MainPath.path + @"\" + path + @"\ObjectData.xml";
            }
            else
            {
                fullPath = Path.GetFullPath(path + "\\ObjectData.xml");
            }

            System.Xml.Serialization.XmlSerializer reader =
                new System.Xml.Serialization.XmlSerializer(typeof(Category));
            using (StreamReader file = new StreamReader(fullPath))
            {
                Category category = (Category)reader.Deserialize(file);
                this.Name = category.Name;
                this.ShortcutList = category.ShortcutList;
                this.Width = category.Width;
                this.ColorString = category.ColorString;
                if(this.ColorString != "Depriciated")
                {
                    System.Drawing.Color _color = ImageFunctions.FromString(this.ColorString);
                    Byte _opacity = Convert.ToByte(category.Opacity * 255.0 / 100.0);
                    category.CatagoryBGColor = System.Windows.Media.Color.FromArgb(_opacity, _color.R, _color.G, _color.B);
                    this.ColorString = "Depriciated";
                }
                this.Opacity = category.Opacity;
                this.allowOpenAll = category.allowOpenAll;
                this.CatagoryBGColor = category.CatagoryBGColor;
            }
        }

        public Category() // needed for XML serialization
        {

        }

        public void CreateConfig(BitmapSource groupImage)
        {

            string path = @"config\" + this.Name;
            //string filePath = path + @"\" + this.Name + "Group.exe";
            //
            // Directory and .exe
            //
            System.IO.Directory.CreateDirectory(@path);

            //System.IO.File.Copy(@"config\config.exe", @filePath);
            //
            // XML config
            //
            System.Xml.Serialization.XmlSerializer writer =
                new System.Xml.Serialization.XmlSerializer(typeof(Category));

            using (FileStream file = System.IO.File.Create(@path + @"\ObjectData.xml"))
            {
                writer.Serialize(file, this);
                file.Close();
            }

            // Create .ico
            BitmapSource img = ImageFunctions.ResizeImage(groupImage, 256.0, 256.0); // Resize img if too big
            ImageFunctions.SaveBitmapSourceToFile(img , path + @"\GroupImage.png");

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
                 this.Name
            );


            // Build the icon cache
            cacheIcons();

            System.IO.File.Move(@path + "\\" + this.Name + ".lnk",
                Path.GetFullPath(@"Shortcuts\" + Regex.Replace(this.Name, @"(_)+", " ") + ".lnk")); // Move .lnk to correct directory
        }

        private static void createMultiIcon(BitmapSource iconImage, string filePath)
        {


            var diffList = from number in iconSizes
                select new
                    {
                        number,
                        difference = Math.Abs(number - iconImage.Height)
                    };
            var nearestSize = (from diffItem in diffList
                          orderby diffItem.difference
                          select diffItem).First().number;

            List<BitmapSource> iconList = new List<BitmapSource>();

            while (nearestSize != 8)
            {
                iconList.Add(ImageFunctions.ResizeImage(iconImage, nearestSize, nearestSize) as BitmapSource);
                nearestSize = (int)Math.Round((decimal) nearestSize / 2);
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
            string path = @MainPath.path + @"\config\" + this.Name;
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
                    String cacheImagePath = @Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + 
                        @"\config\" + this.Name + @"\Icons\" + ((shortcutObject.isWindowsApp) ? specialCharRegex.Replace(programPath, string.Empty) : 
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
                    String path = MainPath.path + @"\config\" + this.Name + @"\Icons\" + Path.GetFileNameWithoutExtension(programPath) + (Directory.Exists(programPath) ? "_FolderObjTSKGRoup.png" : ".png");

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
                        finalImage = ImageFunctions.IconToBitmapSource(Icon.ExtractAssociatedIcon(programPath));
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
                if (codec.FormatID == imgguid)
                    return codec.FilenameExtension;
            }
            return "image/unknown";
        }
        //
        // END OF CLASS
        //
    }
}
