using System.IO;
using System.Text.RegularExpressions;

namespace TaskbarGroupsEx.Classes
{
    public enum ShortcutType
    {
        Application,
        Directory,
        UWP,
        URI,
        URL,
        File,
        Unknown,
    }

    public class ProgramShortcut
    {
        public string FilePath { get; set; } = "";
        public string name { get; set; } = "";
        public string Arguments = "";
        public string WorkingDirectory = "";
        public ShortcutType type = ShortcutType.Unknown;

        public string IconPath = "";

        public static ProgramShortcut? CreateShortcut(string filepath)
        {
            ProgramShortcut newShortcut = DetermineShortcutType(filepath);        
            newShortcut.Init();
            return newShortcut.type == ShortcutType.Unknown ? null : newShortcut;
        }

        public static ProgramShortcut DetermineShortcutType(string shortcutCommand)
        {
            ProgramShortcut newShortcut = new ProgramShortcut();

            if (System.IO.File.Exists(shortcutCommand))
            {
                if (System.IO.Path.GetExtension(shortcutCommand).ToLower() == ".lnk")
                {
                    string iconLocation = ShellLink.GetIconPath(shortcutCommand);
                    newShortcut.FilePath = shortcutCommand;
                    newShortcut.type = ShortcutType.Shortcut;
                    return newShortcut;
                }
                else if (System.IO.Path.GetExtension(shortcutCommand).ToLower() == ".url")
                {
                    newShortcut.name = System.IO.Path.GetFileNameWithoutExtension(shortcutCommand);
                    string fileBuffer = ShellLink.ReadUrlShortcutFile(shortcutCommand);
                    newShortcut.FilePath = Regex.Match(fileBuffer, "(?im)^\\s*URL\\s*=\\s*([^\r\n]*)").Groups[1].Value;
                    newShortcut.IconPath = Regex.Match(fileBuffer, "(?im)^\\s*IconFile\\s*=\\s*([^\r\n]*)").Groups[1].Value;
                    newShortcut.type = ShortcutType.URI;
                    return newShortcut;
                }
                else
                {
                    if (ShellLink.GetExeType(shortcutCommand) != ShellLink.ShellFileGetInfo.ShellFileType.Unknown)
                    {
                        newShortcut.type = ShortcutType.Application;
                    }
                    else
                    {
                        newShortcut.type = ShortcutType.File;
                    }
                    newShortcut.IconPath = Environment.ExpandEnvironmentVariables(shortcutCommand);
                    newShortcut.FilePath = Environment.ExpandEnvironmentVariables(shortcutCommand);
                    newShortcut.WorkingDirectory = MainPath.GetShortcutWorkingDir(shortcutCommand);
                }
            }
            else
            {
                if (shortcutCommand.StartsWith("url:"))
                {
                    newShortcut.type = ShortcutType.URL;
                    string[] parts = shortcutCommand.Split("url:")[1].Split("\0")[0].Split(((char)10));
                    newShortcut.FilePath = parts[0];
                    newShortcut.name = parts[1];
                    return newShortcut;
                }
                else if (Directory.Exists(shortcutCommand))
                {
                    newShortcut.type = ShortcutType.Directory;
                    newShortcut.FilePath = Environment.ExpandEnvironmentVariables(shortcutCommand);
                }
                else if (handleWindowsApp.DoesAppExist(shortcutCommand))
                {
                    newShortcut.type = ShortcutType.UWP;
                    newShortcut.FilePath = Environment.ExpandEnvironmentVariables(shortcutCommand);
                }
                else if (Uri.IsWellFormedUriString(shortcutCommand, UriKind.RelativeOrAbsolute))
                {
                    newShortcut.type = ShortcutType.URI;
                    newShortcut.FilePath = Environment.ExpandEnvironmentVariables(shortcutCommand);
                }
            }

            return newShortcut;
        }

        public void UpdateWorkingDirectory(string path)
        {
            WorkingDirectory = MainPath.GetShortcutWorkingDir(path);
        }

        public string GetFullIconPath(string groupName)
        {
            if(Path.IsPathRooted(IconPath))
                return IconPath;

            return MainPath.Config + IconPath;
        }

        public void Init()
        {
            switch(type)
            {
                case ShortcutType.UWP:
                    name = handleWindowsApp.findWindowsAppsName(FilePath);
                    break;

                case ShortcutType.Directory:
                    string? DirName = System.IO.Path.GetDirectoryName(FilePath);
                    name = DirName != null ? new DirectoryInfo(DirName).Name : "";
                    break;

                case ShortcutType.URI:
                case ShortcutType.URL:
                    break;


                default:
                    name = Path.GetFileNameWithoutExtension(FilePath);
                    break;
            }    
        }
    }
}
