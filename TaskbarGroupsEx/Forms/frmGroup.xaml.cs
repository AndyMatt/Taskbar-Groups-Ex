using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
//using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Interop;
using IWshRuntimeLibrary;
using System.IO;
using System.Diagnostics.Eventing.Reader;
using TaskbarGroupsEx;
using TaskbarGroupsEx.Classes;
using System.Text.RegularExpressions;
using System.Windows.Media;
using ColorPicker;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Shell;
using System.Transactions;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Xml.Linq;

namespace TaskbarGroupsEx
{
    /// <summary>
    /// Interaction logic for frmGroup.xaml
    /// </summary>
    public partial class frmGroup : Window
    {
        public Category Category;
        public frmClient Client;
        public bool IsNew;
        private String[] imageExt = new String[] { ".png", ".jpg", ".jpe", ".jfif", ".jpeg", };
        private String[] extensionExt = new String[] { ".exe", ".lnk", ".url" };
        private String[] specialImageExt = new String[] { ".ico", ".exe", ".lnk" };
        private String[] newExt;

        public ucProgramShortcut? selectedShortcut = null;

        public static Shell32.Shell shell = new Shell32.Shell();

        private List<ProgramShortcut> shortcutChanged = new List<ProgramShortcut>();

        //--------------------------------------
        // CTOR AND LOAD
        //--------------------------------------

        // CTOR for creating a new group
        public frmGroup(frmClient client)
        {
            // Setting from profile
            System.Runtime.ProfileOptimization.StartProfile("frmGroup.Profile");

            InitializeComponent();

            // Setting default category properties
            newExt = imageExt.Concat(specialImageExt).ToArray();
            Category = new Category { ShortcutList = new List<ProgramShortcut>() };
            Client = client;
            IsNew = true;

            // Setting default control values
            clmnDelete.Width = new GridLength(0.0);
            radioDark.IsChecked = true;
        }

