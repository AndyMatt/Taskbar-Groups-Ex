using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using TaskbarGroupsEx;
using TaskbarGroupsEx.Classes;
using TaskbarGroupsEx.Forms;

namespace TaskbarGroupsEx
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            TaskbarGroupsEx.Classes.MainPath.path = Path.GetFullPath(new Uri(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase)).LocalPath);
            TaskbarGroupsEx.Classes.MainPath.exeString = Path.GetFullPath(new Uri(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath);
        }

        public static string[] arguments = Environment.GetCommandLineArgs();

        // Define functions to set AppUserModelID
        [DllImport("shell32.dll", SetLastError = true)]
        static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
        public static Point GetMousePosition()
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);

            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        [STAThread]
        public void EntryPoint(object sender, StartupEventArgs e)
        {
            ProfileOptimization.SetProfileRoot(MainPath.path + "\\JITComp");

            // Use existing methods to obtain cursor already imported as to not import any extra functions
            // Pass as two variables instead of Point due to Point requiring System.Drawing
            Point MousePos = GetMousePosition();

            // Set the MainPath to the absolute path where the exe is located
            MainPath.path = Path.GetFullPath(new Uri(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase)).LocalPath);
            MainPath.exeString = Path.GetFullPath(new Uri(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath);

            // Creats folder for JIT compilation 
            Directory.CreateDirectory($"{MainPath.path}\\JITComp");

            // Creates directory in case it does not exist for config files
            Directory.CreateDirectory($"{MainPath.path}\\config");
            Directory.CreateDirectory($"{MainPath.path}\\Shortcuts");

            /*
            try
            {
                System.IO.File.Create(MainPath.path + "\\directoryTestingDocument.txt").Close();
                System.IO.File.Delete(MainPath.path + "\\directoryTestingDocument.txt");
            }
            catch
            {
                using (Process configTool = new Process())
                {
                    configTool.StartInfo.FileName = MainPath.exeString;
                    configTool.StartInfo.Verb = "runas";
                    try
                    {
                        configTool.Start();
                    } catch
                    {
                        Process.GetCurrentProcess().Kill();
                    }
                }
            }*/

            if (arguments.Length > 1) // Checks for additional arguments; opens either main application or taskbar drawer application
            {
                // Sets the AppUserModelID to tjackenpacken.taskbarGroup.menu.groupName
                // Distinguishes each shortcut process from one another to prevent them from stacking with the main application
                SetCurrentProcessExplicitAppUserModelID("tjackenpacken.taskbarGroup.menu."+ arguments[1]);

                new frmMain(arguments[1], MousePos).ShowDialog();
            } else
            {
                // See comment above
                SetCurrentProcessExplicitAppUserModelID("tjackenpacken.taskbarGroup.main");
                new frmClient().ShowDialog();
            }
        }


    }

}
