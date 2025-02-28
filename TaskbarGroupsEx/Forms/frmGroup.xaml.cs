using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using TaskbarGroupsEx.Classes;
using IWshRuntimeLibrary;

namespace TaskbarGroupsEx
{
    public partial class frmGroup : Window
    {
        public FolderGroupConfig? fgConfig;
        public frmClient Client;
        public bool IsNew;
        private String[] imageExt = new String[] { ".png", ".jpg", ".jpe", ".jfif", ".jpeg", };
        private String[] extensionExt = new String[] { ".exe", ".lnk", ".url" };
        private String[] specialImageExt = new String[] { ".ico", ".exe", ".lnk" };
        private String[] newExt = new String[]{};

        public ucProgramShortcut? selectedShortcut = null;

        public static Shell32.Shell shell = new Shell32.Shell();

        private List<ProgramShortcut> shortcutChanged = new List<ProgramShortcut>();

        //--------------------------------------
        // CTOR AND LOAD
        //--------------------------------------

        // CTOR for creating a new group
        public frmGroup(frmClient client, FolderGroupConfig? category = null)
        {
            System.Runtime.ProfileOptimization.StartProfile("frmGroup.Profile");

            InitializeComponent();

            Client = client;
            IsNew = category == null ? true : false;

            if (category == null)
            {
                newExt = imageExt.Concat(specialImageExt).ToArray();
                fgConfig = new FolderGroupConfig { ShortcutList = new List<ProgramShortcut>() };
                clmnDelete.Width = new GridLength(0.0);
                radioDark.IsChecked = true;
            }
            else
            {
                fgConfig = category;

                this.Title = "Edit group";
                pnlAllowOpenAll.IsChecked = fgConfig.allowOpenAll;
                cmdAddGroupIcon.Source = fgConfig.LoadIconImage();
                lblNum.Text = fgConfig.CollumnCount.ToString();
                txtGroupName.Text = Regex.Replace(fgConfig.GetName(), @"(_)+", " ");

                Color categoryColor = fgConfig.CatagoryBGColor;

                UpdateOpacityControls(Convert.ToInt32(((double)categoryColor.A) * 100.0 / 255.0));

                if (fgConfig.CatagoryBGColor == System.Windows.Media.Color.FromArgb(fgConfig.CatagoryBGColor.A, 31, 31, 31))
                {
                    radioDark.IsChecked = true;
                }
                else if (fgConfig.CatagoryBGColor == System.Windows.Media.Color.FromArgb(fgConfig.CatagoryBGColor.A, 230, 230, 230))
                {
                    radioLight.IsChecked = true;
                }
                else
                {
                    radioCustom.IsChecked = true;
                    pnlCustomColor.Visibility = Visibility.Visible;
                }

                // Loading existing shortcutpanels
                for (int i = 0; i < fgConfig.ShortcutList.Count; i++)
                {
                    LoadShortcut(fgConfig.ShortcutList[i], i);
                }
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            NativeMethods.WindowsUXHelper.ApplyWindowsImmersion(this);
        }

        //--------------------------------------
        // SHORTCUT PANEL HANLDERS
        //--------------------------------------

        // Load up shortcut panel
        public void LoadShortcut(ProgramShortcut psc, int position)
        {
            ucProgramShortcut ucPsc = new ucProgramShortcut()
            {
                MotherForm = this,
                Shortcut = psc,
                Position = position,
            };
            pnlShortcuts.Children.Add(ucPsc);           
            RefreshProgramControls();
        }

        private void pnlAddShortcut_Click(object sender, MouseButtonEventArgs e)
        {
            resetSelection();

            lblErrorShortcut.Visibility = Visibility.Hidden; // resetting error msg
            
            if (fgConfig != null && fgConfig.ShortcutList.Count >= 20)
            {
                lblErrorShortcut.Text = "Max 20 shortcuts in one group";
                //lblErrorShortcut.BringToFront();
                lblErrorShortcut.Visibility = Visibility.Visible;
            }


            OpenFileDialog openFileDialog = new OpenFileDialog // ask user to select exe file
            {
                InitialDirectory = @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs",
                Title = "Create New Shortcut",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = true,
                DefaultExt = "exe",
                Filter = "Exe or Shortcut (.exe, .lnk)|*.exe;*.lnk;*.url",
                RestoreDirectory = true,
                ReadOnlyChecked = true,
                DereferenceLinks = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (String file in openFileDialog.FileNames)
                {
                    addShortcut(file);
                }
                resetSelection();
            }

            if (pnlShortcuts.Children.Count != 0)
            {
                pnlScrollViewer.ScrollToBottom();
            }
            RefreshProgramControls();
        }
        private void pnlDragDropExt(object sender, DragEventArgs e)
        {
            String[]? files = (String[])e.Data.GetData(DataFormats.FileDrop);

            if (files != null)
            {
                foreach (var file in files)
                {
                    if (System.IO.File.Exists(file) || Directory.Exists(file))
                    {
                        bool isExtension = !extensionExt.Contains(System.IO.Path.GetExtension(file));
                        addShortcut(file, isExtension);
                    }
                }
            }

            if (pnlShortcuts.Children.Count != 0)
            {
                pnlScrollViewer.ScrollToBottom();
            }

            resetSelection();
        }

        private void addShortcut(String file, bool isExtension = false)
        {
            if (fgConfig == null)
                return;

            String workingDirec = getProperDirectory(file);

            ProgramShortcut psc = new ProgramShortcut() { FilePath = Environment.ExpandEnvironmentVariables(file), isWindowsApp = isExtension, WorkingDirectory = workingDirec }; //Create new shortcut obj
            fgConfig.ShortcutList.Add(psc); // Add to panel shortcut list
            LoadShortcut(psc, fgConfig.ShortcutList.Count - 1);
            RefreshProgramControls();
        }

        // Delete shortcut
        public void DeleteShortcut(ProgramShortcut psc)
        {
            resetSelection();

            if(fgConfig != null)
                fgConfig.ShortcutList.Remove(psc);

            resetSelection();

            ucProgramShortcut? _ShortcutControl = FindProgramShortcutControl(psc);
            int ShortcutIndex = pnlShortcuts.Children.IndexOf(_ShortcutControl);
            pnlShortcuts.Children.Remove(_ShortcutControl);
            RefreshProgramControls();
        }

        // Change positions of shortcut panels
        public void RepositionControl(ucProgramShortcut shortcut, int offset)
        {
            int controlIndex = pnlShortcuts.Children.IndexOf(shortcut);
            int newIndex = controlIndex + offset;

            if (newIndex > -1 && newIndex < (pnlShortcuts.Children.Count))
            {
                pnlShortcuts.Children.Remove(shortcut);
                pnlShortcuts.Children.Insert(newIndex, shortcut);
                RefreshProgramControls();
            }
        }

        //--------------------------------------
        // IMAGE HANDLERS
        //--------------------------------------
        private void cmdAddGroupIcon_Click(object sender, MouseButtonEventArgs e)
        {
            resetSelection();

            lblErrorIcon.Visibility = Visibility.Hidden;  //resetting error msg

            OpenFileDialog openFileDialog = new OpenFileDialog  // ask user to select img as group icon
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Title = "Select Group Icon",
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "img",
                Filter = "Image files and exec (*.jpg, *.jpeg, *.jpe, *.jfif, *.png, *.exe, *.ico) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png; *.ico; *.exe",
                FilterIndex = 2,
                RestoreDirectory = true,
                ReadOnlyChecked = true,
                DereferenceLinks = false,
            };

            if (openFileDialog.ShowDialog() == true)
            {

                String imageExtension = System.IO.Path.GetExtension(openFileDialog.FileName).ToLower();

                handleIcon(openFileDialog.FileName, imageExtension);
            }
            e.Handled = true;
        }

