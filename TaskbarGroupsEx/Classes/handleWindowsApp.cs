using System.IO;
using System.Windows.Media.Imaging;
using System.Xml;
using Windows.Management.Deployment;

namespace TaskbarGroupsEx.Classes
{
    class handleWindowsApp
    {
        public static Dictionary<string, string> fileDirectoryCache = new Dictionary<string, string>();

        private static PackageManager pkgManger = new PackageManager();
        public static BitmapSource getWindowsAppIcon(String file, bool alreadyAppID = false)
        {
            // Get the app's ID from its shortcut target file (Ex. 4DF9E0F8.Netflix_mcm4njqhnhss8!Netflix.app)
            String microsoftAppName = (!alreadyAppID) ? GetLnkTarget(file) : file;

            // Split the string to get the app name from the beginning (Ex. 4DF9E0F8.Netflix)
            String subAppName = microsoftAppName.Split('!')[0];

            // Loop through each of the folders with the app name to find the one with the manifest + logos
            String appPath = findWindowsAppsFolder(subAppName);

            // Load and read manifest to get the logo path
            XmlDocument appManifest = new XmlDocument();
            appManifest.Load(appPath + "\\AppxManifest.xml");

            XmlNamespaceManager appManifestNamespace = new XmlNamespaceManager(new NameTable());
            appManifestNamespace.AddNamespace("sm", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");


            XmlNode? node = appManifest.SelectSingleNode("/sm:Package/sm:Properties/sm:Logo", appManifestNamespace);

            String? logoLocation = null;
            if (node != null) {
                logoLocation = (node.InnerText).Replace("\\", @"\");
            }


            if (logoLocation != null)
            {
                // Get the last instance or usage of \ to cut out the path of the logo just to have the path leading to the general logo folder
                logoLocation = logoLocation.Substring(0, logoLocation.LastIndexOf(@"\"));
                String logoLocationFullPath = Path.GetFullPath(appPath + "\\" + logoLocation);

                // Search for all files with 150x150 in its name and use the first result
                DirectoryInfo logoDirectory = new DirectoryInfo(logoLocationFullPath);
                List<string> filesInDir = getLogoFolder("StoreLogo", logoDirectory);

                if (filesInDir.Count != 0)
                {
                    return getLogo(filesInDir.First(), file);
                }
                else
                {

                    filesInDir = getLogoFolder("scale-200", logoDirectory);

                    if (filesInDir.Count != 0)
                    {
                        return getLogo(filesInDir.First(), file);
                    }
                }
            }
            return ImageFunctions.ExtractIconToBitmapSource(file);
        }

        private static List<string> getLogoFolder(String keyname, DirectoryInfo logoDirectory)
        {
            // Search for all files with the keyname in its name and use the first result
            List<string> filesInDir = Directory.EnumerateFiles(logoDirectory.FullName, "*" + keyname + "*.*", SearchOption.AllDirectories).Where(s => !s.Contains("contrast")).ToList();
            return filesInDir;
        }

        private static BitmapSource getLogo(String logoPath, String defaultFile)
        {
            if (File.Exists(logoPath))
            {
                return ImageFunctions.BitmapSourceFromFile(logoPath);
            }

            return ImageFunctions.ExtractIconToBitmapSource(defaultFile);
        }

        public static string GetLnkTarget(string lnkPath)
        {
            var shl = new Shell32.Shell();
            lnkPath = System.IO.Path.GetFullPath(lnkPath);
            var dir = shl.NameSpace(System.IO.Path.GetDirectoryName(lnkPath));
            var itm = dir.Items().Item(System.IO.Path.GetFileName(lnkPath));
            var lnk = (Shell32.ShellLinkObject)itm.GetLink;
            return lnk.Target.Path;
        }

        public static string findWindowsAppsFolder(string subAppName)
        {

            if (!fileDirectoryCache.ContainsKey(subAppName))
            {
                try
                {
                    IEnumerable<Windows.ApplicationModel.Package> packages = pkgManger.FindPackagesForUser("", subAppName);


                    String finalPath = Environment.ExpandEnvironmentVariables("%ProgramW6432%") + $@"\WindowsApps\" + packages.First().InstalledLocation.DisplayName + @"\";
                    fileDirectoryCache[subAppName] = finalPath;
                    return finalPath;
                }
                catch (UnauthorizedAccessException) { };
                return "";
            }
            else
            {
                return fileDirectoryCache[subAppName];
            }
        }

        public static string findWindowsAppsName(string AppName)
        {
            String subAppName = AppName.Split('!')[0];
            String appPath = findWindowsAppsFolder(subAppName);

            

                // Load and read manifest to get the logo path
                XmlDocument appManifest = new XmlDocument();
            appManifest.Load(appPath + "\\AppxManifest.xml");

            XmlNamespaceManager appManifestNamespace = new XmlNamespaceManager(new NameTable());
            appManifestNamespace.AddNamespace("sm", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
            appManifestNamespace.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10");

            try
            {
                XmlNode? node = appManifest.SelectSingleNode("/sm:Package/sm:Applications/sm:Application/uap:VisualElements", appManifestNamespace);
                if (node != null){
                    XmlAttributeCollection? attrib = node.Attributes;
                    if (attrib != null) {
                        node = attrib.GetNamedItem("DisplayName");
                        if (node != null) {
                            return node.InnerText;
                        }
                    }
                }
            } catch (Exception)
            {
                XmlNode? node = appManifest.SelectSingleNode("/sm:Package/sm:Properties/sm:DisplayName", appManifestNamespace);
                if (node != null) {
                    return node.InnerText;
                }
            }

            return "ERROR";
        }
    }
}
