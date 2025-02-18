using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using TaskbarGroupsEx;
using TaskbarGroupsEx.Classes;

namespace TaskbarGroupsEx
{
    /// <summary>
    /// Interaction logic for ucCategoryPanel.xaml
    /// </summary>
    public partial class ucCategoryPanel : UserControl
    {
        public Category Category;
        public frmClient Client;
        public ucCategoryPanel(frmClient client, Category category)
        {
            InitializeComponent();
            Client = client;
            Category = category;
            lblTitle.Text = Regex.Replace(category.Name, @"(_)+", " ");
            picGroupIcon.Source = Category.LoadIconImage();

            if (!Directory.Exists((@"config\" + category.Name) + "\\Icons\\"))
            {
                category.cacheIcons();
            }

            foreach (ProgramShortcut psc in Category.ShortcutList) // since this is calculating uc height it cant be placed in load
            {
                CreateShortcut(psc);
            }
        }

        private void CreateShortcut(ProgramShortcut programShortcut)
        {
            // creating shortcut picturebox from shortcut
            this.shortcutPanel = new System.Windows.Controls.Image
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(10, 2, 10, 2),
                Width = Height = 32,
            };
            this.shortcutPanel.MouseEnter += new MouseEventHandler((sender, e) => Client.EnterControl(sender, e, this));
            this.shortcutPanel.MouseLeave += new MouseEventHandler((sender, e) => Client.LeaveControl(sender, e, this));
            this.shortcutPanel.MouseLeftButtonUp += new MouseButtonEventHandler((sender, e) => OpenFolder(sender, e));

            // Check if file is stil existing and if so render it
            if (File.Exists(programShortcut.FilePath) || Directory.Exists(programShortcut.FilePath) || programShortcut.isWindowsApp)
            {
                this.shortcutPanel.Source = Category.loadImageCache(programShortcut);
            }
            else // if file does not exist
            {
                this.shortcutPanel.Source = (BitmapImage)Application.Current.Resources["ErrorIcon"];
                this.shortcutPanel.ToolTip = "Program does not exist";
            }

            this.pnlShortcutIcons.Children.Add(this.shortcutPanel);
        }

        public void OpenFolder(object sender, MouseEventArgs e)
        {
            // Open the shortcut folder for the group when click on category panel

            // Build path based on the directory of the main .exe file
            string filePath = System.IO.Path.GetFullPath(new Uri($"{MainPath.path}\\Shortcuts").LocalPath + "\\" + Category.Name + ".lnk");

            // Open directory in explorer and highlighting file
            System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", @filePath));
        }

        private void cmdEdit_Click(object sender, RoutedEventArgs e)
        {
            frmGroup editGroup = new frmGroup(Client, Category);
            editGroup.ShowDialog();
        }

        public static Bitmap LoadBitmap(string path) // needed to access img without occupying read/write
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                var memoryStream = new MemoryStream(reader.ReadBytes((int)stream.Length));
                reader.Close();
                stream.Close();
                return new Bitmap(memoryStream);
            }
        }

        public System.Windows.Controls.Image shortcutPanel;

    }
}
