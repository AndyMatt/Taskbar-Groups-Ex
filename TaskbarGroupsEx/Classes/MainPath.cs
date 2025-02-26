using System;
using System.IO;
using System.Reflection;

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
                        return assName.Version.ToString();
                    }
                }
            }

            return new Version(0, 0, 0, 0).ToString();
        }
    }
}
