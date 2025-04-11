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

        //Struct used by SHGetFileInfo function
        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        //Constants flags for SHGetFileInfo 
        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0; // 'Large icon

        //Import SHGetFileInfo function
        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        public static BitmapSource GetFolderIcon(string Path)
        {
            SHFILEINFO shinfo = new SHFILEINFO();

            //Call function with the path to the folder you want the icon for
            SHGetFileInfo(
                Path,
                0, ref shinfo, (uint)Marshal.SizeOf(shinfo),
                SHGFI_ICON | SHGFI_LARGEICON);



            using (Icon i = System.Drawing.Icon.FromHandle(shinfo.hIcon))
            {
                //Convert icon to a Bitmap source
                BitmapSource img = Imaging.CreateBitmapSourceFromHIcon(
                                        i.Handle,
                                        new Int32Rect(0, 0, i.Width, i.Height),
                                        BitmapSizeOptions.FromEmptyOptions());

                //WPF Image control
                return img;
            }
        }

        public static BitmapSource GetShortcutIcon(string iconPath)
        {
            if (System.IO.File.Exists(iconPath))
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream(System.IO.File.ReadAllBytes(iconPath)))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = ms;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }

            return GetDefaultShortcutIcon();
        }

        public static Task<BitmapSource>GetFaviconFromURL(string url)
        {
            try
            {
                Uri favIconURI = new Uri(@"https://t2.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url=" + url + "&size=48");
                var client = new HttpClient();
                var response = client.GetByteArrayAsync(favIconURI);
                response.Wait();

                using (MemoryStream ms = new MemoryStream(response.Result))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = ms;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return Task.FromResult((BitmapSource)bitmap);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return Task.FromResult((BitmapSource)Application.Current.Resources["ErrorIcon"]);
        }
        //
        // END OF CLASS
        //
    }
}