        // Handle drag and dropped images
        private void pnlDragDropImg(object sender, DragEventArgs e)
        {
            resetSelection();

            var files = (String[])e.Data.GetData(DataFormats.FileDrop);

            String imageExtension = System.IO.Path.GetExtension(files[0]).ToLower();

            if (files.Length == 1 && newExt.Contains(imageExtension) && System.IO.File.Exists(files[0]))
            {
                // Checks if the files being added/dropped are an .exe or .lnk in which tye icons need to be extracted/processed
                handleIcon(files[0], imageExtension);
            }
        }

        private void handleIcon(String file, String imageExtension)
        {
            // Checks if the files being added/dropped are an .exe or .lnk in which tye icons need to be extracted/processed
            if (specialImageExt.Contains(imageExtension))
            {
                if (imageExtension == ".lnk")
                {
                    cmdAddGroupIcon.Source = handleLnkExt(file);
                }
                else
                {
                    cmdAddGroupIcon.Source = Classes.ImageFunctions.IconPathToBitmapSource(file);
                }
            }
            else
            {
                cmdAddGroupIcon.Source = ImageFunctions.BitmapSourceFromFile(file);
            }
            lblAddGroupIcon.Text = "Change group icon";
        }

        public static BitmapSource handleLnkExt(String file)
        {
            IWshShortcut lnkIcon = (IWshShortcut)new WshShell().CreateShortcut(file);
            String[] icLocation = lnkIcon.IconLocation.Split(',');
            // Check if iconLocation exists to get an .ico from; if not then take the image from the .exe it is referring to
            // Checks for link iconLocations as those are used by some applications
            if (icLocation[0] != "" && !lnkIcon.IconLocation.Contains("http"))
            {
                return ImageFunctions.IconPathToBitmapSource(System.IO.Path.GetFullPath(Environment.ExpandEnvironmentVariables(icLocation[0])));
            }
            else if (icLocation[0] == "" && lnkIcon.TargetPath == "")
            {
                return handleWindowsApp.getWindowsAppIcon(file);
            }
            else
            {
                return ImageFunctions.IconPathToBitmapSource(System.IO.Path.GetFullPath(Environment.ExpandEnvironmentVariables(lnkIcon.TargetPath)));
            }
        }

