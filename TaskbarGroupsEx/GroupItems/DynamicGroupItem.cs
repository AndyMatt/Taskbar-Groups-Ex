using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Xml.Linq;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Windows.Shapes;
using System.Windows;
using Windows.Security.Cryptography.Core;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using TaskbarGroupsEx.Handlers;
using TaskbarGroupsEx.Classes;

namespace TaskbarGroupsEx.GroupItems
{
    public enum Types
    {
        Application,
        UWP,
        URI,
        URL,
        File,
        Folder,
        Unknown,
    };

    public class DynamicGroupItem
    {
        public string mName = "";
        public string mCommand = "";
        public string mIconPath = "";

        public BitmapSource? mIcon;
        public virtual Types GetGroupType() { return Types.Unknown; }
        public DynamicGroupItem() { }
        public DynamicGroupItem(string filePath)
        {
            mName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            mCommand = filePath;
        }

        public DynamicGroupItem(ConfigFile.GroupItemConfig itemConfig)
        {
            itemConfig.GetProperty("Name", ref mName!);
            itemConfig.GetProperty("Command", ref mCommand!);
            if (mCommand == null)
                itemConfig.GetProperty("FilePath", ref mCommand!);
            itemConfig.GetProperty("IconPath", ref mIconPath!);
            LoadCacheIcon();
        }

        public void SetIconPath(string groupName)
        {
            string path = MainPath.CreateFolder(@MainPath.Config + groupName);
            string iconPath = MainPath.CreateFolder(path + "\\Icons\\");

            mIconPath = iconPath + "\\" + MainPath.GetSafeFileName(mName) + ".png";
        }

        public virtual bool LoadIconFromFile(string filePath)
        {
            if (System.IO.Path.GetExtension(filePath) == ".ico")
            {
                mIcon = FileHandler.OpenIco(filePath);
            }
            else if (System.IO.Path.GetExtension(filePath) == ".png")
            {
                mIcon = FileHandler.OpenPNG(filePath);
            }
            return mIcon != null;
        }

        public void LoadCacheIcon()
        {
            string iconPath = mIconPath!;
            if (!System.IO.Path.IsPathRooted(iconPath))
            {
                iconPath = MainPath.Config + mIconPath;
            }

            LoadIconFromFile(iconPath);
        }

        string GetRelativeIconPath()
        {
            return MainPath.GetRelativeDir(mIconPath!);
        }

        public void WriteIcon()
        {
            if (File.Exists(mIconPath))
                File.Delete(mIconPath);

            ImageFunctions.SaveBitmapSourceToFile(mIcon!, mIconPath!);
        }

        public virtual void OnExecute()
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = mCommand,
                    UseShellExecute = true
                });
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }

        public virtual void OnWrite(ConfigFile.GroupItemConfig itemConfig)
        {
            itemConfig.WriteProperty("Type", GetGroupType().ToString());
            itemConfig.WriteProperty("Name", mName);
            itemConfig.WriteProperty("Command", mCommand);
            itemConfig.WriteProperty("IconPath", GetRelativeIconPath());
        }
        public virtual BitmapSource GetIcon() { return mIcon != null ? mIcon : ImageFunctions.GetDefaultShortcutIcon(); }
    }

    class GroupItemComparer : IEqualityComparer<DynamicGroupItem?>
    {
        public bool Equals(DynamicGroupItem? item_a, DynamicGroupItem? item_b)
        {
            if (ReferenceEquals(item_a, item_b)) return true;

            if (ReferenceEquals(item_a, null) || ReferenceEquals(item_b, null))
                return false;

            return string.Compare(item_a.mName, item_b.mName, true) == 0 && string.Compare(item_a.mCommand, item_b.mCommand, true) == 0;
        }
        public int GetHashCode(DynamicGroupItem? item)
        {
            if (ReferenceEquals(item, null))
                return 0;

            int hashProductName = item.mName == null ? 0 : item.mName.ToLower().GetHashCode();
            int hashProductCommand = item.mCommand == null ? 0 : item.mCommand.ToLower().GetHashCode();

            return hashProductName ^ hashProductCommand;
        }
    }
}
