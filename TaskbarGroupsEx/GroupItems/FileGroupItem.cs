using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskbarGroupsEx.Classes;

namespace TaskbarGroupsEx.GroupItems
{
    internal class FileGroupItem : DynamicGroupItem
    {
        public override Types GetGroupType() { return Types.File; }
        public FileGroupItem() : base() { }
        public FileGroupItem(ConfigFile.GroupItemConfig itemConfig) : base(itemConfig) { }

        public FileGroupItem(string filePath) : base(filePath)
        {
            mName = System.IO.Path.GetFileName(filePath);
            LoadIconFromFile(filePath);
        }

        public override bool LoadIconFromFile(string filePath)
        {
            if (!base.LoadIconFromFile(filePath))
            {
                Icon? _ico = Icon.ExtractAssociatedIcon(filePath);
                mIcon = ImageFunctions.IconToBitmapSource(_ico);
                return true;
            }
            return false;
        }
    }
}