        public static String handleExtName(String file)
        {
            if (file == null)
                return "Error";

            string fileName = System.IO.Path.GetFileName(file);
            string? filepath = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(file));
            Shell32.Folder shellFolder = shell.NameSpace(filepath);
            Shell32.FolderItem shellItem = shellFolder.Items().Item(fileName);

            return shellItem.Name;
        }

        // Below two functions highlights the background as you would if you hovered over it with a mosue
        // Use checkExtension to allow file dropping after a series of checks
        // Only highlights if the files being dropped are valid in extension wise
        private void pnlDragDropEnterExt(object sender, DragEventArgs e)
        {
            resetSelection();

            if (checkExtensions(e, extensionExt))
            {
                pnlAddShortcut.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 23, 23, 23));
            }
        }

        private void pnlDragDropEnterImg(object sender, DragEventArgs e)
        {
            resetSelection();

            if (checkExtensions(e, imageExt.Concat(specialImageExt).ToArray()))
            {
                pnlGroupIcon.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 23, 23, 23));
            }
        }

        // Series of checks to make sure it can be dropped
        private Boolean checkExtensions(DragEventArgs e, String[] exts)
        {

            // Make sure the file can be dragged dropped
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return false;

            if (e.Data.GetDataPresent("Shell IDList Array"))
            {
                e.Effects = e.AllowedEffects;
                return true;
            }


            // Get the list of files of the files dropped
            String[] files = (String[])e.Data.GetData(DataFormats.FileDrop);

            // Loop through each file and make sure the extension is allowed as defined by a series of arrays at the top of the script
            foreach (var file in files)
            {
                String ext = System.IO.Path.GetExtension(file);

                if (exts.Contains(ext.ToLower()) || Directory.Exists(file))
                {
                    // Gives the effect that it can be dropped and unlocks the ability to drop files in
                    e.Effects = DragDropEffects.Copy;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
 
        //--------------------------------------
        // SAVE/EXIT/DELETE GROUP
        //--------------------------------------

        // Exit editor
        private void cmdExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void cmdSave_Click(object sender, RoutedEventArgs e)
        {
            if (fgConfig == null)
                return;

            resetSelection();

            //List <Directory> directories = 

            if (txtGroupName.Text == "Name the new group...") // Verify category name
            {
                lblErrorTitle.Text = "Must select a name";
                lblErrorTitle.Visibility = Visibility.Visible;
            }
            else if (IsNew && Directory.Exists(@MainPath.Config + txtGroupName.Text) ||
                     !IsNew && fgConfig.GetName() != txtGroupName.Text && Directory.Exists(@MainPath.Config + txtGroupName.Text))
            {
                lblErrorTitle.Text = "There is already a group with that name";
                lblErrorTitle.Visibility = Visibility.Visible;
            }
            else if (!new Regex("^[0-9a-zA-Z \b]+$").IsMatch(txtGroupName.Text))
            {
                lblErrorTitle.Text = "Name must not have any special characters";
                lblErrorTitle.Visibility = Visibility.Visible;
            }
            else if (cmdAddGroupIcon.Source == (BitmapImage)Application.Current.Resources["AddWhite"]) // Verify icon
            {
                lblErrorIcon.Text = "Must select group icon";
                lblErrorIcon.Visibility = Visibility.Visible;
            }
            else if (fgConfig.ShortcutList.Count == 0) // Verify shortcuts
            {
                lblErrorShortcut.Text = "Must select at least one shortcut";
                lblErrorShortcut.Visibility = Visibility.Visible;
            }
            else
            {
                try
                {

                    foreach (ProgramShortcut shortcutModifiedItem in shortcutChanged)
                    {
                        if (!Directory.Exists(shortcutModifiedItem.WorkingDirectory))
                        {
                            shortcutModifiedItem.WorkingDirectory = getProperDirectory(shortcutModifiedItem.FilePath);
                        }
                    }


                    if (!IsNew)
                    {
                        //
                        // Delete old config
                        //
                        string configPath = @MainPath.Config + fgConfig.GetName();
                        string shortcutPath = @MainPath.Shortcuts + Regex.Replace(fgConfig.GetName(), @"(_)+", " ") + ".lnk";

                        try
                        {
                            using (TransactionScope scope1 = new TransactionScope())
                            {
                                Directory.Delete(configPath, true);
                                System.IO.File.Delete(shortcutPath);
                                scope1.Complete();
                                scope1.Dispose();
                            }
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("Please close all programs used within the taskbar group in order to save!");
                            return;
                        }
                    }

                    // Creating new config
                    fgConfig.CollumnCount = int.Parse(lblNum.Text);

                    // Normalize string so it can be used in path; remove spaces
                    fgConfig.SetName(Regex.Replace(txtGroupName.Text, @"\s+", "_"));

                    BitmapSource? groupImage = cmdAddGroupIcon.Source as BitmapSource;
                    if(groupImage == null)
                    {
                        groupImage = ImageFunctions.GetErrorImage();
                    }
                    fgConfig.CreateConfig(groupImage); // Creating group config files
                    Client.LoadCategory(System.IO.Path.GetFullPath(@"config\" + fgConfig.GetName())); // Loading visuals

                    Close();
                    Client.Reload();
                }
                catch (IOException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        // Delete group
        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if(fgConfig == null)
                return;

            resetSelection();

            try
            {
                string configPath = @MainPath.Config + fgConfig.GetName();
                string shortcutPath = @MainPath.Shortcuts + Regex.Replace(fgConfig.GetName(), @"(_)+", " ") + ".lnk";

                var dir = new DirectoryInfo(configPath);

                try
                {
                    using (TransactionScope scope1 = new TransactionScope())
                    {
                        Directory.Delete(configPath, true);
                        System.IO.File.Delete(shortcutPath);
                        this.Hide();
                        Client.Reload(); //flush and reload category panels
                        scope1.Complete();
                        scope1.Dispose();
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Please close all programs used within the taskbar group in order to delete!");
                    return;
                }

            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //--------------------------------------
        // UI CUSTOMIZATION
        //--------------------------------------

        // Change category width
        private void cmdWidthUp_Click(object sender, RoutedEventArgs e)
        {
            resetSelection();

            int num = int.Parse(lblNum.Text);
            if (num > 19)
            {
                lblErrorNum.Text = "Max width";
                lblErrorNum.Visibility = Visibility.Visible;
            }
            else
            {
                num++;
                lblErrorNum.Visibility = Visibility.Hidden;
                lblNum.Text = num.ToString();
            }

        }

        private void cmdWidthDown_Click(object sender, RoutedEventArgs e)
        {
            resetSelection();

            int num = int.Parse(lblNum.Text);
            if (num == 1)
            {
                lblErrorNum.Text = "Width cant be less than 1";
                lblErrorNum.Visibility = Visibility.Visible;
            }
            else
            {
                num--;
                lblErrorNum.Visibility = Visibility.Hidden;
                lblNum.Text = num.ToString();
            }
        }

        // Color radio buttons
        private void radioCustom_Click(object sender, RoutedEventArgs e)
        {
            if (fgConfig == null)
                return;

            frmColorPicker _colorPicker = new frmColorPicker(pnlCustomColor, fgConfig.CatagoryBGColor);
            if (_colorPicker.ShowDialog() == true)
            {
                fgConfig.CatagoryBGColor = _colorPicker.SelectedColor;
                pnlCustomColor.Visibility = Visibility.Visible;
                pnlCustomColor.Fill = new SolidColorBrush(_colorPicker.SelectedColor);
                lblOpacity.Text = Convert.ToInt32(((double)fgConfig.CatagoryBGColor.A) * 100.0 / 255.0).ToString();
            }
        }

        private void radioDark_Click(object sender, RoutedEventArgs e)
        {
            if (fgConfig != null)
            {
                fgConfig.CatagoryBGColor = System.Windows.Media.Color.FromArgb(fgConfig.CatagoryBGColor.A, 31, 31, 31);
                pnlCustomColor.Visibility = Visibility.Hidden;
                UpdateOpacityControls(int.Parse(lblOpacity.Text));
            }
        }

        private void radioLight_Click(object sender, RoutedEventArgs e)
        {
            if (fgConfig != null)
            {
                fgConfig.CatagoryBGColor = System.Windows.Media.Color.FromArgb(fgConfig.CatagoryBGColor.A, 230, 230, 230);
                pnlCustomColor.Visibility = Visibility.Hidden;
                UpdateOpacityControls(int.Parse(lblOpacity.Text));
            }
        }

        // Opacity buttons
        private void numOpacUp_Click(object sender, RoutedEventArgs e)
        {
            if (fgConfig == null)
                return;
            
            int opacity = Math.Min(int.Parse(lblOpacity.Text) + 10, 100);
            UpdateOpacityControls(opacity);
        }

        private void numOpacDown_Click(object sender, RoutedEventArgs e)
        {
            if (fgConfig == null)
                return;

            int opacity = Math.Max(int.Parse(lblOpacity.Text) - 10, 0);
            UpdateOpacityControls(opacity);
        }

        private int DisableOpacityButton(Button button)
        {
            button.IsHitTestVisible = false;
            button.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 125, 125, 125));
            return 0;
        }

        private int EnableOpacityButton(Button button)
        {
            button.IsHitTestVisible = true;
            button.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            return 0;
        }

        private void UpdateOpacityControls(int opacity)
        {
            if (fgConfig == null)
                return;

            lblOpacity.Text = opacity.ToString();
            fgConfig.CatagoryBGColor.A = (byte)(opacity * 255 / 100);
            pnlCustomColor.Fill = new SolidColorBrush(fgConfig.CatagoryBGColor);

            _ = opacity == 0 ? DisableOpacityButton(numOpacDown) : EnableOpacityButton(numOpacDown);
            _ = opacity == 100 ? DisableOpacityButton(numOpacUp) : EnableOpacityButton(numOpacUp);
        }

        //--------------------------------------
        // FORM VISUAL INTERACTIONS
        //--------------------------------------
        private void pnlGroupIcon_MouseEnter(object sender, MouseEventArgs e)
        {
            pnlGroupIcon.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 23, 23, 23));
        }

        private void pnlGroupIcon_MouseLeave(object sender, MouseEventArgs e)
        {
            pnlGroupIcon.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 31, 31, 31));
        }

        private void pnlAddShortcut_MouseEnter(object sender, MouseEventArgs e)
        {
            pnlAddShortcut.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 23, 23, 23));
        }

        private void pnlAddShortcut_MouseLeave(object sender, MouseEventArgs e)
        {
            pnlAddShortcut.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 31, 31, 31));
        }

        // Handles placeholder text for group name
        private void txtGroupName_GotFocus(object sender, RoutedEventArgs e)
        {
            resetSelection();
            if (txtGroupName.Text == "Name the new group...")
                txtGroupName.Text = "";
        }

        private void txtGroupName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtGroupName.Text == "")
                txtGroupName.Text = "Name the new group...";
        }

        private void txtGroupName_TextChanged(object sender, TextChangedEventArgs e)
        {
            lblErrorTitle.Visibility = Visibility.Hidden;
        }


        //--------------------------------------
        // SHORTCUT/PRGORAM SELECTION
        //--------------------------------------

        // Deselect selected program/shortcut
        public void resetSelection()
        {
            pnlArgumentTextbox.IsEnabled = false;
            cmdSelectDirectory.IsEnabled = false;
            if (selectedShortcut != null)
            {
                pnlColor.Visibility = Visibility.Visible;
                pnlArguments.Visibility = Visibility.Hidden;
                selectedShortcut.ucDeselected();
                selectedShortcut = null;
            }
        }


        // Enable the argument textbox once a shortcut/program has been selected
        public void enableSelection(ucProgramShortcut passedShortcut)
        {
            selectedShortcut = passedShortcut;
            passedShortcut.ucSelected();

            ProgramShortcut? pShortcut = passedShortcut.Shortcut;
            if (pShortcut != null)
            {
                pnlArgumentTextbox.Text = pShortcut.Arguments;
                pnlArgumentTextbox.IsEnabled = true;

                pnlWorkingDirectory.Text = pShortcut.WorkingDirectory;
                pnlWorkingDirectory.IsEnabled = true;
                cmdSelectDirectory.IsEnabled = true;

                pnlColor.Visibility = Visibility.Hidden;
                pnlArguments.Visibility = Visibility.Visible;
            }
        }

        private void pnlArgumentTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(fgConfig != null && selectedShortcut != null)
                fgConfig.ShortcutList[selectedShortcut.Position].Arguments = pnlArgumentTextbox.Text;
        }

        private void pnlArgumentTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                lblAddGroupIcon.Focus();

                e.Handled = true;
            }
        }

        private void pnlAllowOpenAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if(fgConfig != null)
                fgConfig.allowOpenAll = pnlAllowOpenAll.IsChecked == true ? true : false;
        }

        private void cmdSelectDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (selectedShortcut == null || fgConfig == null)
                return;

            String? InitDir = fgConfig.ShortcutList[selectedShortcut.Index].WorkingDirectory;
            OpenFolderDialog openFileDialog = new OpenFolderDialog
            {
                InitialDirectory = InitDir
            };

            if (openFileDialog.ShowDialog(this) == true)
            {
                pnlWorkingDirectory.Text = fgConfig.ShortcutList[selectedShortcut.Index].WorkingDirectory = openFileDialog.FolderName;
            }
        }

        private void pnlWorkingDirectory_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (selectedShortcut == null || fgConfig == null)
                return;

            fgConfig.ShortcutList[selectedShortcut.Position].WorkingDirectory = pnlWorkingDirectory.Text;

            if (!shortcutChanged.Contains(fgConfig.ShortcutList[selectedShortcut.Position]))
            {
                shortcutChanged.Add(fgConfig.ShortcutList[selectedShortcut.Position]);
            }
        }

        private String getProperDirectory(String file)
        {
            try
            {
                string? dirName = null;
                if (System.IO.Path.GetExtension(file).ToLower() == ".lnk")
                {
                    IWshShortcut extension = (IWshShortcut)new WshShell().CreateShortcut(file);
                    dirName = System.IO.Path.GetDirectoryName(extension.TargetPath);
                }
                else
                {
                    dirName = System.IO.Path.GetDirectoryName(file);
                }

                if (dirName != null)
                {
                    return dirName;
                }
            }
            catch{ }

            return MainPath.GetExecutablePath();
        }

        private void frmGroup_MouseClick(object sender, MouseButtonEventArgs e)
        {
            resetSelection();
        }

        private void pnlDragDropLeaveExt(object sender, DragEventArgs e)
        {
            pnlAddShortcut.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 31, 31, 31));
        }

        private void pnlDragDropLeaveImg(object sender, DragEventArgs e)
        {
            pnlGroupIcon.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 31, 31, 31));
        }

        private byte GetOpacity(double opacity)
        {
            return Convert.ToByte(opacity * 255.0 / 100.0);
        }

        public ucProgramShortcut? FindProgramShortcutControl(ProgramShortcut shortcut)
        {
            foreach (UIElement element in pnlShortcuts.Children)
            {
                ucProgramShortcut? ucPsc = element as ucProgramShortcut;
                if (ucPsc == null)
                {
                    continue;
                }

                if (ucPsc.Shortcut == shortcut)
                    return ucPsc;
            }

            return null;
        }
        public void RefreshProgramControls()
        {
            int ShortcutCount = pnlShortcuts.Children.Count;
            foreach (UIElement element in pnlShortcuts.Children)
            {
                ucProgramShortcut? ucPsc = element as ucProgramShortcut;
                if (ucPsc != null)
                {
                    int controlIndex = pnlShortcuts.Children.IndexOf(ucPsc);
                    ucPsc.UpdateIndex(controlIndex, controlIndex == (ShortcutCount - 1));
                }

            }
        }
    }
}
