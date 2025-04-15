using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TaskbarGroupsEx.Classes
{
    public static class ImageFunctions
    {
        static BitmapSource? errorBitmapSource = null;
        public static BitmapSource GetErrorImageSource()
        {
            if(errorBitmapSource == null)
            {
                Bitmap errorBitmap = new Bitmap(32, 32);
                Graphics flagGraphics = Graphics.FromImage(errorBitmap);
                flagGraphics.FillRectangle(System.Drawing.Brushes.Red, 0, 0, 32, 32);
                errorBitmapSource = Bitmap2BitmapSource(errorBitmap);
            }

            return errorBitmapSource;
        }

        static BitmapImage? errorBitmapImage = null;
        public static BitmapImage GetErrorImage()
        {
            if (errorBitmapImage == null)
            {
                Bitmap errorBitmap = new Bitmap(32, 32);            
                Graphics flagGraphics = Graphics.FromImage(errorBitmap);
                flagGraphics.FillRectangle(System.Drawing.Brushes.Red, 0, 0, 32, 32);
                errorBitmapImage = Bitmap2BitmapImage(errorBitmap);
            }

            return errorBitmapImage;
        }

        public static BitmapSource GetDefaultShortcutIcon()
        {
            return new BitmapImage(new Uri("/Resources/DefaultIcon.png", UriKind.Relative));
        }

        public static BitmapSource ResizeImage(BitmapSource image, double width, double height, bool maintainAspect = true)
        {
            DrawingGroup drawingGroup = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(drawingGroup, BitmapScalingMode.Fant);

            double MinScale = Math.Min(width / image.Width, height / image.Height);


            double sourceWidth = maintainAspect ? image.Width * MinScale : width;
            double sourceHeight = maintainAspect ? image.Height * MinScale : height;

            double x = maintainAspect ? (width - sourceWidth) / 2.0 : 0.0;
            double y = maintainAspect ? (height - sourceHeight) / 2.0 : 0.0;
  
            Rect targetRect = new Rect(x, y, sourceWidth, sourceHeight);

            drawingGroup.Children.Add(new ImageDrawing(image, targetRect));

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawDrawing(drawingGroup);
            drawingContext.Close();

            RenderTargetBitmap bmp = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);

            return bmp;
        }

        public static System.Drawing.Color FromString(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name");
            }

            KnownColor knownColor;

            if (Enum.TryParse(name, out knownColor))
            {
                return System.Drawing.Color.FromKnownColor(knownColor);
            }

            return ColorTranslator.FromHtml(name);
        }

        public static System.Windows.Media.Color ToWindowsColor(System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A,color.R,color.G,color.B);
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public static BitmapSource Bitmap2BitmapSource(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSource retval;

            try
            {
                retval = Imaging.CreateBitmapSourceFromHBitmap(
                             hBitmap,
                             IntPtr.Zero,
                             Int32Rect.Empty,
                             BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }

            return retval;
        }

        public static BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }   
        }

        public static BitmapSource IconPathToBitmapSource(string filePath)
        {
            Icon? _icon = Icon.ExtractAssociatedIcon(filePath);
            if( _icon != null )
            {
                return Imaging.CreateBitmapSourceFromHIcon(_icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }

            return GetErrorImageSource();
        }

        public static BitmapSource IconToBitmapSource(Icon? icon)
        {
            if(icon != null)
                return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            return GetErrorImageSource();
        }

        public static void SaveBitmapSourceToFile(BitmapSource bitmap, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fileStream);
            }
        }

        public static BitmapSource BitmapSourceFromFile(string filePath)
        {
            var img = new System.Windows.Media.Imaging.BitmapImage(new Uri(filePath));
            return img;
        }

        public static Stack<BitmapSource> CreateMultiSizeIcon(BitmapSource bitmapSource)
        {
            Stack<BitmapSource> iconList = new Stack<BitmapSource>();

            double mipSize = IconFactory.MaxIconWidth;
            while (mipSize > IconFactory.MinIconWidth)
            {
                iconList.Push(ImageFunctions.ResizeImage(bitmapSource, mipSize, mipSize) as BitmapSource);
                mipSize = Math.Round(mipSize / 2.0);
            }
            return iconList;
        }
        //
        // END OF CLASS
        //
    }
}
