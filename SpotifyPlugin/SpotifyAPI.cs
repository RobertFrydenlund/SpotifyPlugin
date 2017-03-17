using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Rm = Rainmeter.API;

namespace SpotifyPlugin
{
    public class SpotifyAPI
    {

        // Whoa, just made a big discovery about queueing! Using play.json you can append '?action=queue' suffix to the uri parameter and the song goes to queue.Awesome! - http://cgbystrom.com/articles/deconstructing-spotifys-builtin-http-server/
        private int updateRate;

        public bool Active { get; private set; } = true;

        public string rawData;

        private string oauth;
        private string csrf;

        TimeoutWebClient wc;

        private static Process[] procs;

        public SpotifyAPI(int updateRate, string token)
        {
            Out.Log(Verbosity.DEBUG, "SpotifyAPI started...");

            this.updateRate = updateRate;
            this.oauth = token;

            // Start mining thread...
            Thread t = new Thread(Run);
            t.Start();
        }

        private void Run()
        {
            try
            {
                // Check Processes first
                if (!CheckProcesses())
                    return;

                #region Web Client config
                wc = new TimeoutWebClient {Timeout = StatusControl.timeout};

                // Must have these headers
                wc.Headers.Add("Origin", "https://embed.spotify.com");
                wc.Headers[HttpRequestHeader.UserAgent] = $"SpotifyPlugin {System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString()}";
                #endregion
                
                Gather();
            }
            catch (Exception e)
            {
                Out.ChrashDump(e);
            }
            finally
            {
                Active = false;
            }
        }

        /// <summary>
        /// Checks if Spotify.exe and SpotifyWebHelper.exe is running. 
        /// Makes sure there are webhelpers running.
        /// </summary>
        /// <returns>true if spotify is running</returns>
        private bool CheckProcesses()
        {
            procs = Process.GetProcessesByName("Spotify");
            if (procs.Length < 1)
            {
                Out.Log(Verbosity.DEBUG, "Spotify is not running");
                return false;
            }
            else
            {
                // Spotify is running
                procs[0].Exited += Spotify_Exited;

                if (Process.GetProcessesByName("SpotifyWebHelper").Length < 1)
                {
                    Out.Log(Verbosity.DEBUG, "SpotifyWebHelper is not running");
                    StartWebHelper();
                }
            }
            return true;
        }

        /// <summary>
        /// Starts the WebHelper for the currently running instance of spotify.
        /// </summary>
        private void StartWebHelper()
        {
            string spotifyPath = procs[0].MainModule.FileName.ToLower();
            try
            {
                Process.Start(spotifyPath.Replace("spotify.exe", "SpotifyWebHelper.exe"));
            }
            catch
            {
                try
                {
                    Process.Start(spotifyPath.Replace("spotify.exe", "Data\\SpotifyWebHelper.exe"));
                }
                catch
                {
                    Out.Log(Verbosity.WARNING, "Can't find SpotifyWebHelper.exe, try starting it manually from your current spotify folder");
                }
            }
        }

        private void Spotify_Exited(object sender, EventArgs e)
        {
            // Shut down web helpers as well
            Process[] helperProcs = Process.GetProcessesByName("SpotifyWebHelper");
            foreach (Process process in helperProcs)
            {
                // Kill all webhelpers
                process.Kill();
            }

            Active = false;
        }
        
        private void SetupAuthentication()
        {
            // CSRF
            string rcsrf = wc.DownloadString("http://spotifyPlugin.spotilocal.com:4380/simplecsrf/token.json");
            csrf = JObject.Parse(rcsrf).GetValue("token").ToString();

            Out.Log(Verbosity.DEBUG, "Fetching new token");
            // OAUTH
            // Why does it reset user-agent, but not origin?
            wc.Headers[HttpRequestHeader.UserAgent] = $"SpotifyPlugin {System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString()}";
            string roauth = wc.DownloadString("https://open.spotify.com/token");

            // {"t": "sdfasdfsadgdsfg"}
            oauth = JObject.Parse(roauth).GetValue("t").ToString();

            Out.Log(Verbosity.DEBUG, "Recieved token {0}", oauth);
            
        }

        private void Gather()
        {
            try
            {
                while (StatusControl.lastCall.Seconds < 5)
                {
                    rawData = wc.DownloadString("http://spotifyPlugin.spotilocal.com:4380/remote/status.json?&oauth=" + oauth + "&csrf=" + csrf);

                    //Console.WriteLine(rawData);

                    Status s = JsonConvert.DeserializeObject<Status>(rawData);
                    s.token = oauth;

                    // TODO slightly better
                    if (s.error != null)
                    {
                        switch (s.error.type)
                        {
                            // Invalid 4102-oauth token, 4107-rfid
                            case "4107":
                            case "4102":
                                SetupAuthentication();
                                break;
                            default:
                                throw new JsonException(s.error.type + " - " + s.error.message);
                        }
                    }

                    StatusControl.Current_Status = s;

                    Thread.Sleep(updateRate);
                }
            }
            catch (Exception e)
            {
                Out.ChrashDump(e);
            }
            finally
            {
                Active = false;
            }
        }
    }
}