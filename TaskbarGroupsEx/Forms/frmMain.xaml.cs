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
        public Panel shortcutPanel;



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
            newSetLocation();
            //SetLocation();
        }

        private void newSetLocation()
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
                IUIAutomationElementArray elementArray = null;
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
                        }
                            //Console.WriteLine("\tName : {0} - AutomationId : {1}  - Rect({2}, {3}, {4}, {5})", sName, sAutomationId, rect.left, rect.top, rect.right, rect.bottom);
                    }
                }
            }
        }
        // Sets location of form
        private void SetLocation()
        {
            List<System.Windows.Shapes.Rectangle> taskbarList = FindDockedTaskBars();
            Rect taskbar = new Rect();
            Rect screen = new Rect();
            Rect MouseCursor = new Rect(mouseClick.X, mouseClick.Y, 1.0, 1.0);

            int i = 0;
            double locationy;
            double locationx;
            if (taskbarList.Count != 0)
            {
                foreach (Screen scr in Screen.AllScreens) // Get what screen user clicked on
                {
                    if (scr.Bounds.Contains(mouseClick.X, mouseClick.Y))
                    {
                        screen.X = Convert.ToInt32(scr.Bounds.X);
                        screen.Y = Convert.ToInt32(scr.Bounds.Y);
                        screen.Width = Convert.ToInt32(scr.Bounds.Width);
                        screen.Height = Convert.ToInt32(scr.Bounds.Height);
                        taskbar = new Rect(taskbarList[i].Margin.Left, taskbarList[i].Margin.Top, taskbarList[i].Width, taskbarList[i].Height);
                    }
                    i++;
                }

                if (Rect.Intersect(taskbar, MouseCursor) != Rect.Empty) // Click on taskbar
                {
                    if (taskbar.Top == screen.Top && taskbar.Width == screen.Width)
                    {
                        // TOP
                        locationy = screen.Y + taskbar.Height + 10;
                        locationx = mouseClick.X - (pnlShortcutIcons.Width / 2.0);
                    }
                    else if (taskbar.Bottom == screen.Bottom && taskbar.Width == screen.Width)
                    {
                        // BOTTOM
                        locationy = screen.Y + screen.Height - Convert.ToInt32(pnlShortcutIcons.Height) - taskbar.Height - 10;
                        locationx = mouseClick.X - (pnlShortcutIcons.Width / 2.0);
                    }
                    else if (taskbar.Left == screen.Left)
                    {
                        // LEFT
                        locationy = mouseClick.Y - (pnlShortcutIcons.Height / 2);
                        locationx = screen.X + taskbar.Width + 10;

                    }
                    else
                    {
                        // RIGHT
                        locationy = mouseClick.Y - (pnlShortcutIcons.Height / 2);
                        locationx = screen.X + screen.Width - pnlShortcutIcons.Width - taskbar.Width - 10;
                    }

                }
                else // not click on taskbar
                {
                    locationy = mouseClick.Y - pnlShortcutIcons.Height - 20;
                    locationx = mouseClick.X - (pnlShortcutIcons.Width / 2);

                }

                //this.Location = new Point(locationx, locationy);
                this.Left = locationx;
                this.Top = locationy;

                // If form goes over screen edge
                if (this.Left < screen.Left)
                    this.Left = screen.Left + 10;
                if (this.Top < screen.Top)
                    this.Top = screen.Top + 10;
                if (this.Right > screen.Right)
                    this.Left = screen.Right - this.Width - 10;

                // If form goes over taskbar
                if (taskbar.Contains(Convert.ToInt32(this.Left), Convert.ToInt32(this.Top)) && taskbar.Contains(Convert.ToInt32(this.Right), Convert.ToInt32(this.Top))) // Top taskbar
                    this.Top = screen.Top + 10 + taskbar.Height;
                if (taskbar.Contains(Convert.ToInt32(this.Left), Convert.ToInt32(this.Top))) // Left taskbar
                    this.Left = screen.Left + 10 + taskbar.Width;
                if (taskbar.Contains(Convert.ToInt32(this.Right), Convert.ToInt32(this.Top)))  // Right taskbar
                    this.Left = screen.Right - this.Width - 10 - taskbar.Width;

            }
            else // Hidded taskbar
            {
                foreach (var scr in Screen.AllScreens) // get what screen user clicked on
                {
                    if (scr.Bounds.Contains(Convert.ToDouble(mouseClick.X), Convert.ToDouble(mouseClick.Y)))
                    {

                        screen.X = Convert.ToInt32(scr.Bounds.X);
                        screen.Y = Convert.ToInt32(scr.Bounds.Y);
                        screen.Width = Convert.ToInt32(scr.Bounds.Width);
                        screen.Height = Convert.ToInt32(scr.Bounds.Height);
                    }
                    i++;
                }

                if (mouseClick.Y > Screen.PrimaryScreen.Bounds.Height - 35)
                    locationy = Screen.PrimaryScreen.Bounds.Height - this.Height - 45;
                else
                    locationy = mouseClick.Y - this.Height - 20;
                locationx = mouseClick.X - (this.Width / 2);

                //this.Location = new Point(locationx, locationy);
                this.Left = locationx;
                this.Top = locationy;

                // If form goes over screen edge
                if (this.Left < screen.Left)
                    this.Left = screen.Left + 10;
                if (this.Top < screen.Top)
                    this.Top = screen.Top + 10;
                if (this.Right > screen.Right)
                    this.Left = screen.Right - this.Width - 10;

                // If form goes over taskbar
                if (taskbar.Contains(Convert.ToInt32(this.Left), Convert.ToInt32(this.Top)) && taskbar.Contains(Convert.ToInt32(this.Right), Convert.ToInt32(this.Top))) // Top taskbar
                    this.Top = screen.Top + 10 + taskbar.Height;
                if (taskbar.Contains(Convert.ToInt32(this.Left), Convert.ToInt32(this.Top))) // Left taskbar
                    this.Left = screen.Left + 10 + taskbar.Width;
                if (taskbar.Contains(Convert.ToInt32(this.Right), Convert.ToInt32(this.Top)))  // Right taskbar
                    this.Left = screen.Right - this.Width - 10 - taskbar.Width;
            }
        }
        // Search for active taskbars on screen
        public static List<System.Windows.Shapes.Rectangle> FindDockedTaskBars()
        {
            List<System.Windows.Shapes.Rectangle> dockedRects = new List<System.Windows.Shapes.Rectangle>();
            foreach (var tmpScrn in Screen.AllScreens)
            {
                if (!tmpScrn.Bounds.Equals(tmpScrn.WorkingArea))
                {
                    System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();

                    var leftDockedWidth = Math.Abs((Math.Abs(tmpScrn.Bounds.Left) - Math.Abs(tmpScrn.WorkingArea.Left)));
                    var topDockedHeight = Math.Abs((Math.Abs(tmpScrn.Bounds.Top) - Math.Abs(tmpScrn.WorkingArea.Top)));
                    var rightDockedWidth = ((tmpScrn.Bounds.Width - leftDockedWidth) - tmpScrn.WorkingArea.Width);
                    var bottomDockedHeight = ((tmpScrn.Bounds.Height - topDockedHeight) - tmpScrn.WorkingArea.Height);
                    if ((leftDockedWidth > 0))
                    {
                        rect.Margin = new Thickness(tmpScrn.Bounds.Left,tmpScrn.Bounds.Top,0,0);
                        rect.Width = leftDockedWidth;
                        rect.Height = tmpScrn.Bounds.Height;
                    }
                    else if ((rightDockedWidth > 0))
                    {
                        rect.Margin = new Thickness(tmpScrn.WorkingArea.Right, tmpScrn.Bounds.Top, 0, 0);
                        rect.Width = rightDockedWidth;
                        rect.Height = tmpScrn.Bounds.Height;
                    }
                    else if ((topDockedHeight > 0))
                    {
                        rect.Margin = new Thickness(tmpScrn.WorkingArea.Left, tmpScrn.Bounds.Top, 0, 0);
                        rect.Width = tmpScrn.WorkingArea.Width;
                        rect.Height = topDockedHeight;
                    }
                    else if ((bottomDockedHeight > 0))
                    {
                        rect.Margin = new Thickness(tmpScrn.WorkingArea.Left, tmpScrn.WorkingArea.Bottom, 0, 0);
                        rect.Width = tmpScrn.WorkingArea.Width;
                        rect.Height = bottomDockedHeight;
                    }
                    else
                    {
                        // Nothing found!
                    }

                    dockedRects.Add(rect);
                }
            }

            return dockedRects;
        }
        //
        //------------------------------------------------------------------------------------
        //

        // Loading category and building shortcuts
        private void LoadCategory()
        {
            // Check if icon caches exist for the category being loaded
            // If not then rebuild the icon cache
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
            //this.Close();
            this.Visibility = Visibility.Hidden;
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
            //System.Diagnostics.Debugger.Launch();
            if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.Enter && fgConfig.allowOpenAll)
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
        //
        // endregion
        //

        private void Window_Activated(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Visible;
            NativeMethods.GlobalActivate(this);
            //this.Show();
            //this.WindowState = WindowState.Normal;
        }

        //
        // END OF CLASS
        //
    }
}
