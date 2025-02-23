using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
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

        static Dictionary<string, frmMain> cachedGroups = new Dictionary<string, frmMain>();

        [STAThread]
        public void EntryPoint(object sender, StartupEventArgs e)
        {
            while (!Debugger.IsAttached)
            {
                Thread.Sleep(1000);
                //NativeMethods.MessageBox("Waiting for Debugger");
            }

            ProfileOptimization.SetProfileRoot(MainPath.path + "\\JITComp");
            //this.Activated += AfterLoading;

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

            if (arguments.Length > 1 && arguments[1] != "/s") // Checks for additional arguments; opens either main application or taskbar drawer application
            {
                SetCurrentProcessExplicitAppUserModelID("tjackenpacken.taskbarGroup.menu." + arguments[1]);

                if (!cachedGroups.ContainsKey(arguments[1]))
                {
                    cachedGroups[arguments[1]] = new frmMain(arguments[1], MousePos);
                }

                cachedGroups[arguments[1]].Show();
                // Sets the AppUserModelID to tjackenpacken.taskbarGroup.menu.groupName
                // Distinguishes each shortcut process from one another to prevent them from stacking with the main application


            } else
            {
                // See comment above
                SetCurrentProcessExplicitAppUserModelID("tjackenpacken.taskbarGroup.main");
                new frmClient().Show();
            }
        }

        private void AfterLoading(object? sender, EventArgs e)
        {
            while(!MainWindow.IsInitialized)
            {
                Thread.Sleep(100);
            }
            IntPtr handle = new WindowInteropHelper(MainWindow).Handle;
            HwndSource source = HwndSource.FromHwnd(handle);
            source.AddHook(new HwndSourceHook(WndProc));
        }

        public struct COPYDATASTRUCT
        {
            public int cbData;
            public IntPtr dwData;
            [MarshalAs(UnmanagedType.LPStr)] public string lpData;
        }
        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x004A)
            {
                COPYDATASTRUCT cds = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                //byte[] managedArray = new byte[wParam];
                //Marshal.Copy(lParam, managedArray,0, (int)wParam);
                string arg = cds.lpData;

                if (!string.IsNullOrEmpty(arg))
                {
                    SetCurrentProcessExplicitAppUserModelID("tjackenpacken.taskbarGroup.menu." + arg);
                    if (!cachedGroups.ContainsKey(arg))
                    {
                        cachedGroups[arg] = new frmMain(arg, new Point(0, 0));
                    }

                    cachedGroups[arg].Show();

                }
                handled = true;
            }
            else if (msg == MSG_OPENSETTINGS)
            {
                //OpenClient();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private const int MSG_OPENTASKBARGROUP = 0xFF01;
        private const int MSG_OPENSETTINGS = 0xFF02;


    }

}
