namespace TaskbarGroupsEx.Classes
{
    public class ProgramShortcut
    {
        public string FilePath { get; set; } = "";
        public bool isWindowsApp { get; set; }

        public string name { get; set; } = "";
        public string Arguments = "";
        public string WorkingDirectory = MainPath.GetPath();
     }
}
