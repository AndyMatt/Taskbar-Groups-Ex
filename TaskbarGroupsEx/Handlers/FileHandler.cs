using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarGroupsEx.Handlers
{
    static class FileHandler
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
    }
}
