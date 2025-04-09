using Shell32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TaskbarGroupsEx.Classes;
using TaskbarGroupsEx.Handlers;

namespace TaskbarGroupsEx.GroupItems
{
    public class UWPGroupItem : DynamicGroupItem
    {
        public string? mPackageInstallPath;
        public string? mID;
        private dynamic? mShellItem;

        public override Types GetGroupType() { return Types.UWP; }
        public UWPGroupItem() : base() { }
        public UWPGroupItem(ConfigFile.GroupItemConfig itemConfig) : base(itemConfig) { }

        public UWPGroupItem(string appID) : base(appID)
        {
            mCommand = mID = appID;
            mShellItem = ShellApplicationHelper.GetShellItem(appID);
            mName = GetDisplayName();
            mPackageInstallPath = GetPackageInstallPath();
            mIcon = GetIcon();
        }

        override public void OnExecute()
        {
            try
            {
                Process p = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        UseShellExecute = true,
                        FileName = $@"shell:appsFolder\{mCommand}"
                    }
                };
                p.Start();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }

        override public BitmapSource GetIcon()
        {
            if (mIcon == null)
            {
                string iconPath = $"{mPackageInstallPath}\\{GetSmallLogo()}";
                iconPath = FindAsset(iconPath);
                Uri IconUri = new Uri(iconPath);
                mIcon = new BitmapImage(IconUri);
            }
            return mIcon;
        }

        string FindAsset(string path)
        {
            string parentDir = Path.GetDirectoryName(path);
            string pattern = $"{Path.GetFileNameWithoutExtension(path)}*";
            string[] files = Directory.GetFiles(parentDir, pattern);

            return files.Length > 0 ? files[0] : "";
        }

        /*
        private static dynamic? ShellApp;

        static dynamic? GetShellAppsFolder()
        {
            if (ShellApp == null)
            {
                Type? shellAppType = Type.GetTypeFromProgID("Shell.Application");
                if (shellAppType == null)
                    return null;

                ShellApp = Activator.CreateInstance(shellAppType);
            }

            return ShellApp != null ? ShellApp.NameSpace("shell:Appsfolder") : null;
        }
        */

        private string GetPackageInstallPath()
        {
            return ShellApplicationHelper.GetStringPropertyFromShell(mShellItem, "System.AppUserModel.PackageInstallPath");

            /*
            dynamic? shellFolder = GetShellAppsFolder();

            if (shellFolder != null)
            {
                foreach (FolderItem2 item in shellFolder.Items())
                {
                    dynamic? appID = item.ExtendedProperty("System.AppUserModel.ID");
                    if (appID != null && appID == mID)
                    { 
                            dynamic? installPath = item.ExtendedProperty("System.AppUserModel.PackageInstallPath");
                        if (installPath != null)
                        {
                            return installPath;
                        }
                    }
                }
            }
            return "";
            */
        }

        private string GetDisplayName()
        {
            return ShellApplicationHelper.GetStringPropertyFromShell(mShellItem, "System.Tile.LongDisplayName");
            /*
            dynamic? shellFolder = GetShellAppsFolder();

            if (shellFolder != null)
            {
                foreach (FolderItem2 item in shellFolder.Items())
                {
                    dynamic? appID = item.ExtendedProperty("System.AppUserModel.ID");
                    if (appID != null && appID == mID)
                    {
                        dynamic? displayName = item.ExtendedProperty("System.Tile.LongDisplayName");
                        return displayName != null ? displayName : mID;
                    }
                }
            }
            return "";
            */
        }

        private string GetSmallLogo()
        {
            return ShellApplicationHelper.GetStringPropertyFromShell(mShellItem, "System.Tile.SmallLogoPath");
            /*
            dynamic? shellFolder = GetShellAppsFolder();

            if (shellFolder != null)
            {
                foreach (FolderItem2 item in shellFolder.Items())
                {
                    dynamic? appID = item.ExtendedProperty("System.AppUserModel.ID");
                    if (appID != null && appID == mID)
                    {
                        dynamic? SmallLogoPath = item.ExtendedProperty("System.Tile.SmallLogoPath");
                        if (SmallLogoPath != null)
                        {
                            return SmallLogoPath;
                        }
                    }
                }
            }
            return "";
            */
        }
    }
}
