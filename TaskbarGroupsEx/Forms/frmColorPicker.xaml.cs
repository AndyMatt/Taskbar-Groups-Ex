using System.Windows;
using System.Windows.Media;

namespace TaskbarGroupsEx
{
    public partial class frmColorPicker : Window
    {
        public Color SelectedColor = Color.FromArgb(255, 31, 31, 31);

        public frmColorPicker(UIElement parent, Color _currentColor)
        {
            InitializeComponent();

            var openPosition = parent.PointToScreen(new System.Windows.Point(0, 0));
            this.Top = openPosition.Y - this.Height + 10; 
            this.Left = openPosition.X +5;

            modColorPicker.SelectedColor = _currentColor;
        }
        private void cmdOK_Click(object sender, RoutedEventArgs e)
        {
            SelectedColor = modColorPicker.SelectedColor;
            DialogResult = true;
            Close();
        }

        private void cmdCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
