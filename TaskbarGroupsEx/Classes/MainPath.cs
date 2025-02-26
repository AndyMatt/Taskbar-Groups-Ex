using System;
using System.IO;

namespace TaskbarGroupsEx.Classes
{
    // Function that is accessed by all forms to get the starting absolute path of the .exe
    // Added as to not keep generating the path in each form
    static class MainPath
    {
        static MainPath()
        {
            exeString = Environment.ProcessPath;
            path = Path.GetDirectoryName(exeString) + "\\";
            JITComp = CreateSubDirectory(JITComp);
            Config = CreateSubDirectory(Config);
            Shortcuts = CreateSubDirectory(Shortcuts);
        }

        static string CreateSubDirectory(string subdir)
        {
            string newPath = path + subdir;
            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }
            return newPath;
        }

        public static String? path;
        public static String? exeString;
        public static String JITComp = "\\JITComp\\";
        public static String Config = "\\config\\";
        public static String Shortcuts = "\\Shortcuts\\";
    }
}