        public frmGroup(frmClient client, Category category)
        {
            // Setting form profile
            System.Runtime.ProfileOptimization.StartProfile("frmGroup.Profile");

            InitializeComponent();

            // Setting properties
            Category = category;
            Client = client;
            IsNew = false;

            // Setting control values from loaded group
            this.Title = "Edit group";
            txtGroupName.Text = Regex.Replace(Category.Name, @"(_)+", " ");
            pnlAllowOpenAll.IsChecked = category.allowOpenAll;
            cmdAddGroupIcon.Source = Category.LoadIconImage();
            lblNum.Text = Category.Width.ToString();


            System.Windows.Media.Color categoryColor = Category.CatagoryBGColor;

            lblOpacity.Text = Convert.ToInt32(((double)categoryColor.A) * 100.0 / 255.0).ToString();

            if (categoryColor == System.Windows.Media.Color.FromArgb(categoryColor.A, 31, 31, 31))
                radioDark.IsChecked = true;
            else if (categoryColor == System.Windows.Media.Color.FromArgb(categoryColor.A, 230, 230, 230))
                radioLight.IsChecked = true;
            else
            {
                radioCustom.IsChecked = true;
                pnlCustomColor.Visibility = Visibility.Visible;
                pnlCustomColor.Fill = new System.Windows.Media.SolidColorBrush(categoryColor);
            }

            // Loading existing shortcutpanels
            int position = 0;
            foreach (ProgramShortcut psc in category.ShortcutList)
            {
                LoadShortcut(psc, position);
                position++;
            }
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

            if (Category.ShortcutList.Count >= 20)
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
            var files = (String[])e.Data.GetData(DataFormats.FileDrop);

            if (files == null)
            {
                ShellObjectCollection ShellObj = ShellObjectCollection.FromDataObject((System.Runtime.InteropServices.ComTypes.IDataObject)e.Data);

                foreach (ShellNonFileSystemItem item in ShellObj)
                {
                    addShortcut(item.ParsingName, true);
                }
            } else
            {
                // Loops through each file to make sure they exist and to add them directly to the shortcut list
                foreach (var file in files)
                {
                    if (extensionExt.Contains(System.IO.Path.GetExtension(file)) && System.IO.File.Exists(file) || Directory.Exists(file))
                    {
                        addShortcut(file);
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
            String workingDirec = getProperDirectory(file);

            ProgramShortcut psc = new ProgramShortcut() { FilePath = Environment.ExpandEnvironmentVariables(file), isWindowsApp = isExtension, WorkingDirectory = workingDirec }; //Create new shortcut obj
            Category.ShortcutList.Add(psc); // Add to panel shortcut list
            LoadShortcut(psc, Category.ShortcutList.Count - 1);
            RefreshProgramControls();
        }

        // Delete shortcut
        public void DeleteShortcut(ProgramShortcut psc)
        {
            resetSelection();

            Category.ShortcutList.Remove(psc);
            resetSelection();
            //bool before = true;
            //int i = 0;

            ucProgramShortcut? _ShortcutControl = FindProgramShortcutControl(psc);
            int ShortcutIndex = pnlShortcuts.Children.IndexOf(_ShortcutControl);
            pnlShortcuts.Children.Remove(_ShortcutControl);
            RefreshProgramControls();

            /*
            foreach (UIElement element in pnlShortcuts.Children)
            {
                ucProgramShortcut? ucPsc = element as ucProgramShortcut;
                if(ucPsc == null)
                {
                    continue;
                }

                if (before)
                {
                    //ucPsc.Top -= 50;
                    ucPsc.Position -= 1;
                }
                if (ucPsc.Shortcut == psc)
                {
                    //i = pnlShortcuts.Controls.IndexOf(ucPsc);

                    int controlIndex = pnlShortcuts.Children.IndexOf(ucPsc);

                    

                    if (controlIndex + 1 != pnlShortcuts.Children.Count)
                    {
                        try
                        {
                            pnlScrollViewer.ScrollToHorizontalOffset(ucPsc.Height * controlIndex);
                            //pnlShortcuts.ScrollControlIntoView(pnlShortcuts.Controls[controlIndex]);
                        }
                        catch
                        {
                            if (pnlShortcuts.Children.Count != 0)
                            {
                                pnlScrollViewer.ScrollToHorizontalOffset(ucPsc.Height * (controlIndex-1));
                                //pnlShortcuts.ScrollControlIntoView(pnlShortcuts.Controls[controlIndex - 1]);
                            }
                        }
                    }

                    before = false;
                }
            }

            /*
            if (pnlShortcuts.Controls.Count < 5)
            {
                pnlShortcuts.Height -= 50;
                pnlAddShortcut.Top -= 50;
            }
            */
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
            string fileName = System.IO.Path.GetFileName(file);
            file = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(file));
            Shell32.Folder shellFolder = shell.NameSpace(file);
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
            resetSelection();

            //List <Directory> directories = 

            if (txtGroupName.Text == "Name the new group...") // Verify category name
            {
                lblErrorTitle.Text = "Must select a name";
                lblErrorTitle.Visibility = Visibility.Visible;
            }
            else if (IsNew && Directory.Exists(@MainPath.path + @"\config\" + txtGroupName.Text) ||
                     !IsNew && Category.Name != txtGroupName.Text && Directory.Exists(@MainPath.path + @"\config\" + txtGroupName.Text))
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
            else if (Category.ShortcutList.Count == 0) // Verify shortcuts
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
                        string configPath = @MainPath.path + @"\config\" + Category.Name;
                        string shortcutPath = @MainPath.path + @"\Shortcuts\" + Regex.Replace(Category.Name, @"(_)+", " ") + ".lnk";

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
                    //
                    // Creating new config
                    //
                    //int width = int.Parse(lblNum.Text);

                    Category.Width = int.Parse(lblNum.Text);

                    //Category category = new Category(txtGroupName.Text, Category.ShortcutList, width, System.Drawing.ColorTranslator.ToHtml(CategoryColor), Category.Opacity); // Instantiate category

                    // Normalize string so it can be used in path; remove spaces
                    Category.Name = Regex.Replace(txtGroupName.Text, @"\s+", "_");

                    Category.CreateConfig(cmdAddGroupIcon.Source as BitmapSource); // Creating group config files
                    Client.LoadCategory(System.IO.Path.GetFullPath(@"config\" + Category.Name)); // Loading visuals

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
            resetSelection();

            try
            {
                string configPath = @MainPath.path + @"\config\" + Category.Name;
                string shortcutPath = @MainPath.path + @"\Shortcuts\" + Regex.Replace(Category.Name, @"(_)+", " ") + ".lnk";

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
            frmColorPicker _colorPicker = new frmColorPicker(pnlCustomColor, Category.CatagoryBGColor);
            if (_colorPicker.ShowDialog() == true)
            {
                Category.CatagoryBGColor = _colorPicker.SelectedColor;
                pnlCustomColor.Visibility = Visibility.Visible;
                pnlCustomColor.Fill = new SolidColorBrush(_colorPicker.SelectedColor);
                lblOpacity.Text = Convert.ToInt32(((double)Category.CatagoryBGColor.A) * 100.0 / 255.0).ToString();
            }
        }

        private void radioDark_Click(object sender, RoutedEventArgs e)
        {
            Category.CatagoryBGColor = System.Windows.Media.Color.FromArgb(GetOpacity(Category.Opacity), 31, 31, 31);
            pnlCustomColor.Visibility = Visibility.Hidden;
        }

        private void radioLight_Click(object sender, RoutedEventArgs e)
        {
            Category.CatagoryBGColor = System.Windows.Media.Color.FromArgb(GetOpacity(Category.Opacity), 230, 230, 230);
            pnlCustomColor.Visibility = Visibility.Hidden;
        }

        // Opacity buttons
        private void numOpacUp_Click(object sender, RoutedEventArgs e)
        {
            double op = double.Parse(lblOpacity.Text);
            if (op >= 100.0)
                return;

            op += 10;
            Category.Opacity = op;
            Category.CatagoryBGColor.A = GetOpacity(op);
            pnlCustomColor.Fill = new SolidColorBrush(Category.CatagoryBGColor);
            lblOpacity.Text = op.ToString();
            numOpacDown.IsEnabled = true;
            numOpacDown.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));

            if (op > 90)
            {
                numOpacUp.IsEnabled = false;
                numOpacUp.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 125, 125, 125));
            }
        }

        private void numOpacDown_Click(object sender, RoutedEventArgs e)
        {
            double op = double.Parse(lblOpacity.Text);
            if (op <= 0.0)
                return;

            op -= 10;
            Category.Opacity = op;
            Category.CatagoryBGColor.A = GetOpacity(op);
            pnlCustomColor.Fill = new SolidColorBrush(Category.CatagoryBGColor);
            lblOpacity.Text = op.ToString();
            numOpacUp.IsEnabled = true;
            numOpacUp.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));

            if (op < 10)
            {
                numOpacDown.IsEnabled = false;
                numOpacDown.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 125, 125, 125));
            }
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
                selectedShortcut.IsSelected = false;
                selectedShortcut = null;
            }
        }


