using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TaskbarGroupsEx.Classes;
using TaskbarGroupsEx.Forms;

namespace TaskbarGroupsEx.User_Controls
{
    public partial class ucShortcut : UserControl
    {
        public ProgramShortcut? Psc = null;
        public frmMain? MotherForm = null;
        public FolderGroupConfig? ThisCategory = null;
        public ucShortcut()
        {
            InitializeComponent();
        }

        private void ucShortcut_Load(object sender, RoutedEventArgs e)
        {
            if (ThisCategory != null && Psc != null)
            {
                selectionCursor.Source = picIcon.Source = ThisCategory.loadImageCache(Psc); // Use the local icon cache for the file specified as the icon image
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

            if(Psc.type == ShortcutType.UWP) {
                Process p = new Process() { StartInfo = new ProcessStartInfo() { UseShellExecute = true, FileName = $@"shell:appsFolder\{Psc.FilePath}" } };
                p.Start();
            }
            else
            {
                if(MotherForm != null)
                {
                    if (System.IO.Path.GetExtension(Psc.FilePath).ToLower() == ".lnk" && Psc.FilePath == MainPath.GetExecutablePath())
                    {
                        MotherForm.OpenFile(Psc.Arguments, Psc.FilePath, MainPath.GetPath());
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
            selectionCursor.Visibility = Visibility.Visible;
        }

        private void ucShortcut_MouseLeave(object sender, MouseEventArgs e)
        {
            ucShortcut_OnMouseLeave();
        }

        public void ucShortcut_OnMouseLeave()
        {
            selectionCursor.Visibility = Visibility.Hidden;
        }
    }
}
