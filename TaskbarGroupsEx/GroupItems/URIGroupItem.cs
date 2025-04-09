using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskbarGroupsEx.Classes;
using TaskbarGroupsEx.Handlers;

namespace TaskbarGroupsEx.GroupItems
{
    internal class URIGroupItem : DynamicGroupItem
    {
        public override Types GetGroupType() { return Types.URI; }
        public URIGroupItem() : base() { }
        public URIGroupItem(string filePath) : base(filePath) { }
        public URIGroupItem(ConfigFile.GroupItemConfig itemConfig) : base(itemConfig) { }

        public URIGroupItem(string name, string filePath, string iconPath) : base(filePath)
        {
            mName = name;
            LoadIconFromFile(iconPath);
        }

        public URIGroupItem(urlFileHandler urlFileHandle) : base()
        {
            mCommand = urlFileHandle.mCommand;
            mName = urlFileHandle.mName;
            LoadIconFromFile(urlFileHandle.mIconPath);
        }

        public static URIGroupItem OnRead(Dictionary<string, string> data, string shortcutKey)
        {
            URIGroupItem groupItem = new URIGroupItem()
            {
                mName = data[shortcutKey + ".Name"],
                mCommand = data[shortcutKey + ".Command"],
                mIconPath = data[shortcutKey + ".IconPath"],
            };
            groupItem.LoadCacheIcon();
            return groupItem;
        }
    }
}
