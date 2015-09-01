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
        public SpotifyAPI(int updateRate, string token)
        {
            this.updateRate = updateRate;
            this.oauth = token;
            // Start mining thread...
            Thread t = new Thread(() => Run());
            t.Start();
        }

        private void Run()
        {
            // Web Client config
            wc = new WebClient();
            wc.Headers.Add("Origin", "https://embed.spotify.com");
            wc.Headers[HttpRequestHeader.UserAgent] = String.Format("SpotifyPlugin {0}", System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString());

            SetupProcesses();
            try
            {
                CheckAuthentication();

                Gather();
            }
            catch (Exception e)
            {
                Out.Log("ERROR: " + e.Message, Verbosity.ERROR);
            }
        }


        private void SetupProcesses()
        {
            // Dont know if i need this, but just in case
            Process[] procs = Process.GetProcessesByName("Spotify");

            if (procs.Length < 1)
            {
                active = false;
                Out.Log("Spotify is not running", Verbosity.WARNING);
                // Shut down web helper as well
                if (Process.GetProcessesByName("SpotifyWebHelper").Length > 0)
                {
                    Process.GetProcessesByName("SpotifyWebHelper")[0].Kill();
                }
                return;
            }
            else
            {
                if (Process.GetProcessesByName("SpotifyWebHelper").Length < 1)
                {
                    Out.Log("SpotifyWebHelper is not running", Verbosity.WARNING);
                    try
                    {
                        System.Diagnostics.Process.Start(procs[0].MainModule.FileName.Replace("Spotify.exe", "SpotifyWebHelper.exe"));
                    }
                    catch (Exception e)
                    {
                        Out.Log("WebHelper not found in default path, checking old version", Verbosity.DEBUG);
                        try
                        {
                            System.Diagnostics.Process.Start(procs[0].MainModule.FileName.Replace("Spotify.exe", "Data\\SpotifyWebHelper.exe"));
                        }
                        catch
                        {
                            Out.Log("Can't find SpotifyWebHelper.exe", Verbosity.ERROR);
                            return;
                        }
                    }
                }
            }
        }

        private void CheckAuthentication()
        {
            // CSRF
            string rcsrf = wc.DownloadString("http://localhost:4380/simplecsrf/token.json");
            csrf = JObject.Parse(rcsrf).GetValue("token").ToString();

            //Checking if old token is valid
            string authCheck = wc.DownloadString("http://localhost:4380/remote/status.json?&oauth=" + oauth + "&csrf=" + csrf);

            // TODO Should have a proper json convert here, in case there are songs named ""error""
            if (authCheck.Contains("\"error\""))
            {
                Out.Log("Invalid Token", Verbosity.WARNING);
                SetupAuthentication();
            }
        }

        private void SetupAuthentication()
        {
            Out.Log("Fetching new token", Verbosity.DEBUG);
            // OAUTH
            string roauth = wc.DownloadString("https://open.spotify.com/token");
            oauth = JObject.Parse(roauth).GetValue("t").ToString();

            // TODO might not be needed
            // CSRF
            string rcsrf = wc.DownloadString("http://localhost:4380/simplecsrf/token.json");
            csrf = JObject.Parse(rcsrf).GetValue("token").ToString();
        }

        private void Gather()
        {
            try
            {
                while (StatusControl.lastCall.Seconds < 5)
                {
                    rawData = wc.DownloadString("http://localhost:4380/remote/status.json?&oauth=" + oauth + "&csrf=" + csrf);

                    Status s = JsonConvert.DeserializeObject<Status>(rawData);
                    s.token = oauth;

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
