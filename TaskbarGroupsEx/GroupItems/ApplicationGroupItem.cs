using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TaskbarGroupsEx.Classes;
using Windows.Security.Cryptography.Core;

namespace TaskbarGroupsEx.GroupItems
{
    internal class ApplicationGroupItem : DynamicGroupItem
    {
        public string mWorkingDirectory = "";
        public string mArguments = "";
        public override Types GetGroupType() { return Types.Application; }
        public ApplicationGroupItem() : base() { }

        public ApplicationGroupItem(string filePath, string args = "", string workingDir = "") : base(filePath)
        {
            mArguments = args;
            mWorkingDirectory = workingDir;
            LoadIconFromFile(filePath);
        }

        public ApplicationGroupItem(ConfigFile.GroupItemConfig itemConfig) : base(itemConfig)
        {
            itemConfig.GetProperty("Arguments", ref mArguments!);
            itemConfig.GetProperty("WorkingDirectory", ref mWorkingDirectory!);
        }

        public override bool LoadIconFromFile(string filePath)
        {
            if (filePath.EndsWith("exe"))
            {
                Icon? _ico = Icon.ExtractAssociatedIcon(filePath);
                mIcon = ImageFunctions.IconToBitmapSource(_ico);
                return true;
            }

            return base.LoadIconFromFile(filePath);
        }

        override public void OnExecute()
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    Arguments = mArguments,
                    FileName = mCommand,
                    WorkingDirectory = mWorkingDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }

        override public void OnWrite(ConfigFile.GroupItemConfig Config)
        {
            base.OnWrite(Config);
            Config.WriteProperty("Arguments", mArguments);
            Config.WriteProperty("WorkingDirectory", mWorkingDirectory);
        }
    }
}
