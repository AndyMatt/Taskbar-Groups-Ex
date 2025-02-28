using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TaskbarGroupsEx.Classes;

namespace TaskbarGroupsEx
{
    /// <summary>
    /// Interaction logic for ucProgramShortcut.xaml
    /// </summary>
    public partial class ucProgramShortcut : UserControl
    {
        public ProgramShortcut? Shortcut { get; set; }
        public frmGroup? MotherForm { get; set; }
        public int Position { get; set; }
        public int Index = -1;

        public BitmapSource? logo;
        public ucProgramShortcut()
        {
            InitializeComponent();
        }

        private void ucProgramShortcut_Loaded(object sender, RoutedEventArgs e)
        {
            if (Shortcut == null)
                return;

            if (Shortcut.isWindowsApp)
            {
                txtShortcutName.Text = handleWindowsApp.findWindowsAppsName(Shortcut.FilePath);
            } else if (Shortcut.name == "")
            {
                if (File.Exists(Shortcut.FilePath) && System.IO.Path.GetExtension(Shortcut.FilePath).ToLower() == ".lnk")
                {
                    txtShortcutName.Text = frmGroup.handleExtName(Shortcut.FilePath);
                }
                else
                {
                    txtShortcutName.Text = System.IO.Path.GetFileNameWithoutExtension(Shortcut.FilePath);
                }
            } else
            {
                txtShortcutName.Text = Shortcut.name;
            }

            if (Shortcut.isWindowsApp)
            {
                picShortcut.Source = handleWindowsApp.getWindowsAppIcon(Shortcut.FilePath, true);
            }
            else if (File.Exists(Shortcut.FilePath)) // Checks if the shortcut actually exists; if not then display an error image
            {
                String imageExtension = System.IO.Path.GetExtension(Shortcut.FilePath).ToLower();

                // Start checking if the extension is an lnk (shortcut) file
                // Depending on the extension, the icon can be directly extracted or it has to be gotten through other methods as to not get the shortcut arrow
                if (imageExtension == ".lnk")
                {
                    logo = frmGroup.handleLnkExt(Shortcut.FilePath);
                }
                else
                {
                    logo = ImageFunctions.ExtractIconToBitmapSource(Shortcut.FilePath);
                }
                picShortcut.Source = logo;
            }
            else if (Directory.Exists(Shortcut.FilePath))
            {
                try
                {
                    picShortcut.Source = logo = ImageFunctions.ExtractIconToBitmapSource(Shortcut.FilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                logo = (BitmapImage)Application.Current.Resources["ErrorIcon"];
                picShortcut.Source = logo;
            }
        }

        private void ucProgramShortcut_MouseEnter(object sender, MouseEventArgs e)
        {
            ucSelected();
        }

        private void ucProgramShortcut_MouseLeave(object sender, MouseEventArgs e)
        {
            if (MotherForm != null && MotherForm.selectedShortcut != this)
            {
                ucDeselected();
            }
        }

        private void cmdNumUp_Click(object sender, RoutedEventArgs e)
        {
            cmdNumUp.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            if(MotherForm != null)
                MotherForm.RepositionControl(this, -1);
            e.Handled = true;
        }

        private void cmdNumDown_Click(object sender, RoutedEventArgs e)
        {
            cmdNumDown.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            if(MotherForm != null)
                MotherForm.RepositionControl(this, 1);
            e.Handled = true;
        }

        private void cmdDelete_Click(object sender, RoutedEventArgs e)
        {
            if(MotherForm != null && Shortcut != null)
                MotherForm.DeleteShortcut(Shortcut);
        }

        // Handle what is selected/deselected when a shortcut is clicked on
        // If current item is already selected, then deselect everything
        private void ucProgramShortcut_Click(object sender, MouseButtonEventArgs e)
        {
            if (MotherForm != null)
            {
                if (MotherForm.selectedShortcut == this)
                {
                    MotherForm.resetSelection();
                }
                else
                {
                    if (MotherForm.selectedShortcut != null)
                    {
                        MotherForm.resetSelection();
                    }

                    MotherForm.enableSelection(this);
                }
            }
        }

        public void ucDeselected()
        {
            txtShortcutName.Select(0, 0);
            txtShortcutName.IsEnabled = txtShortcutName.IsTabStop = false;
            txtShortcutName.IsEnabled = true;

            this.Background = txtShortcutName.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 31, 31, 31));
        }

        public void ucSelected()
        {
            this.Background = txtShortcutName.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 56, 56, 56));
        }


        private void txtShortcutName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Shortcut != null)
                Shortcut.name = txtShortcutName.Text;
        }

        private void ucProgramShortcut_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                picShortcut.Focus();


                e.Handled = true;
            }
        }

        private void txtShortcutName_GotFocus(object sender, RoutedEventArgs e)
        {
            txtShortcutName.BorderThickness = new Thickness(1.0);
            if (MotherForm != null)
            {
                if (MotherForm.selectedShortcut == this)
                {
                    MotherForm.resetSelection();
                }
                else
                {
                    if (MotherForm.selectedShortcut != null)
                    {
                        MotherForm.resetSelection();
                    }

                    MotherForm.enableSelection(this);
                }
            }
        }  

        private void txtShortcutName_LostFocus(object sender, RoutedEventArgs e)
        {
            txtShortcutName.BorderThickness = new Thickness(0.0);
        }

        public void UpdateIndex(int index, bool isLast)
        {
            Index = index;
            if (index == 0)
            {
                cmdNumUp.IsEnabled = false;
                cmdNumUp.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 50, 50, 50));
            }
            else
            {
                cmdNumUp.IsEnabled = true;
                cmdNumUp.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            }
            
            if (isLast)
            {
                cmdNumDown.IsEnabled = false;
                cmdNumDown.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 50, 50, 50));
            }
            else
            {
                cmdNumDown.IsEnabled = true;
                cmdNumDown.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            }
        }

        private void cmdNum_MouseEnter(object sender, MouseEventArgs e)
        {
            Label? btn = sender as Label;
            if (btn != null)
            {
                if (btn.IsEnabled)
                {
                    btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 125, 125, 125));
                }
            }
        }

        private void cmdNum_MouseLeave(object sender, MouseEventArgs e)
        {
            Label? btn = sender as Label;
            if (btn != null)
            {
                if (btn.IsEnabled)
                {
                    btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
                }
            }
        }
    }
}
