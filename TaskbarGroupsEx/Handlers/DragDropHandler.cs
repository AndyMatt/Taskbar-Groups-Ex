using Shell32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TaskbarGroupsEx.Classes;
using TaskbarGroupsEx.GroupItems;

namespace TaskbarGroupsEx.Handlers
{
    internal class DragDropHandler
    {
        public static List<DynamicGroupItem?> GetShortcuts(IDataObject dropData)
        {
            List<DynamicGroupItem?> shortcuts = new List<DynamicGroupItem?>();

            string[] formats = dropData.GetFormats();

            if (dropData.GetDataPresent(DataFormats.FileDrop))
            {
                ProcessFileDrop(ref shortcuts, dropData.GetData(DataFormats.FileDrop));

                if (shortcuts.Count > 0)
                {
                    return shortcuts.Distinct(new GroupItemComparer()).ToList();
                }
            }

            if (dropData.GetDataPresent("Shell IDList Array"))
            {
                ProcessShellIDListArray(ref shortcuts, dropData.GetData("Shell IDList Array"));
            }

            if (dropData.GetDataPresent("text/x-moz-url"))    //Chromium
            {
                ProcessChromiumUrl(ref shortcuts, dropData.GetData("text/x-moz-url"));
            }

            if (dropData.GetDataPresent("UniformResourceLocatorW"))
            {
                ProcessUniformResourceLocatorW(ref shortcuts, dropData.GetData("UniformResourceLocatorW"));
            }

            return shortcuts.Distinct(new GroupItemComparer()).ToList();
        }

        public static List<DynamicGroupItem?> GetFiles(string[] files)
        {
            List<DynamicGroupItem?> groupItems = new List<DynamicGroupItem?>();
            ProcessFiles(ref groupItems, files);
            groupItems.RemoveAll(IsNull);
            return groupItems;
        }

        private static bool IsNull(DynamicGroupItem? groupItem)
        {
            return groupItem == null;
        }

        static bool ProcessUniformResourceLocatorW(ref List<DynamicGroupItem?> shortcuts, object DropData)
        {
            if (DropData.GetType() == typeof(MemoryStream))
            {
                string memStr = Encoding.Unicode.GetString(((MemoryStream)DropData).ToArray());
                if (memStr.StartsWith("ms-appid:W~"))
                {
                    memStr = memStr.Replace("ms-appid:W~", "");
                    memStr = memStr.Replace("\0", "");

                    if (ShellApplicationHelper.IsAppUserModelID(memStr))
                    {
                        shortcuts.Add(ShellApplicationHelper.GetGroupItem(memStr));
                        return true;
                    }
                }
            }
            return false;
        }

        static bool ProcessChromiumUrl(ref List<DynamicGroupItem?> shortcuts, object DropData)
        {
            if (DropData.GetType() == typeof(MemoryStream))
            {
                string urlStr = Encoding.Unicode.GetString(((MemoryStream)DropData).ToArray());
                string[] urlInfo = urlStr.Trim('\0').Split('\n');
                if (urlInfo.Length > 1)
                {
                    shortcuts.Add(new URLGroupItem(urlInfo[0], urlInfo[1]));
                }
                else if (urlInfo.Length == 1)
                {
                    shortcuts.Add(new URLGroupItem(urlInfo[0]));
                }
                return true;
            }
            return false;
        }

        static void ProcessShellIDListArray(ref List<DynamicGroupItem?> shortcuts, object DropData)
        {
            if (DropData.GetType() == typeof(MemoryStream))
            {
                MemoryStream memoryStream = (MemoryStream)DropData;
                if (ApplicationShellItemHandler.ContainsApplicationShellItem(memoryStream))
                {
                    shortcuts.Add(ApplicationShellItemHandler.GetGroupItem(memoryStream));
                }
            }
        }

        static void ProcessFileDrop(ref List<DynamicGroupItem?> shortcuts, object DropData)
        {
            if (DropData.GetType() == typeof(string[]))
            {
                List<DynamicGroupItem?> groupItems = new List<DynamicGroupItem?>();
                ProcessFiles(ref groupItems, (string[])DropData);
                groupItems.RemoveAll(IsNull);
                shortcuts.AddRange(groupItems);
            }
        }

        static void ProcessFiles(ref List<DynamicGroupItem?> shortcuts, string[] files)
        {
            foreach (var file in files)
            {
                if (ShellApplicationHelper.IsAppUserModelID(file))
                {
                    shortcuts.Add(ShellApplicationHelper.GetGroupItem(file));
                }
                else if (File.Exists(file))
                {
                    if (lnkFileHandler.Islnk(file))
                    {
                        DynamicGroupItem? groupItem = lnkFileHandler.GetGroupItem(file);
                        if (groupItem != null)
                            shortcuts.Add(groupItem);
                    }
                    else if (File.Exists(file) && urlFileHandler.isURLFile(file))
                    {
                        DynamicGroupItem groupItem;
                        urlFileHandler urlFile = new urlFileHandler(file);
                        if (urlFile.mType == urlFileHandler.LinkType.URL)
                        {
                            groupItem = new URLGroupItem(urlFile.mCommand)
                            {
                                mName = urlFile.mName
                            };
                        }
                        else
                        {
                            groupItem = new URIGroupItem(urlFile.mName, urlFile.mCommand, urlFile.mIconPath);
                        }
                        shortcuts.Add(groupItem);
                    }
                    else
                    {
                        shortcuts.Add(new FileGroupItem(file));
                    }
                }
                else if (Path.Exists(file))
                {
                    shortcuts.Add(new FolderGroupItem(file));
                }
                else if (Uri.IsWellFormedUriString(file, UriKind.Absolute))
                {
                    shortcuts.Add(new URIGroupItem(file));
                }
            }
        }
    }
}
