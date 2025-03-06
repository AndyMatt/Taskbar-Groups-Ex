using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TaskbarGroupsEx.Classes;

namespace TaskbarGroupsEx
{
    public partial class ucCategoryPanel : UserControl
    {
        public FolderGroupConfig Category;
        public frmClient Client;
        public Image? shortcutPanel;

        public ucCategoryPanel(frmClient client, FolderGroupConfig category)
        {
            InitializeComponent();
            Client = client;
            Category = category;
            lblTitle.Text = Regex.Replace(category.GetName(), @"(_)+", " ");
            picGroupIcon.Source = category.LoadIconImage();

            if (!Directory.Exists((@"config\" + category.GetName()) + "\\Icons\\"))
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
            this.shortcutPanel.Source = Category.loadImageCache(programShortcut);

            this.pnlShortcutIcons.Children.Add(this.shortcutPanel);
        }

        public void OpenFolder(object sender, MouseEventArgs e)
        {
            // Open the shortcut folder for the group when click on category panel

            // Build path based on the directory of the main .exe file
            string filePath = System.IO.Path.GetFullPath(new Uri($"{MainPath.Shortcuts}").LocalPath + "\\" + Category.GetName() + ".lnk");

            // Open directory in explorer and highlighting file
            System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", @filePath));
        }

        private void cmdEdit_Click(object sender, RoutedEventArgs e)
        {
            frmGroup editGroup = new frmGroup(Client, Category);
            editGroup.ShowDialog();
        }
    }
}
