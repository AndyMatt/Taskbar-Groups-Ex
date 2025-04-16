using Microsoft.Win32;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TaskbarGroupsEx.Classes;
using TaskbarGroupsEx.GroupItems;
using Windows.Management.Deployment;

namespace TaskbarGroupsEx
{
    /// <summary>
    /// Interaction logic for ucProgramShortcut.xaml
    /// </summary>
    public partial class ucProgramShortcut : UserControl
    {
        public DynamicGroupItem? GroupItem { get; set; }
        public frmGroup? MotherForm { get; set; }
        public int Position { get; set; }
        public int Index = -1;

        public ucProgramShortcut()
        {
            InitializeComponent();
        }

        private void ucProgramShortcut_Loaded(object sender, RoutedEventArgs e)
        {
            if (GroupItem == null)
                return;

            txtShortcutName.Content = GroupItem.mName;
            picShortcut.Source = GroupItem.GetIcon();
        }

        private void ucProgramShortcut_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Background = txtShortcutName.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 56, 56, 56));
        }

        private void ucProgramShortcut_MouseLeave(object sender, MouseEventArgs e)
        {
            if (MotherForm != null && MotherForm.selectedShortcut != this)
            {
                this.Background = txtShortcutName.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 31, 31, 31));
            }
        }

        private void cmdNumUp_Click(object sender, RoutedEventArgs e)
        {
            cmdNumUp.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            if (MotherForm != null)
                MotherForm.RepositionControl(this, -1);
            e.Handled = true;
        }

        private void cmdNumDown_Click(object sender, RoutedEventArgs e)
        {
            cmdNumDown.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            if (MotherForm != null)
                MotherForm.RepositionControl(this, 1);
            e.Handled = true;
        }

        private void cmdDelete_Click(object sender, RoutedEventArgs e)
        {
            if (MotherForm != null && GroupItem != null)
                MotherForm.DeleteGroupItem(GroupItem);
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
            this.Background = txtShortcutName.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 31, 31, 31));
            ItemBorder.BorderThickness = new Thickness(0.0f);
        }

        public void ucSelected()
        {
            this.Background = txtShortcutName.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 56, 56, 56));
            ItemBorder.BorderThickness = new Thickness(1.0f);
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

        public BitmapSource OnChangeIcon()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Title = "Select Item Icon",
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "img",
                Filter = "Image files (*.png, *.ico) | *.png; *.ico; ",
                FilterIndex = 2,
                RestoreDirectory = true,
                ReadOnlyChecked = true,
                DereferenceLinks = false,
            };

            if (openFileDialog.ShowDialog() == true)
            {
                String imageExtension = System.IO.Path.GetExtension(openFileDialog.FileName).ToLower();
                try
                {
                    if (GroupItem != null)
                    {
                        GroupItem.LoadIconFromFile(openFileDialog.FileName);
                        picShortcut.Source = GroupItem.GetIcon();
                    }
                }
                catch
                {
                    MessageBox.Show($"Issue setting {Path.GetFileName(openFileDialog.FileName)} as Icon");
                }
            }
            return GroupItem.GetIcon();
        }

        public void OnNameChanged(string name)
        {
            if (GroupItem != null)
            {
                GroupItem.mName = name;
                txtShortcutName.Content = name;
            }            
        }

        public void OnCommandChanged(string command)
        {
            if (GroupItem != null)
            {
                GroupItem.mCommand = command;
            }
        }

        public void OnWorkingDirChanged(string workingDir)
        {
            if (GroupItem != null && GroupItem is ApplicationGroupItem)
            {
                ((ApplicationGroupItem)GroupItem).mWorkingDirectory = workingDir;
            }
        }

        public void OnArgsChanged(string args)
        {
            if (GroupItem != null && GroupItem is ApplicationGroupItem)
            {
                ((ApplicationGroupItem)GroupItem).mArguments = args;
            }
        }
    }
}
