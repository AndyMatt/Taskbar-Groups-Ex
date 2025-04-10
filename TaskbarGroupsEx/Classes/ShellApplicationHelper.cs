using Shell32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskbarGroupsEx.GroupItems;
using TaskbarGroupsEx.Handlers;
using Windows.Management.Deployment;

namespace TaskbarGroupsEx.Classes
{
    class ShellApplicationHelper
    {
        private static dynamic? _ShellApp;
        private static dynamic? _ShellAppFolder;

        private static dynamic? GetShellApp()
        {
            if (_ShellApp == null)
            {
                Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
                if (shellAppType == null)
                    return null;

                _ShellApp = Activator.CreateInstance(shellAppType);
            }
            return _ShellApp;
        }
        private static dynamic? GetShellAppsFolder()
        {
            if (_ShellAppFolder == null)
            {
                dynamic? shellApp = GetShellApp();
                _ShellAppFolder = shellApp.NameSpace("shell:Appsfolder");
            }

            return _ShellAppFolder;
        }

        public static bool IsAppUserModelID(string appID)
        {
            return GetShellItem(appID) != null;
        }

        public static bool IsUWP(dynamic? shellItem)
        {
            string FamilyName = GetStringPropertyFromShell(shellItem, "System.AppUserModel.PackageFamilyName");
            return FamilyName!= null && FamilyName != "";
        }

        public static dynamic? GetShellItem(string appID)
        {
            dynamic? shellFolder = GetShellAppsFolder();

            if (shellFolder != null)
            {
                foreach (FolderItem2 item in shellFolder.Items())
                {
                    dynamic? _appID = item.ExtendedProperty("System.AppUserModel.ID");
                    if (_appID != null && _appID == appID)
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        public static string GetStringPropertyFromShell(dynamic? shellItem, string Property)
        {
            if (shellItem != null)
            {
                dynamic? prop = shellItem.ExtendedProperty(Property);
                return prop;
            }
            return "";
        }

        public static string? GetTargetFromShell(dynamic? shellItem)
        {
            //Using the ShortcutName/AppName from a shortcut that behaves link a UWP link we
            //then parse all the lnk files in the start menu until we find the matching shortcut

            //Another note is that the ApplicationUserModelID is located at the end of a lnk file,
            //TODO check footer for matching ApplicationUserModelID
            if (shellItem != null)
            {
                string appName = shellItem.Name;

                List<string> files = new List<string>();
                files.AddRange(Directory.GetFiles(MainPath.GetCommonStartMenuPath(), "*.*", SearchOption.AllDirectories));
                files.AddRange(Directory.GetFiles(MainPath.GetStartMenuPath(), "*.*", SearchOption.AllDirectories));

                foreach (string file in files)
                {
                    if (file != null && System.IO.Path.GetFileNameWithoutExtension(file) == appName)
                    {
                        return file;
                    }
                }
            }
            return null;
        }

        public static DynamicGroupItem? GetGroupItem(string appID)
        {
            dynamic? shellItem = ShellApplicationHelper.GetShellItem(appID);
            if (shellItem != null)
            {
                if (ShellApplicationHelper.IsUWP(shellItem))
                {
                    return new UWPGroupItem(appID);
                }
                string? targetCmd = ShellApplicationHelper.GetTargetFromShell(shellItem);
                if (targetCmd != null)
                {
                    if (targetCmd.ToLower().EndsWith(".exe"))
                    {
                        return new ApplicationGroupItem(targetCmd);
                    }
                    else if (targetCmd.ToLower().EndsWith(".lnk"))
                    {
                        if (lnkFileHandler.Islnk(targetCmd))
                        {
                            return lnkFileHandler.GetGroupItem(targetCmd);
                        }
                    }
                    else if (targetCmd.ToLower().EndsWith(".url"))
                    {
                        if (File.Exists(targetCmd) && urlFileHandler.isURLFile(targetCmd))
                        {
                            urlFileHandler urlFile = new urlFileHandler(targetCmd);
                            return new URIGroupItem(Path.GetFileNameWithoutExtension(targetCmd), urlFile.mCommand, urlFile.mIconPath);
                        }
                    }
                    else if (Uri.IsWellFormedUriString(targetCmd, UriKind.Absolute))
                    {
                        new URIGroupItem(targetCmd);
                    }
                }
            }

            return null;
        }
    }
}
