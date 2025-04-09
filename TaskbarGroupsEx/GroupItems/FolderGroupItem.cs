using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Xml.Linq;
using TaskbarGroupsEx.Classes;

namespace TaskbarGroupsEx.GroupItems
{
    internal class FolderGroupItem : DynamicGroupItem
    {
        public override Types GetGroupType() { return Types.Folder; }
        public FolderGroupItem() : base() { }
        public FolderGroupItem(ConfigFile.GroupItemConfig itemConfig) : base(itemConfig) { }

        public FolderGroupItem(string filePath) : base(filePath)
        {
            mName = new DirectoryInfo(filePath).Name;
            mCommand = filePath;
            LoadIconFromFile(mCommand);
        }

        public override bool LoadIconFromFile(string filePath)
        {
            if (!base.LoadIconFromFile(filePath))
            {
                mIcon = GetFolderIcon(filePath);
                return true;
            }
            return false;
        }

        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0;
        public static BitmapSource GetFolderIcon(string Path)
        {
            NativeMethods.SHFILEINFO shinfo = new NativeMethods.SHFILEINFO();
            NativeMethods.SHGetFileInfo(Path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);

            using (Icon i = System.Drawing.Icon.FromHandle(shinfo.hIcon))
            {
                return Imaging.CreateBitmapSourceFromHIcon(i.Handle, new Int32Rect(0, 0, i.Width, i.Height), BitmapSizeOptions.FromEmptyOptions());
            }
        }
    }
}
