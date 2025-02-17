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
using WpfScreenHelper;
using WpfScreenHelper.Enum;

namespace TaskbarGroupsEx.Forms
{
    /// <summary>
    /// Interaction logic for frmMain.xaml
    /// </summary>
    public partial class frmMain : Window
    {
        /*
        // Allow doubleBuffering drawing each frame to memory and then onto screen
        // Solves flickering issues mostly as the entire rendering of the screen is done in 1 operation after being first loaded to memory
        protected override CreateParams CreateParams

        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }
        */

        public Category ThisCategory;
        public List<ucShortcut> ControlList;
        public System.Windows.Media.Color HoverColor;

        private string passedDirec;
        public System.Windows.Point mouseClick;

        public double Right
        {
            get { return this.Left + this.Width; }
        }

        //------------------------------------------------------------------------------------
        // CTOR AND LOAD
        //
        public frmMain(string passedDirectory, System.Windows.Point cursorPos)
        {
            InitializeComponent();

            System.Runtime.ProfileOptimization.StartProfile("frmMain.Profile");
            passedDirec = passedDirectory;
            mouseClick = cursorPos;
            this.WindowStyle = WindowStyle.None;

            this.Icon = ImageFunctions.ExtractIconToBitmapSource(MainPath.path + "\\config\\" + passedDirec + "\\GroupIcon.ico");
            //using (MemoryStream ms = new MemoryStream(System.IO.File.ReadAllBytes()))
             //   this.Icon = new Icon(ms);

            if (Directory.Exists(@MainPath.path + @"\config\" + passedDirec))
            {
                ControlList = new List<ucShortcut>();

                //this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
                ThisCategory = new Category($"config\\{passedDirec}");
                bdrMain.Background = new SolidColorBrush(ThisCategory.CatagoryBGColor);
                System.Windows.Media.Color BorderColor = System.Windows.Media.Color.FromArgb(ThisCategory.CatagoryBGColor.A, 37, 37, 37);
                bdrMain.BorderBrush = new SolidColorBrush(BorderColor);
                //Opacity = (1 - (ThisCategory.Opacity / 100));

                //Should be reproducable with a multiple function
                /*
                if (ThisCategory.CatagoryBGColor.R * 0.2126 + ThisCategory.CatagoryBGColor.G * 0.7152 + ThisCategory.CatagoryBGColor.B * 0.0722 > 255 / 2)
                    //if backcolor is light, set hover color as darker
                    HoverColor = System.Windows.Media.Color.FromArgb(ThisCategory.CatagoryBGColor.A, (ThisCategory.CatagoryBGColor.R - 50), (ThisCategory.CatagoryBGColor.G - 50), (ThisCategory.CatagoryBGColor.B - 50));
                else
                    //light backcolor is light, set hover color as darker
                    HoverColor = Color.FromArgb(BackColor.A, (BackColor.R + 50), (BackColor.G + 50), (BackColor.B + 50));
                */
                HoverColor = System.Windows.Media.Color.Multiply(ThisCategory.CatagoryBGColor, 3.0f);
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

            if (dockedRects.Count == 0)
            {
                // Taskbar is set to "Auto-Hide".
            }

            return dockedRects;
        }
        //
        //------------------------------------------------------------------------------------
        //

        // Loading category and building shortcuts
        private void LoadCategory()
        {
            //System.Diagnostics.Debugger.Launch();

            //this.Width = 0;
            //this.Height = 45;
            int x = 0;
            int y = 0;
            int width = ThisCategory.Width;
            int columns = 1;

            // Check if icon caches exist for the category being loaded
            // If not then rebuild the icon cache
            if (!Directory.Exists(@MainPath.path + @"\config\" + ThisCategory.Name + @"\Icons\"))
            {
                ThisCategory.cacheIcons();
            }

            double columnCount = Math.Ceiling((double)ThisCategory.ShortcutList.Count / ThisCategory.Width);
            pnlShortcutIcons.Height = columnCount * 45 ;
            pnlShortcutIcons.Width = (ThisCategory.Width * 55);

            foreach (ProgramShortcut psc in ThisCategory.ShortcutList)
            {

                /*
                if (columns > width)  // creating new row if there are more psc than max width
                {
                    x = 0;
                    y += 45;
                    this.Height += 45;
                    columns = 1;
                }

                if (this.Width < ((width * 55)))
                    this.Width += (55);
                */
                // OLD
                //BuildShortcutPanel(x, y, psc);

                // Building shortcut controls
                ucShortcut pscPanel = new ucShortcut()
                {
                    Psc = psc,
                    MotherForm = this,
                    ThisCategory = ThisCategory,
                };
                //pscPanel.Location = new System.Drawing.Point(x, y);
                //pscPanel.Margin = new Thickness(x, y,0,0);
                pnlShortcutIcons.Children.Add(pscPanel);
                this.ControlList.Add(pscPanel);
                //pscPanel.Show();
                //pscPanel.BringToFront();

                // Reset values
                x += 55;
                //columns++;
            }

            //this.Width -= 2; // For some reason the width is 2 pixels larger than the shortcuts. Temporary fix
        }

        /*
        // OLD (Having some issues with the uc build, so keeping the old code below)
        private void BuildShortcutPanel(int x, int y, ProgramShortcut psc)
        {
            this.shortcutPic = new System.Windows.Forms.PictureBox();
            this.shortcutPic.BackColor = System.Drawing.Color.Transparent;
            this.shortcutPic.Location = new System.Drawing.Point(25, 15);
            this.shortcutPic.Size = new System.Drawing.Size(25, 25);
            this.shortcutPic.BackgroundImage = ThisCategory.loadImageCache(psc); // Use the local icon cache for the file specified as the icon image
            this.shortcutPic.BackgroundImageLayout = ImageLayout.Stretch;
            this.shortcutPic.TabStop = false;
            this.shortcutPic.Click += new System.EventHandler((sender, e) => OpenFile(psc.Arguments, psc.FilePath, psc.WorkingDirectory));
            this.shortcutPic.Cursor = System.Windows.Forms.Cursors.Hand;
            this.shortcutPanel.Controls.Add(this.shortcutPic);
            this.shortcutPic.Show();
            this.shortcutPic.BringToFront();
            this.shortcutPic.MouseEnter += new System.EventHandler((sender, e) => this.shortcutPanel.BackColor = Color.Black);
            this.shortcutPic.MouseLeave += new System.EventHandler((sender, e) => this.shortcutPanel.BackColor = System.Drawing.Color.Transparent);
        }
        */

        // Click handler for shortcuts
        public void OpenFile(string arguments, string path, string workingDirec)
        {
            // starting program from psc panel click
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.Arguments = arguments;
            proc.FileName = path;
            proc.WorkingDirectory = workingDirec;
            proc.UseShellExecute = true;

            /*
            proc.EnableRaisingEvents = false;
            proc.StartInfo.FileName = path;
            */

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
                switch (e.Key)
                {

                    case Key.D1:
                        ControlList[0].ucShortcut_OnMouseEnter();
                        break;
                    case Key.D2:
                        ControlList[1].ucShortcut_OnMouseEnter();
                        break;
                    case Key.D3:
                        ControlList[2].ucShortcut_OnMouseEnter();
                        break;
                    case Key.D4:
                        ControlList[3].ucShortcut_OnMouseEnter();
                        break;
                    case Key.D5:
                        ControlList[4].ucShortcut_OnMouseEnter();
                        break;
                    case Key.D6:
                        ControlList[5].ucShortcut_OnMouseEnter();
                        break;
                    case Key.D7:
                        ControlList[6].ucShortcut_OnMouseEnter();
                        break;
                    case Key.D8:
                        ControlList[7].ucShortcut_OnMouseEnter();
                        break;
                    case Key.D9:
                        ControlList[8].ucShortcut_OnMouseEnter();
                        break;
                    case Key.D0:
                        ControlList[9].ucShortcut_OnMouseEnter();
                        break;
                }
            }
            catch
            {

            }
        }

        private void frmMain_KeyUp(object sender, KeyEventArgs e)
        {
            //System.Diagnostics.Debugger.Launch();
            if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.Enter && ThisCategory.allowOpenAll)
            {
                foreach (ucShortcut usc in this.ControlList)
                    usc.ucShortcut_OnClick();
            }

            try
            {
                switch (e.Key)
                {
                    case Key.D1:
                        ControlList[0].ucShortcut_OnMouseLeave();
                        ControlList[0].ucShortcut_OnClick();
                        break;
                    case Key.D2:
                        ControlList[1].ucShortcut_OnMouseLeave();
                        ControlList[1].ucShortcut_OnClick();

                        break;
                    case Key.D3:
                        ControlList[2].ucShortcut_OnMouseLeave();
                        ControlList[2].ucShortcut_OnClick();
                        break;
                    case Key.D4:
                        ControlList[3].ucShortcut_OnMouseLeave();
                        ControlList[3].ucShortcut_OnClick();
                        break;
                    case Key.D5:
                        ControlList[4].ucShortcut_OnMouseLeave();
                        ControlList[4].ucShortcut_OnClick();
                        break;
                    case Key.D6:
                        ControlList[5].ucShortcut_OnMouseLeave();
                        ControlList[5].ucShortcut_OnClick();
                        break;
                    case Key.D7:
                        ControlList[6].ucShortcut_OnMouseLeave();
                        ControlList[6].ucShortcut_OnClick();
                        break;
                    case Key.D8:
                        ControlList[7].ucShortcut_OnMouseLeave();
                        ControlList[7].ucShortcut_OnClick();
                        break;
                    case Key.D9:
                        ControlList[8].ucShortcut_OnMouseLeave();
                        ControlList[8].ucShortcut_OnClick();
                        break;
                    case Key.D0:
                        ControlList[9].ucShortcut_OnMouseLeave();
                        ControlList[9].ucShortcut_OnClick();
                        break;
                }
            }
            catch
            {

            }
        }
        //
        // endregion
        //
        public System.Windows.Controls.Image shortcutPic;
        public Panel shortcutPanel;

        //
        // END OF CLASS
        //
    }
}
