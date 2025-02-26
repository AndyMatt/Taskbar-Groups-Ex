using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TaskbarGroupsEx.Classes;
using TaskbarGroupsEx.Forms;

namespace TaskbarGroupsEx.User_Controls
{
    public partial class ucShortcut : UserControl
    {
        public ProgramShortcut? Psc = null;
        public frmMain? MotherForm = null;
        public Category? ThisCategory = null;
        public ucShortcut()
        {
            InitializeComponent();
        }

        private void ucShortcut_Load(object sender, RoutedEventArgs e)
        {
            if (ThisCategory != null && Psc != null)
            {
                picIcon.Source = ThisCategory.loadImageCache(Psc); // Use the local icon cache for the file specified as the icon image
                picBG.Source = ThisCategory.loadImageCache(Psc);
            }
        }

        private void ucShortcut_Click(object sender, MouseButtonEventArgs e)
        {
            ucShortcut_OnClick();
        }

        public void ucShortcut_OnClick()
        {
            if (Psc == null)
                return;

            if(Psc.isWindowsApp)
            {
                Process p = new Process() { StartInfo = new ProcessStartInfo() { UseShellExecute = true, FileName = $@"shell:appsFolder\{Psc.FilePath}" } };
                p.Start();
            }
            else
            {
                if(MotherForm != null)
                {
                    if (System.IO.Path.GetExtension(Psc.FilePath).ToLower() == ".lnk" && Psc.FilePath == MainPath.exeString)
                    {
                        MotherForm.OpenFile(Psc.Arguments, Psc.FilePath, MainPath.path);
                    }
                    else
                    {
                        MotherForm.OpenFile(Psc.Arguments, Psc.FilePath, Psc.WorkingDirectory);
                    }
                }
            }
        }

        private void ucShortcut_MouseEnter(object sender, MouseEventArgs e)
        {
            ucShortcut_OnMouseEnter();
        }

        public void ucShortcut_OnMouseEnter()
        {
            picBG.Visibility = Visibility.Visible;
        }

        private void ucShortcut_MouseLeave(object sender, MouseEventArgs e)
        {
            ucShortcut_OnMouseLeave();
        }

        public void ucShortcut_OnMouseLeave()
        {
            picBG.Visibility = Visibility.Hidden;
        }
    }
}
