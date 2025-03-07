using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TaskbarGroupsEx.Classes
{
    // Function that is accessed by all forms to get the starting absolute path of the .exe
    // Added as to not keep generating the path in each form
    static class MainPath
    {
        static MainPath()
        {
            _exeString = Environment.ProcessPath;
            _path = Path.GetDirectoryName(_exeString) + "\\";
            JITComp = CreateSubDirectory(JITComp);
            Config = CreateSubDirectory(Config);
            Shortcuts = CreateSubDirectory(Shortcuts);
        }

        static string CreateSubDirectory(string subdir)
        {
            string newPath = _path + subdir;
            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }
            return newPath;
        }

        private static String? _path;
        private static String? _exeString;
        private static String JITComp = "\\JITComp\\";
        public static String Config = "\\config\\";
        public static String Shortcuts = "\\Shortcuts\\";

        public static string GetPath()
        {
            return _path != null ? _path : "";
        }

        public static string GetExecutablePath()
        {
            return _exeString != null ? _exeString : "";
        }

        public static string GetJitPath()
        {
            return JITComp != null ? JITComp : "";
        }

        public static string GetConfigPath()
        {
            return Config != null ? Config : "";
        }

        public static string GetAssemblyVersion()
        {
            Assembly? _assembly = Assembly.GetEntryAssembly();
            if (_assembly != null)
            {
                if (_assembly.GetName() != null)
                {
                    AssemblyName? assName = _assembly.GetName();
                    if (assName.Version != null)
                    {
                        Version ver = assName.Version;
                        string VersionStr = (ver.ToString());
                        while(VersionStr.EndsWith(".0"))
                        {
                            VersionStr = VersionStr.Substring(0, VersionStr.Length - 2);
                        }
                        return VersionStr;
                    }
                }
            }

            return "0.0";
        }

        public static String GetShortcutWorkingDir(String file)
        {
            string? dirName = "";

            try
            {
                if (System.IO.Path.GetExtension(file).ToLower() == ".lnk")
                {
                    dirName = System.IO.Path.GetDirectoryName(ShellLink.GetShortcutWorkingDir(file));
                }
                else
                {
                    dirName = System.IO.Path.GetDirectoryName(file);
                }
            }
            catch { }

            return dirName != null ? dirName : "";
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

        public static string ParseGuidInPath(string filePath)
        {
            Match matches = Regex.Match(filePath, @"{[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}}", RegexOptions.IgnoreCase);
            if (!matches.Success)
            {
                return filePath;
            }

            Guid folderGuid = new Guid(matches.Value);

            IntPtr pPath;
            int result = SHGetKnownFolderPath(folderGuid, 0, IntPtr.Zero, out pPath);

            if (result >= 0)
            {
                string? knownFolder = Marshal.PtrToStringUni(pPath);
                Marshal.FreeCoTaskMem(pPath);
                if (knownFolder != null)
                {
                    string fullPath = filePath.Replace(matches.Value, knownFolder);
                    return fullPath;
                }
            }
            else
            {
                throw new ExternalException("Unable to retrieve the known folder path.", result);
            }
            return filePath;
        }

       
    }
}
