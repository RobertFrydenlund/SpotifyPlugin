using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace SpotifyPlugin
{
    class SpotifyAPI
    {
        int updateRate;
        public bool active = true;
        public string rawData;

        string oauth;
        string csrf;
        WebClient wc;

        /// <summary>
        /// </summary>
        /// <param name="updateRate"> ms between each update</param>
        public SpotifyAPI(int updateRate)
        {
            this.updateRate = updateRate;
            // Start mining thread...
            Thread t = new Thread(() => Gather());
            t.Start();
        }

        private void Gather()
        {
            if (Process.GetProcessesByName("Spotify").Length < 1)
            {
                active = false;
                Out.Log("Spotify is not running", Verbosity.WARNING);
                return;
            }
            if (Process.GetProcessesByName("SpotifyWebHelper").Length < 1)
            {
                try
                {
                    System.Diagnostics.Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Spotify\\Data\\SpotifyWebHelper.exe");
                }
                catch (Exception e)
                {
                    throw new Exception("Could not launch SpotifyWebHelper. Your installation of Spotify might be corrupt or you might not have Spotify installed", e);
                }
            }

            // Authentication
            try
            {
                wc = new WebClient();

                // For CSRF request
                wc.Headers.Add("Origin", "https://embed.spotify.com");
                wc.Headers[HttpRequestHeader.UserAgent] = "SpotifyPlugin for Rainmeter, http://rainmeter.net/forum/viewtopic.php?f=18&t=17077";
                //wc.Headers.Add("Referer", "https://embed.spotify.com/?uri=spotify:track:59WN2psjkt1tyaxjspN8fp");

                // OAUTH
                string roauth = wc.DownloadString("http://open.spotify.com/token");
                oauth = JObject.Parse(roauth).GetValue("t").ToString();

                // CSRF
                string rcsrf = wc.DownloadString("http://localhost:4380/simplecsrf/token.json");
                csrf = JObject.Parse(rcsrf).GetValue("token").ToString();

            }
            catch 
            {
                active = false;
                return;
            }

            try
            {
                while (StatusControl.lastCall.Seconds < 5)
                {
                    rawData = wc.DownloadString("http://localhost:4380/remote/status.json?&oauth=" + oauth + "&csrf=" + csrf);
                    
                    Status s = JsonConvert.DeserializeObject<Status>(rawData);

                    if (s.track.track_resource.name.Length < 1)
                    {
                        throw new JsonException("Missing data");
                    }

                    StatusControl.Current_Status = s;
                    Thread.Sleep(updateRate);
                }
            }
            catch (Exception e)
            {
                Out.Log(e.Message, Verbosity.WARNING);
            }
            finally
            {
                active = false;
            }
        }
    }
}
