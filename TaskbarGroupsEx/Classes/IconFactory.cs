using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace TaskbarGroupsEx.Classes
{
    public class IconFactory
    {
        #region constants
        public const int MaxIconWidth = 256;
        public const int MaxIconHeight = 256;

        public const int MinIconWidth = 16;
        public const int MinIconHeight = 16;

        private const ushort HeaderReserved = 0;
        private const ushort HeaderIconType = 1;
        private const byte HeaderLength = 6;

        private const byte EntryReserved = 0;
        private const byte EntryLength = 16;

        private const byte PngColorsInPalette = 0;
        private const ushort PngColorPlanes = 1;

        #endregion

        #region methods

        /// Saves the specified <see cref="Bitmap"/> objects as a single 
        /// icon into the output stream.

        /// <param name="images">The bitmaps to save as an icon.</param>
        /// <param name="stream">The output stream.</param>

        /// The expected input for the <paramref name="images"/> parameter are 
        /// portable network graphic files that have a <see cref="Image.PixelFormat"/> 
        /// of <see cref="PixelFormat.Format32bppArgb"/> and where the
        /// width is less than or equal to <see cref="IconFactory.MaxIconWidth"/> and the 
        /// height is less than or equal to <see cref="MaxIconHeight"/>.

        /// <exception cref="InvalidOperationException">
        /// Occurs if any of the input images do 
        /// not follow the required image format. See remarks for details.

        /// <exception cref="ArgumentNullException">
        /// Occurs if any of the arguments are null.

        public static void SavePngAsIcon(BitmapSource iconBitmap, Stream stream)
        {
            Stack<BitmapSource> images = ImageFunctions.CreateMultiSizeIcon(iconBitmap);

            if (images == null || images.Count == 0)
                throw new ArgumentNullException("images");
            if (stream == null)
                throw new ArgumentNullException("stream");

            BitmapSource[] orderedImages = images.OrderByDescending(i => i.Width)
                                           .ThenBy(i => i.Height)
                                           .ToArray();

            using (var writer = new BinaryWriter(stream))
            {

                // write the header
                writer.Write(IconFactory.HeaderReserved);
                writer.Write(IconFactory.HeaderIconType);
                writer.Write((ushort)orderedImages.Length);

                // save the image buffers and offsets
                Dictionary<uint, byte[]> buffers = new Dictionary<uint, byte[]>();

                // tracks the length of the buffers as the iterations occur
                // and adds that to the offset of the entries
                uint lengthSum = 0;
                uint baseOffset = (uint)(IconFactory.HeaderLength +
                                         IconFactory.EntryLength * orderedImages.Length);

                for (int i = 0; i < orderedImages.Length; i++)
                {
                    BitmapSource image = orderedImages[i];

                    // creates a byte array from an image
                    byte[] buffer = IconFactory.CreateImageBuffer(image);

                    // calculates what the offset of this image will be
                    // in the stream
                    uint offset = (baseOffset + lengthSum);

                    // writes the image entry
                    writer.Write(IconFactory.GetIconWidth(image));
                    writer.Write(IconFactory.GetIconHeight(image));
                    writer.Write(IconFactory.PngColorsInPalette);
                    writer.Write(IconFactory.EntryReserved);
                    writer.Write(IconFactory.PngColorPlanes);
                    writer.Write((ushort)image.Format.BitsPerPixel);
                    writer.Write((uint)buffer.Length);
                    writer.Write(offset);

                    lengthSum += (uint)buffer.Length;

                    // adds the buffer to be written at the offset
                    buffers.Add(offset, buffer);
                }

                // writes the buffers for each image
                foreach (var kvp in buffers)
                {

                    // seeks to the specified offset required for the image buffer
                    writer.BaseStream.Seek(kvp.Key, SeekOrigin.Begin);

                    // writes the buffer
                    writer.Write(kvp.Value);
                }
            }

        }

        private static byte GetIconHeight(BitmapSource image)
        {
            if (image.Height == IconFactory.MaxIconHeight)
                return 0;

            return (byte)image.Height;
        }

        private static byte GetIconWidth(BitmapSource image)
        {
            if (image.Width == IconFactory.MaxIconWidth)
                return 0;

            return (byte)image.Width;
        }

        private static byte[] CreateImageBuffer(BitmapSource image)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        #endregion

    }
}
