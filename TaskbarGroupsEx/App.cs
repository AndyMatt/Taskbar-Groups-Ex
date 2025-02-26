using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Windows;
using TaskbarGroupsEx.Classes;
using TaskbarGroupsEx.Forms;

namespace TaskbarGroupsEx
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public static string[] arguments = Environment.GetCommandLineArgs();
        static Dictionary<string, frmMain> cachedGroups = new Dictionary<string, frmMain>();

        void WaitForDebugger()
        {
            while (!Debugger.IsAttached)
            {
                Thread.Sleep(1000);
            }
        }
        bool CheckWriteAccessToDirectory(string path)
        {
            String TempFilePath = path + Path.GetRandomFileName() + ".lock";
            try
            {
                FileStream Fs = File.Create(TempFilePath);
                Fs.Close();
                File.Delete(TempFilePath);
                return true; 
            }
            catch
            {
                return false;
            }
        }

        void RelauchAsAdmin()
        {
            Process configTool = new Process
            {
                StartInfo =
                {
                    FileName = MainPath.exeString,
                    Verb = "runas",
                    Arguments = String.Join(" ", arguments.Skip(1)),
                    UseShellExecute=true
                }
            };

            if(configTool.Start())
            {
                Process.GetCurrentProcess().Kill();
            }
        }

        [STAThread]
        public void EntryPoint(object sender, StartupEventArgs e)
        {
            ProfileOptimization.SetProfileRoot(MainPath.path + "\\JITComp");

            WaitForDebugger();

            if(MainPath.path != null && !CheckWriteAccessToDirectory(MainPath.path))
            {
                RelauchAsAdmin();
            }

            if (arguments.Length > 1) {
                new frmMain(arguments[1]).Show();
            } else {
                new frmClient().Show();
            }
        }
    }

}
