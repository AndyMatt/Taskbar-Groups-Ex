using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using TaskbarGroupsEx.GroupItems;

namespace TaskbarGroupsEx.Handlers
{
    internal class urlFileHandler
    {
        public enum LinkType
        {
            URL,
            URI
        };

        public LinkType mType;

        public string mName;
        public string mCommand;
        public string mIconPath;
        public uint mIconIndex;

        public urlFileHandler(string urlFilePath)
        {
            mName = System.IO.Path.GetFileNameWithoutExtension(urlFilePath);
            string urlFileStr = ReadUrlShortcutFile(urlFilePath);
            mCommand = ReadUrl(urlFileStr);
            mIconPath = ReadIconPath(urlFileStr);
            mIconIndex = ReadIconIndex(urlFileStr);
            mType = LinkType.URI;

            Uri uriResult;
            bool result = Uri.TryCreate(mCommand, UriKind.Absolute, out uriResult);
            if (uriResult != null) {
                if(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
                {
                    mType = LinkType.URL;
                }
            }
        }

        string ReadUrl(string stringBuffer)
        {
            Match matched = Regex.Match(stringBuffer, "(?im)^\\s*URL\\s*=\\s*([^\r\n]*)");
            if (matched.Success && matched.Groups.Count > 1)
            {
                return matched.Groups[1].Value;
            }
            return "";
        }

        string ReadIconPath(string stringBuffer)
        {
            Match matched = Regex.Match(stringBuffer, "(?im)^\\s*IconFile\\s*=\\s*([^\r\n]*)");
            if (matched.Success && matched.Groups.Count > 1)
            {
                return matched.Groups[1].Value;
            }
            return "";
        }

        uint ReadIconIndex(string stringBuffer)
        {
            Match matched = Regex.Match(stringBuffer, "(?im)^\\s*IconIndex\\s*=\\s*([^\r\n]*)");
            if (matched.Success && matched.Groups.Count > 1)
            {
                return uint.Parse(matched.Groups[1].Value);
            }
            return 0;
        }

        public static bool isURLFile(string filePath)
        {
            using var reader = System.IO.File.OpenText(filePath);
            {
                string? line;
                if ((line = reader.ReadLine()) != null)
                {
                    if(line.Contains("{000214A0-0000-0000-C000-000000000046}", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (line.ToUpper().Contains("[InternetShortcut]", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        public static string ReadUrlShortcutFile(string filePath)
        {
            string buffer = "";
            using var reader = System.IO.File.OpenText(filePath);
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    buffer += line;
                    buffer += Environment.NewLine;
                }
            }
            return buffer;
        }

        public static DynamicGroupItem GetGroupItem(string name, string urlFileLocation)
        {
            urlFileHandler urlFile = new urlFileHandler(urlFileLocation);
            urlFile.mName = name != "" ? name : urlFile.mName;

            return (urlFile.mType == urlFileHandler.LinkType.URL) ? new URLGroupItem(urlFile) : new URIGroupItem(urlFile);
        }
    }
}
