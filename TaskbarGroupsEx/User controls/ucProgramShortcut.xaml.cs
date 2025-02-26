using System;
using System.Collections.Generic;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TaskbarGroupsEx.Classes;
using TaskbarGroupsEx.User_Controls;

namespace TaskbarGroupsEx
{
    /// <summary>
    /// Interaction logic for ucProgramShortcut.xaml
    /// </summary>
    public partial class ucProgramShortcut : UserControl
    {
        public ProgramShortcut Shortcut { get; set; }
        public frmGroup MotherForm { get; set; }

        public bool IsSelected = false;
        public int Position { get; set; }
        public int Index = -1;

        public BitmapSource logo;
        public ucProgramShortcut()
        {
            InitializeComponent();
        }

        private void ucProgramShortcut_Loaded(object sender, RoutedEventArgs e)
        {
            // Grab the file name without the extension to be used later as the naming scheme for the icon .jpg image

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

            /*
            if (Position == 0)
            {
                cmdNumUp.IsEnabled = false;
                cmdNumUp.Foreground  = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 23, 23, 23));
            }
            if (Position == MotherForm.Category.ShortcutList.Count - 1)
            {
                cmdNumDown.IsEnabled = false;
                cmdNumUp.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 23, 23, 23));

            }
            */
        }

        private void ucProgramShortcut_MouseEnter(object sender, MouseEventArgs e)
        {
            ucSelected();
        }

        private void ucProgramShortcut_MouseLeave(object sender, MouseEventArgs e)
        {
            if (MotherForm.selectedShortcut != this)
            {
                ucDeselected();
            }
        }

        private void cmdNumUp_Click(object sender, RoutedEventArgs e)
        {
            cmdNumUp.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            MotherForm.RepositionControl(this, -1);
            e.Handled = true;
        }

        private void cmdNumDown_Click(object sender, RoutedEventArgs e)
        {
            cmdNumDown.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            MotherForm.RepositionControl(this, 1);
            e.Handled = true;
        }

        private void cmdDelete_Click(object sender, RoutedEventArgs e)
        {
            MotherForm.DeleteShortcut(Shortcut);
        }

        // Handle what is selected/deselected when a shortcut is clicked on
        // If current item is already selected, then deselect everything
        private void ucProgramShortcut_Click(object sender, MouseButtonEventArgs e)
        {
            if (MotherForm.selectedShortcut == this)
            {
                MotherForm.resetSelection();
                //IsSelected = false;
            }
            else
            {
                if (MotherForm.selectedShortcut != null)
                {
                    MotherForm.resetSelection();
                    //IsSelected = false;
                }

                MotherForm.enableSelection(this);
               //IsSelected = true;
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

            if (MotherForm.selectedShortcut == this)
            {
                MotherForm.resetSelection();
                //IsSelected = false;
            }
            else
            {
                if (MotherForm.selectedShortcut != null)
                {
                    MotherForm.resetSelection();
                    //IsSelected = false;
                }

                MotherForm.enableSelection(this);
                // IsSelected = true;
            }
        }

        private void ucProgramShortcut_Enter(object sender, RoutedEventArgs e)
        {
            //IsSelected = true;
        }

        private void ucProgramShortcut_Leave(object sender, RoutedEventArgs e)
        {
            //IsSelected = false;
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
