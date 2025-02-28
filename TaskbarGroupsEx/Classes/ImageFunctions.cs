using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TaskbarGroupsEx.Classes
{
    public static class ImageFunctions
    {
        static BitmapSource? errorImage = null;
        public static BitmapSource GetErrorImage()
        {
            if(errorImage == null)
            {
                Bitmap errorBitmap = new Bitmap(32, 32);
                Graphics flagGraphics = Graphics.FromImage(errorBitmap);
                flagGraphics.FillRectangle(System.Drawing.Brushes.Red, 0, 0, 32, 32);
                errorImage = Bitmap2BitmapSource(errorBitmap);
            }

            return errorImage;
        }

        public static BitmapSource ResizeImage(BitmapSource image, double width, double height, bool Padding = false)
        {
                double MinScale = Math.Min(width / image.Width, height / image.Height);
                BitmapSource resizedBmp = new TransformedBitmap(image, new ScaleTransform(MinScale, MinScale, 10, 10));

            if (!Padding)
                return resizedBmp;

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawImage(resizedBmp, new Rect(((width- resizedBmp.Width)/2.0), ((height- resizedBmp.Height)/2.0), resizedBmp.Width, resizedBmp.Height));
            drawingContext.Close();

            RenderTargetBitmap bmp = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);

            return bmp;
        }

        public static Icon IconFromImage(Image img)
        {
            var ms = new System.IO.MemoryStream();
            var bw = new System.IO.BinaryWriter(ms);
            // Header
            bw.Write((short)0);   // 0 : reserved
            bw.Write((short)1);   // 2 : 1=ico, 2=cur
            bw.Write((short)1);   // 4 : number of images
                                  // Image directory
            var w = img.Width;
            if (w >= 256) w = 0;
            bw.Write((byte)w);    // 0 : width of image
            var h = img.Height;
            if (h >= 256) h = 0;
            bw.Write((byte)h);    // 1 : height of image
            bw.Write((byte)0);    // 2 : number of colors in palette
            bw.Write((byte)0);    // 3 : reserved
            bw.Write((short)0);   // 4 : number of color planes
            bw.Write((short)0);   // 6 : bits per pixel
            var sizeHere = ms.Position;
            bw.Write((int)0);     // 8 : image size
            var start = (int)ms.Position + 4;
            bw.Write(start);      // 12: offset of image data
                                  // Image data
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var imageSize = (int)ms.Position - start;
            ms.Seek(sizeHere, System.IO.SeekOrigin.Begin);
            bw.Write(imageSize);
            ms.Seek(0, System.IO.SeekOrigin.Begin);

            // And load it
            return new Icon(ms);
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

        public static Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        public static BitmapSource ExtractIconToBitmapSource(string filePath)
        {
            if (filePath != null)
            {
                Icon? ico = Icon.ExtractAssociatedIcon(filePath);
                if (ico != null)
                {
                    return Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
            }
            return GetErrorImage();
        }

        public static BitmapSource IconPathToBitmapSource(string filePath)
        {
            Icon? _icon = Icon.ExtractAssociatedIcon(filePath);
            if( _icon != null )
            {
                return Imaging.CreateBitmapSourceFromHIcon(_icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }

            return GetErrorImage();
        }

        public static BitmapSource IconToBitmapSource(Icon icon)
        {
            return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
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
        //
        // END OF CLASS
        //
    }
}
