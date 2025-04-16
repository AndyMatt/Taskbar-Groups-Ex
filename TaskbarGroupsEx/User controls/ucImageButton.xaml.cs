using System;
using System.Collections.Generic;
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
using Windows.System.UserProfile;

namespace TaskbarGroupsEx
{    
    public partial class ucImageButton : UserControl
    {
        private BitmapSource? bitmap;
        public bool isHover;
        public bool isEnabled;
        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        public static readonly DependencyProperty ImageSourceProperty =
    DependencyProperty.Register("ImageSource",
        typeof(ImageSource), typeof(ucImageButton),
        new FrameworkPropertyMetadata(new PropertyChangedCallback(OnImageSourceChanged)));
 
        public Brush ButtonDefault
        {
            get { return (Brush)GetValue(ButtonDefaultProperty); }
            set { SetValue(ButtonDefaultProperty, value); }
        }

        public static readonly DependencyProperty ButtonDefaultProperty = DependencyProperty.Register("ButtonDefault",
            typeof(Brush), typeof(ucImageButton),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnColorChanged)));

        public Brush ButtonHighlight
        {
            get { return (Brush)GetValue(ButtonHighlightProperty); }
            set { SetValue(ButtonHighlightProperty, value); }
        }

        public static readonly DependencyProperty ButtonHighlightProperty = DependencyProperty.Register("ButtonHighlight", typeof(Brush), typeof(ucImageButton));

        public Brush ButtonDepressed
        {
            get { return (Brush)GetValue(ButtonDepressedProperty); }
            set { SetValue(ButtonDepressedProperty, value); }
        }

        public static readonly DependencyProperty ButtonDepressedProperty = DependencyProperty.Register("Depressed", typeof(Brush), typeof(ucImageButton));

        public double Rotation
        {
            get { return (double)GetValue(RotationProperty); }
            set { SetValue(RotationProperty, value); }
        }

        public static readonly DependencyProperty RotationProperty = DependencyProperty.Register("Rotation",
            typeof(double), typeof(ucImageButton),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnRotationChanged)));

        public ucImageButton()
        {
            InitializeComponent();
            isHover = false;
            isEnabled = true;
        }

        private static void OnImageSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is BitmapSource)
            {
                ((ucImageButton)sender).bitmap = (BitmapSource)e.NewValue;
                BitmapSource bitmapSource = (new TransformedBitmap(((ucImageButton)sender).bitmap, new RotateTransform(((ucImageButton)sender).Rotation)));
                ((ucImageButton)sender).RectImage.OpacityMask = new ImageBrush(bitmapSource) { Stretch = Stretch.Uniform };
            }
        }

        private static void OnColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((ucImageButton)sender).RectImage.Fill = e.NewValue as Brush;
        }

        private static void OnRotationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if(e.NewValue is double)
            {
                if (((ucImageButton)sender).bitmap != null)
                {
                    double newRotation = (double)e.NewValue;
                    BitmapSource bitmapSource = (new TransformedBitmap(((ucImageButton)sender).bitmap, new RotateTransform(((ucImageButton)sender).Rotation)));
                    ((ucImageButton)sender).RectImage.OpacityMask = new ImageBrush(bitmapSource) { Stretch = Stretch.Uniform };
                }
            }
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!isEnabled)
                return;

            isHover = true;

            this.RectImage.Fill = ButtonHighlight;
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!isEnabled)
                return;

            isHover = false;
            
            this.RectImage.Fill = ButtonDefault;
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!isEnabled)
                return;

            UIElement el = (UIElement)sender;
            el.CaptureMouse();

            this.RectImage.Fill = ButtonDepressed;
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isEnabled)
                return;

            UIElement el = (UIElement)sender;
            el.ReleaseMouseCapture();

            this.RectImage.Fill = isHover ? ButtonHighlight : ButtonDefault;
        }

        public int Enable()
        {
            if (!isEnabled)
            {
                isEnabled = true;
                this.RectImage.Fill = ButtonDefault;
                this.IsHitTestVisible = true;
            }
            return 0;
        }

        public int Disable()
        {
            if (isEnabled)
            {
                isHover = isEnabled = false;
                this.IsHitTestVisible = false;
                this.RectImage.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 100, 100, 100));
            }
            return 0;
        }
    }
}
