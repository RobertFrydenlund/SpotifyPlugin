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
        private int updateRate;
        
        private bool _active = true;
        public bool active { get { return _active; } }

        public string rawData;

        string oauth;
        string csrf;
        TimeoutWebClient wcoauth;
        TimeoutWebClient wccsrf;

        private static Process[] procs;

        public SpotifyAPI(int updateRate, string token)
        {
            Rm.Log(Rm.LogType.Debug, "SpotifyAPI started...");
            //Out.Log(Verbosity.DEBUG, "SpotifyAPI started...");
            this.updateRate = updateRate;
            this.oauth = token;
            // Start mining thread...
            Thread t = new Thread(() => Run());
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
                wccsrf = new TimeoutWebClient();
                wccsrf.Timeout = StatusControl.timeout;

                // Must have these headers
                wccsrf.Headers.Add("Origin", "https://embed.spotify.com");
                wccsrf.Headers[HttpRequestHeader.UserAgent] = String.Format("SpotifyPlugin {0}", System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString());


                wcoauth = new TimeoutWebClient();
                wcoauth.Timeout = StatusControl.timeout;
                wcoauth.Headers[HttpRequestHeader.UserAgent] = String.Format("SpotifyPlugin {0}", System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString());
                #endregion

                // Authenticate
                CheckAuthentication();

                // Start gathering loop
                Gather();
            }
            catch (Exception e)
            {
                Out.ChrashDump(e);
            }
            finally
            {
                _active = false;
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
                Rm.Log(Rm.LogType.Debug, "Spotify is not running");
                //Out.Log(Verbosity.WARNING, "Spotify is not running");
                return false;
            }
            else
            {
                // Spotify is running
                procs[0].Exited += Spotify_Exited;

                if (Process.GetProcessesByName("SpotifyWebHelper").Length < 1)
                {
                    Rm.Log(Rm.LogType.Warning, "SpotifyWebHelper is not running");
                    //Out.Log(Verbosity.WARNING, "SpotifyWebHelper is not running");
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
                System.Diagnostics.Process.Start(spotifyPath.Replace("spotify.exe", "Data\\SpotifyWebHelper.exe"));
            }
            catch
            {
                Rm.Log(Rm.LogType.Debug, "WebHelper not found in default path, checking old version");
                //Out.Log(Verbosity.DEBUG, "WebHelper not found in default path, checking old version");
                try
                {
                    System.Diagnostics.Process.Start(spotifyPath.Replace("spotify.exe", "SpotifyWebHelper.exe"));
                }
                catch
                {
                    Rm.Log(Rm.LogType.Error, "Can't find SpotifyWebHelper.exe");
                    //Out.Log(Verbosity.ERROR, "Can't find SpotifyWebHelper.exe");
                    throw new System.IO.FileNotFoundException("Can't find SpotifyWebHelper.exe");
                }
            }
        }

        void Spotify_Exited(object sender, EventArgs e)
        {
            // Shut down web helpers as well
            Process[] helperProcs = Process.GetProcessesByName("SpotifyWebHelper");
            for (int i = 0; i < helperProcs.Length; i++)
            {
                // Kill all webhelpers
                helperProcs[i].Kill();
            }

            _active = false;
        }

        private void CheckAuthentication()
        {
            // CSRF
            string rcsrf = wccsrf.DownloadString("http://localhost:4380/simplecsrf/token.json");
            csrf = JObject.Parse(rcsrf).GetValue("token").ToString();

            //Checking if old token is valid
            string authCheck = wccsrf.DownloadString("http://localhost:4380/remote/status.json?&oauth=" + oauth + "&csrf=" + csrf);

            // TODO Should have a proper json convert here, in case there are songs named ""error""
            if (authCheck.Contains("\"error\""))
            {
                Rm.Log(Rm.LogType.Warning, "Invalid Token");
                //Out.Log(Verbosity.WARNING, "Invalid Token");
                SetupAuthentication();
            }
        }

        private void SetupAuthentication()
        {
            Rm.Log(Rm.LogType.Debug, "Fetching new token");
            //Out.Log(Verbosity.DEBUG, "Fetching new token");
            // OAUTH
            string roauth = wcoauth.DownloadString("https://open.spotify.com/token");

            if (!_active) throw new ThreadStateException("API thread no longer active");

            oauth = JObject.Parse(roauth).GetValue("t").ToString();
            Rm.Log(Rm.LogType.Debug, String.Format("Recieved token {0}", oauth));
            //Out.Log(Verbosity.DEBUG, "Recieved token {0}", oauth);

            // CSRF
            string rcsrf = wccsrf.DownloadString("http://localhost:4380/simplecsrf/token.json");
            csrf = JObject.Parse(rcsrf).GetValue("token").ToString();
        }

        private void Gather()
        {
            try
            {
                while (StatusControl.lastCall.Seconds < 5)
                {
                    rawData = wccsrf.DownloadString("http://localhost:4380/remote/status.json?&oauth=" + oauth + "&csrf=" + csrf);

                    Status s = JsonConvert.DeserializeObject<Status>(rawData);
                    s.token = oauth;

                    // TODO bad way to handle this
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
                Out.ChrashDump(e);
            }
            finally
            {
                _active = false;
            }
        }
    }
}