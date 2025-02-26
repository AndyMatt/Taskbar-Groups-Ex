using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using TaskbarGroupsEx.Classes;
using TaskbarGroupsEx.User_Controls;
using Windows.Storage.Search;
using WpfScreenHelper;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using Interop.UIAutomationClient;
using System.Text.RegularExpressions;


namespace TaskbarGroupsEx.Forms
{
    /// <summary>
    /// Interaction logic for frmMain.xaml
    /// </summary>
    public partial class frmMain : Window
    {
        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

        public FolderGroupConfig? fgConfig;
        public List<ucShortcut> ControlList = new List<ucShortcut>();
        public System.Windows.Media.Color HoverColor;

        private string mShortcutName;
        private string mPath;
        public System.Windows.Point mouseClick;


        public double Right
        {
            get { return this.Left + this.Width; }
        }

        //------------------------------------------------------------------------------------
        // CTOR AND LOAD
        //
        public frmMain(string ShortcutName)
        {
            NativeMethods.SetCurrentProcessExplicitAppUserModelID("tjackenpacken.taskbarGroup.menu." + ShortcutName);
            System.Runtime.ProfileOptimization.StartProfile("frmMain.Profile");

            InitializeComponent();

            mShortcutName = ShortcutName;
            mPath = MainPath.Config + mShortcutName;
            mouseClick = new System.Windows.Point(0, 0);
            this.WindowStyle = WindowStyle.None;


            if (Directory.Exists(mPath))
            {
                this.Icon = ImageFunctions.ExtractIconToBitmapSource(mPath + "\\GroupIcon.ico");

                ControlList = new List<ucShortcut>();
                fgConfig = FolderGroupConfig.ParseConfiguration(mPath);
                bdrMain.Background = new SolidColorBrush(fgConfig.CatagoryBGColor);
                System.Windows.Media.Color BorderColor = System.Windows.Media.Color.FromArgb(fgConfig.CatagoryBGColor.A, 37, 37, 37);
                bdrMain.BorderBrush = new SolidColorBrush(BorderColor);
                
                HoverColor = System.Windows.Media.Color.Multiply(fgConfig.CatagoryBGColor, 3.0f);
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void frmMain_Load(object sender, RoutedEventArgs e)
        {
            LoadCategory();
            SetLocation();
        }

        private void SetLocation()
        {
            IntPtr hWndTray = FindWindow("Shell_TrayWnd", null);

            IntPtr hWndRebar = FindWindowEx(hWndTray, IntPtr.Zero, "ReBarWindow32", null);
            IntPtr hWndMSTaskSwWClass = FindWindowEx(hWndRebar, IntPtr.Zero, "MSTaskSwWClass", null);
            IntPtr hWndMSTaskListWClass = FindWindowEx(hWndMSTaskSwWClass, IntPtr.Zero, "MSTaskListWClass", null);

            IUIAutomation pUIAutomation = new CUIAutomation();

            // Taskbar
            IUIAutomationElement windowElement = pUIAutomation.ElementFromHandle(hWndMSTaskListWClass);
            if (windowElement != null)
            {
                IUIAutomationElementArray? elementArray = null;
                IUIAutomationCondition condition = pUIAutomation.CreateTrueCondition();
                elementArray = windowElement.FindAll(Interop.UIAutomationClient.TreeScope.TreeScope_Descendants | Interop.UIAutomationClient.TreeScope.TreeScope_Children, condition);
                if (elementArray != null)
                {
                    Console.WriteLine("Taskbar");
                    int nNbItems = elementArray.Length;
                    for (int nItem = 0; nItem <= nNbItems - 1; nItem++)
                    {
                        IUIAutomationElement element = elementArray.GetElement(nItem);
                        string sName = element.CurrentName;
                        string sAutomationId = element.CurrentAutomationId;
                        tagRECT rect = element.CurrentBoundingRectangle;
                        if (sAutomationId.Contains("tjackenpacken.taskbarGroup.menu."+ mShortcutName))
                        {
                            this.Left = rect.left + ((rect.right - rect.left) / 2) - (pnlShortcutIcons.Width/2);
                            this.Top = rect.top - (pnlShortcutIcons.Height);
                            return;
                        }
                    }
                }
            }

            //Fallback to Mice
            NativeMethods.Win32Point mousePos = NativeMethods.GetMousePosition();

            this.Left = mousePos.X; this.Top = mousePos.Y;
        }
        //
        //------------------------------------------------------------------------------------
        //

        // Loading category and building shortcuts
        private void LoadCategory()
        {
            if (fgConfig == null)
                return;

            if (!Directory.Exists(@MainPath.Config + fgConfig.GetName() + @"\Icons\"))
            {
                fgConfig.cacheIcons();
            }

            double columnCount = Math.Ceiling((double)fgConfig.ShortcutList.Count / fgConfig.CollumnCount);
            pnlShortcutIcons.Height = columnCount * 45 ;
            pnlShortcutIcons.Width = (fgConfig.CollumnCount * 55);

            foreach (ProgramShortcut psc in fgConfig.ShortcutList)
            {
                // Building shortcut controls
                ucShortcut pscPanel = new ucShortcut()
                {
                    Psc = psc,
                    MotherForm = this,
                    ThisCategory = fgConfig,
                };

                pnlShortcutIcons.Children.Add(pscPanel);
                this.ControlList.Add(pscPanel);
            }
        }

        // Click handler for shortcuts
        public void OpenFile(string arguments, string path, string workingDirec)
        {
            // starting program from psc panel click
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.Arguments = arguments;
            proc.FileName = path;
            proc.WorkingDirectory = workingDirec;
            proc.UseShellExecute = true;

            try
            {
                Process.Start(proc);
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }

        // Closes application upon deactivation
        private void frmMain_Deactivate(object sender, EventArgs e)
        {
            // closes program if user clicks outside form
            this.Close();
        }

        // Keyboard shortcut handlers
        private void frmMain_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key >= Key.D1 && e.Key <= Key.D0)
                {
                    int idx = e.Key - Key.D1;
                    ControlList[idx].ucShortcut_OnMouseEnter();
                }
            }
            catch{}
        }

        private void frmMain_KeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.Enter && fgConfig != null && fgConfig.allowOpenAll)
            {
                foreach (ucShortcut usc in this.ControlList)
                    usc.ucShortcut_OnClick();
            }

            try
            {
                if (e.Key >= Key.D1 && e.Key <= Key.D0)
                {
                    int idx = e.Key - Key.D1;
                    ControlList[idx].ucShortcut_OnMouseLeave();
                    ControlList[idx].ucShortcut_OnClick();
                }
            }              
            catch{}
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Visible;
            NativeMethods.GlobalActivate(this);
        }

        //
        // END OF CLASS
        //
    }
}
