using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Threading;

namespace SpotifyPlugin
{
    public static class StatusControl
    {

        private static SpotifyAPI sAPI;
        private static Status status = new Status();

        private static bool useCover = true;
        private static string albumURI = "";

        private static DateTime lastCallTime = DateTime.Now;
        public static TimeSpan lastCall { get { return DateTime.Now - lastCallTime; } }

        public static int timeout = 5000;

        public static String CoverPath = "";

        public static Status Current_Status
        {
            get
            {
                lastCallTime = DateTime.Now;
                if (sAPI == null || sAPI.Active == false)
                {
                    sAPI = new SpotifyAPI(200, status.token);
                }
                return status;
            }
            set
            {
                status = value;
            }
        }

        public static string getArt(int resolution, string defaultPath, string coverPath)
        {
            // Image changed
            if (albumURI != status.track.album_resource.uri)
            {
                // Update URI
                albumURI = status.track.album_resource.uri;
                // Default image
                useCover = false;
                // Get image in separate thread
                Thread t = new Thread(() => GetAlbumImage(resolution, coverPath));
                t.Start();
            }
            return useCover ? coverPath : defaultPath;
        }

        private static byte[] ReadStream(Stream input)
        {
            byte[] buffer = new byte[1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public static void GetImageFromUrl(string url, string filePath)
        {
            // Create http request
            HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            using (HttpWebResponse httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse())
            {

                // Read as stream
                using (Stream stream = httpWebReponse.GetResponseStream())
                {
                    Byte[] buffer = ReadStream(stream);
                    // Make sure the path folder exists
                    System.IO.Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Rainmeter/SpotifyPlugin");
                    // Write stream to file
                    File.WriteAllBytes(filePath, buffer);
                }
            }
            // Change back to cover image
            useCover = true;
            CoverPath = url;
            Out.Log(Verbosity.DEBUG, "Artwork updated");
        }

        public static void GetAlbumImage(int resolution, string filePath)
        {
            try
            {
                string rawData;
                using (var webpage = new WebClient())
                {
                    // Request gets ignored if not called from a proper browser
                    // webpage.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                    webpage.Headers[HttpRequestHeader.UserAgent] = String.Format("SpotifyPlugin {0}", System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString());

                    Out.Log(Verbosity.DEBUG, "Downloading embed page: {0}", status.track.album_resource.uri);
                    rawData = webpage.DownloadString("https://embed.spotify.com/oembed/?url=" + status.track.album_resource.uri);
                }

                JObject jo = JObject.Parse(rawData);
                // Retrieve cover url
                string imgUrl = jo.GetValue("thumbnail_url").ToString();

                // Specify album resolution
                imgUrl = imgUrl.Replace("cover", resolution.ToString());

                Out.Log(Verbosity.DEBUG, "Artwork found, downloading image...");

                GetImageFromUrl(imgUrl, filePath);

            }
            catch (Exception e)
            {
                Out.ChrashDump(e);
            }
        }

        public static string getData()
        {
            if (sAPI != null)
            {
                return sAPI.rawData;
            }
            return "No data gathered yet.";
        }

    }
}
