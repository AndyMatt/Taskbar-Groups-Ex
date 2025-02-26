using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Windows.Data.Json;
using TaskbarGroupsEx.Classes;
using System.Windows.Shapes;

namespace TaskbarGroupsEx
{
    public partial class frmClient : Window
    {
        public frmClient()
        {
            NativeMethods.SetCurrentProcessExplicitAppUserModelID("tjackenpacken.taskbarGroup.main");
            System.Runtime.ProfileOptimization.StartProfile("frmClient.Profile");
            InitializeComponent();
            Reload();
           
            currentVersion.Text = currentVersion.Text.Replace("{CurrentVersion}", "v" + MainPath.GetAssemblyVersion());

            githubVersion.Inlines.Clear();
            githubVersion.Inlines.Add(Task.Run(() => getVersionData()).Result);
        }
        public void Reload()
        {
            pnlExistingGroups.Children.Clear();

            string[] subDirectories = Directory.GetDirectories(@MainPath.Config);
            foreach (string dir in subDirectories)
            {
                try
                {
                    LoadCategory(dir);
                }
                catch (IOException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            if (pnlExistingGroups.Children.Count > 0) // helper if no group is created
            {
                lblHelpTitle.Text = "Click on a group to add a taskbar shortcut";
                pnlHelp.Visibility = Visibility.Hidden;
            }
            else
            {
                lblHelpTitle.Text = "Press on \"Add Taskbar group\" to get started";
                pnlHelp.Visibility = Visibility.Hidden;
            }
        }

        public void LoadCategory(string dir)
        {
            Classes.FolderGroupConfig category = FolderGroupConfig.ParseConfiguration(dir);
            ucCategoryPanel newCategory = new ucCategoryPanel(this, category);
            pnlExistingGroups.Children.Add(newCategory);
            newCategory.MouseEnter += new MouseEventHandler((sender, e) => EnterControl(sender, e, newCategory));
            newCategory.MouseLeave += new MouseEventHandler((sender, e) => LeaveControl(sender, e, newCategory));
        }

        private void cmdAddGroup_Click(object sender, MouseButtonEventArgs e)
        {
            frmGroup newGroup = new frmGroup(this);
            newGroup.Show();
        }

        private void pnlAddGroup_MouseLeave(object sender, MouseEventArgs e)
        {
            pnlAddGroup.Background = new SolidColorBrush(Color.FromArgb(255, 3, 3, 3));
        }

        private void pnlAddGroup_MouseEnter(object sender, MouseEventArgs e)
        {
            pnlAddGroup.Background = new SolidColorBrush(Color.FromArgb(255, 31, 31, 31));
        }

        public void EnterControl(object sender, MouseEventArgs e, Control control)
        {
            control.Background = new SolidColorBrush(Color.FromArgb(255, 31, 31, 31));
        }
        public void LeaveControl(object sender, MouseEventArgs e, Control control)
        {
            control.Background = new SolidColorBrush(Color.FromArgb(255, 3, 3, 3));
        }

        private static async Task<String> getVersionData()
        {
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "taskbar-groups");
                var res = await client.GetAsync("https://api.github.com/repos/tjackenpacken/taskbar-groups/releases");
                res.EnsureSuccessStatusCode();
                string responseBody = await res.Content.ReadAsStringAsync();

                JsonArray responseJSON = (JsonArray)JsonArray.Parse(responseBody);
                JsonObject jsonObjectData = responseJSON[0].GetObject();

                return jsonObjectData["tag_name"].GetString();
            }
            catch { return "Not found"; }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo proc = new ProcessStartInfo(e.Uri.AbsoluteUri);
            proc.UseShellExecute = true;
            Process.Start(proc);
            e.Handled = true;
        }
    }
}
