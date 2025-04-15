using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TaskbarGroupsEx.Classes;
using static TaskbarGroupsEx.Classes.ConfigFile;

namespace TaskbarGroupsEx.Handlers
{
    class FileHandler
    {
        public static MemoryStream? GetMemoryStream(string filePath)
        {
            try
            {
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    MemoryStream memoryStream = new MemoryStream();
                    fs.CopyTo(memoryStream);
                    fs.Close();
                    return memoryStream;
                }
            }
            catch { }
            return null;
        }

        

        public static BitmapSource? OpenIco(string filePath)
        {
            try
            {
                Icon? _ico = Icon.ExtractAssociatedIcon(filePath);
                return ImageFunctions.IconToBitmapSource(_ico);
            }
            catch { }

            return null;
        }

        public static BitmapImage? OpenPNG(string filePath)
        {
            try
            {
                var source = new BitmapImage();
                using (MemoryStream? stream = FileHandler.GetMemoryStream(filePath))
                {
                    source.BeginInit();
                    source.CacheOption = BitmapCacheOption.OnLoad;
                    source.StreamSource = stream;
                    source.EndInit();
                    source.Freeze(); // optional
                }
                return source;
            }
            catch { }

            return null;
        }

        FileStream? fileStream;
        public FileHandler(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            fileStream = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
        }

        public void Close()
        {
            if(fileStream != null)
            {
                fileStream.Close();
                fileStream = null;
            }
        }

        public void Write(ConfigPropertyContainer propertyContainer, string? subID = null)
        {
            Write(propertyContainer.mProperties, subID);
        }

        public void Write(List<KeyValuePair<string,string>> listValues, string? subID = null)
        {
            if (fileStream != null)
            {
                foreach (KeyValuePair<string, string> pairValue in listValues)
                {
                    WriteLine(subID, $"{pairValue.Key}={pairValue.Value}");               
                }
            }
        }

        private void WriteLine(string? subID, string value)
        {
            WriteLine((subID != null) ? ($"{subID}.{value}") : value);
        }

        private void WriteLine(string value)
        {
            if (fileStream != null)
            {
                byte[] info = new UTF8Encoding(true).GetBytes(value);
                fileStream.Write(info, 0, info.Length);
                fileStream.WriteByte(0xA); //Newline /n
            }
        }
    }
}
