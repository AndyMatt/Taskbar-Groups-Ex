using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Net.Http;
using System.Security.Policy;
using System.Reflection.PortableExecutable;
using TaskbarGroupsEx.Handlers;
using TaskbarGroupsEx.Classes;

namespace TaskbarGroupsEx.GroupItems
{
    internal class URLGroupItem : DynamicGroupItem
    {
        public override Types GetGroupType() { return Types.URL; }
        public URLGroupItem() : base() { }
        public URLGroupItem(ConfigFile.GroupItemConfig itemConfig) : base(itemConfig) { }

        public URLGroupItem(string url) : base()
        {
            mCommand = url;
            mName = GetTitle();
            mIcon = GetIcon();
        }
        public URLGroupItem(string url, string name) : base()
        {
            mCommand = url;
            mName = name != "" ? name : GetTitle();
            mIcon = GetIcon();
        }

        public URLGroupItem(urlFileHandler urlFileHandle) : base()
        {
            mCommand = urlFileHandle.mCommand;
            mName = urlFileHandle.mName;
            mIcon = GetIcon();
        }

        override public BitmapSource GetIcon()
        {
            if (mIcon == null)
            {
                var FavIconTask = _downloadFavIcon(mCommand!);
                FavIconTask.Wait();
                mIcon = FavIconTask.Result;
            }
            return mIcon;
        }
        private string GetTitle()
        {
            var Titletask = _downloadTitle(mCommand!);
            Titletask.Wait();
            return Titletask.Result;
        }

        public Task<string> _downloadTitle(string url)
        {
            try
            {
                Uri titleUrl = new Uri(url);
                var client = new HttpClient();

                client.DefaultRequestHeaders.Add("User-Agent", "Other");

                var response = client.GetStringAsync(titleUrl);
                response.Wait();

                Match match = Regex.Match(response.Result, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase);
                string title = match.Groups["Title"].Value;
                return Task.FromResult(title);
            }
            catch { }

            return Task.FromResult("");
        }

        public static Task<BitmapSource> _downloadFavIcon(string url)
        {
            try
            {
                Uri favIconURI = new Uri(@"https://t2.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url=" + url + "&size=48");
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Other");
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
            catch { }

            return Task.FromResult((BitmapSource)Application.Current.Resources["ErrorIcon"]);
        }
    }
}
