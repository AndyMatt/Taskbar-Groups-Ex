using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TaskbarGroupsEx.Classes;
using TaskbarGroupsEx.Forms;
using TaskbarGroupsEx.GroupItems;

namespace TaskbarGroupsEx.User_Controls
{
    public partial class ucShortcut : UserControl
    {
        public DynamicGroupItem? GroupItem = null;
        public frmMain? MotherForm = null;
        public FolderGroupConfig? ThisCategory = null;
        public ucShortcut()
        {
            InitializeComponent();
        }

        private void ucShortcut_Load(object sender, RoutedEventArgs e)
        {
            if (ThisCategory != null && GroupItem != null)
            {
                selectionCursor.Source = picIcon.Source = GroupItem.GetIcon();
            }
        }

        private void ucShortcut_Click(object sender, MouseButtonEventArgs e)
        {
            ucShortcut_OnClick();
        }

        public void ucShortcut_OnClick()
        {
            if (GroupItem != null)
                GroupItem.OnExecute();
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
