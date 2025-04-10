using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TaskbarGroupsEx.Classes;
using TaskbarGroupsEx.GroupItems;
using Windows.Devices.Geolocation;
using Windows.Security.Cryptography.Core;
using static TaskbarGroupsEx.Handlers.ApplicationShellItemHandler;
using static TaskbarGroupsEx.Handlers.lnkFileHandler;

namespace TaskbarGroupsEx.Handlers
{
    internal class ApplicationShellItemHandler
    {
        internal class ApplicationShellItem
        {
            public string? ID;
            public string? PackageFamilyName;
            public string? PackageFullName;
            public string? PackageInstallPath;

            public dynamic? shellItem;

            private static Guid GuidApplicationShellItem = new Guid("4234d49b-0245-4df3-b780-3893943456e1");
            private static Guid GuidApplicationShellPropertySets = new Guid("9f4c2855-9f79-4b39-a8d0-e1d42de1d5f3");

            public ApplicationShellItem() { }

            private uint GetProperty(ByteReader byteReader, ref object? property)
            {
                uint _size = byteReader.scan_uint();
                using (ByteReader propSetReader = new ByteReader(byteReader.read_bytes(_size)))
                {
                    propSetReader.read_uint();//skipSize
                    uint propertyType = propSetReader.read_uint();
                    propSetReader.read_byte(); //buffer
                    uint propType = propSetReader.read_uint();
                    if (propType == 0x1f)
                    {
                        property = propSetReader.read_LPWSTR();
                    }
                    return propertyType;
                }
            }

            private void ReadPropertySets(ByteReader byteReader)
            {
                if (byteReader.findGuid(GuidApplicationShellPropertySets) == uint.MaxValue)
                    return;

                ByteReader propSetReader = new ByteReader(byteReader);

                propSetReader.jump2guid(GuidApplicationShellPropertySets);
                while (propSetReader.scan_uint() != 0)
                {
                    object? _property = null;
                    uint propertyType = GetProperty(propSetReader, ref _property);

                    switch (propertyType)
                    {
                        case 5: ID = (string?)_property; break; //ID
                        case 15: PackageInstallPath = (string?)_property; break; //PackageInstallPath
                        case 17: PackageFamilyName = (string?)_property; break; //PackageFamilyName
                        case 21: PackageFullName = (string?)_property; break; //PackageFullName
                    }
                }
            }

            private ApplicationShellItem(ByteReader byteReader)
            {
                ReadPropertySets(byteReader);

                if (ID != null)
                    shellItem = ShellApplicationHelper.GetShellItem(ID);
            }

            public static ApplicationShellItem? GetApplicationShellItem(byte[] bytes)
            {
                ByteReader byteReader = new ByteReader(bytes);
                uint AppShellLocation = byteReader.findGuid(GuidApplicationShellItem);
                if (AppShellLocation == uint.MaxValue)
                    return null;

                return new ApplicationShellItem(byteReader);
            }

            public static bool IsShellItemPresent(ByteReader byteReader)
            {
                uint AppShellLocation = byteReader.findGuid(ApplicationShellItem.GuidApplicationShellItem);
                return AppShellLocation != uint.MaxValue;
            }

            public string? GetAppID()
            {
                return ID;
            }

            public string? GetPackageFamilyName()
            {
                if (shellItem != null)
                {
                    return ShellApplicationHelper.GetStringPropertyFromShell(shellItem, "System.AppUserModel.PackageFamilyName");
                }
                return null;
            }

            public string? GetSmallLogo()
            {
                if (shellItem != null)
                {
                    return ShellApplicationHelper.GetStringPropertyFromShell(shellItem, "System.Tile.SmallLogoPath");
                }
                return null;
            }
        }

        public static bool ContainsApplicationShellItem(MemoryStream dropData)
        {
            byte[] bs = dropData.ToArray();
            ByteReader byteReader = new ByteReader(bs);

            return ApplicationShellItem.IsShellItemPresent(byteReader);
        }

        public static DynamicGroupItem? GetGroupItem(MemoryStream dropData)
        {
            ApplicationShellItem? applicationShellItem = ApplicationShellItem.GetApplicationShellItem(dropData.ToArray());

            if (applicationShellItem == null || applicationShellItem.ID == null)
                return null;

            string targetCmd = MainPath.ParseGuidInPath(applicationShellItem.ID);

            if (ShellApplicationHelper.IsAppUserModelID(applicationShellItem.ID))
            {
                dynamic? shellItem = ShellApplicationHelper.GetShellItem(applicationShellItem.ID);
                if (shellItem != null)
                {
                    string itemLabel = "";
                    string itemArgs = "";

                    if (ShellApplicationHelper.IsUWP(shellItem))
                        return new UWPGroupItem(targetCmd);

                    itemLabel = ShellApplicationHelper.GetStringPropertyFromShell(shellItem, "System.ItemNameDisplay");
                    itemArgs = ShellApplicationHelper.GetStringPropertyFromShell(shellItem, "System.Link.Arguments");
                    targetCmd = ShellApplicationHelper.GetStringPropertyFromShell(shellItem, "System.Link.TargetParsingPath");
                    if (!File.Exists(targetCmd))
                        targetCmd = ShellApplicationHelper.GetTargetFromShell(shellItem);

                    if (Path.GetExtension(targetCmd) == ".lnk")
                    {
                        if(lnkFileHandler.Islnk(targetCmd))
                        {
                            return lnkFileHandler.GetGroupItem(targetCmd);
                        }
                    }

                    if (Path.GetExtension(targetCmd) == ".url")
                    {
                        if (urlFileHandler.isURLFile(targetCmd))
                        {
                            return urlFileHandler.GetGroupItem(itemLabel, targetCmd);
                        }
                    }

                    DynamicGroupItem? groupItem = null;

                    if (Path.GetExtension(targetCmd) == ".exe")
                    {
                        groupItem = new ApplicationGroupItem(targetCmd, itemArgs);
                    }
                    else if (File.Exists(targetCmd))
                    {
                        groupItem = new FileGroupItem(targetCmd);
                    }
                    else if (Directory.Exists(targetCmd))
                    {
                        groupItem = new FolderGroupItem(targetCmd);
                    }

                    if (groupItem != null && itemLabel != "")
                    {
                        groupItem.mName = itemLabel;
                    }

                    return groupItem;
                }
            }

            return null;
        }
    }
}