        // Enable the argument textbox once a shortcut/program has been selected
        public void enableSelection(ucProgramShortcut passedShortcut)
        {
            selectedShortcut = passedShortcut;
            passedShortcut.ucSelected();
            passedShortcut.IsSelected = true;

            pnlArgumentTextbox.Text = Category.ShortcutList[selectedShortcut.Index].Arguments;
            pnlArgumentTextbox.IsEnabled = true;

            pnlWorkingDirectory.Text = Category.ShortcutList[selectedShortcut.Index].WorkingDirectory;
            pnlWorkingDirectory.IsEnabled = true;
            cmdSelectDirectory.IsEnabled = true;

            pnlColor.Visibility = Visibility.Hidden;
            pnlArguments.Visibility = Visibility.Visible;
        }

        private void pnlArgumentTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Category.ShortcutList[selectedShortcut.Position].Arguments = pnlArgumentTextbox.Text;
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
            Category.allowOpenAll = pnlAllowOpenAll.IsChecked == true ? true : false;
        }

        private void cmdSelectDirectory_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog openFileDialog = new CommonOpenFileDialog()
            {
                EnsurePathExists = true,
                IsFolderPicker = true,
                InitialDirectory = Category.ShortcutList[selectedShortcut.Index].WorkingDirectory
            };

            if (openFileDialog.ShowDialog(this) == CommonFileDialogResult.Ok)
            {
                pnlWorkingDirectory.Text = Category.ShortcutList[selectedShortcut.Index].WorkingDirectory = openFileDialog.FileName;
            }
        }

        private void pnlWorkingDirectory_TextChanged(object sender, TextChangedEventArgs e)
        {
            Category.ShortcutList[selectedShortcut.Position].WorkingDirectory = pnlWorkingDirectory.Text;

            if (!shortcutChanged.Contains(Category.ShortcutList[selectedShortcut.Position]))
            {
                shortcutChanged.Add(Category.ShortcutList[selectedShortcut.Position]);
            }
        }

        private String getProperDirectory(String file)
        {
            try
            {
                if (System.IO.Path.GetExtension(file).ToLower() == ".lnk")
                {
                    IWshShortcut extension = (IWshShortcut)new WshShell().CreateShortcut(file);

                    return System.IO.Path.GetDirectoryName(extension.TargetPath);
                }
                else
                {
                    return System.IO.Path.GetDirectoryName(file);
                }
            }
            catch (Exception)
            {
                return MainPath.exeString;
            }
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
