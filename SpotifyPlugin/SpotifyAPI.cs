using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;

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
        TimeoutWebClient wc;

        /// <summary>
        /// </summary>
        /// <param name="updateRate"> ms between each update</param>
        public SpotifyAPI(int updateRate, string token)
        {
            Out.Log(Verbosity.DEBUG, "SpotifyAPI started...");
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
                if (!SetupProcesses())
                {
                    return;
                }

                // Web Client config
                wc = new TimeoutWebClient();
                wc.Timeout = 1000;
                // Must have these headers to avoid 404
                wc.Headers.Add("Origin", "https://embed.spotify.com");
                wc.Headers[HttpRequestHeader.UserAgent] = String.Format("SpotifyPlugin {0}", System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString());

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


        private bool SetupProcesses()
        {
            // Dont know if i need this, but just in case
            Process[] procs = Process.GetProcessesByName("Spotify");
            if (procs.Length < 1)
            {
                Out.Log(Verbosity.WARNING, "Spotify is not running");

                // Shut down web helpers as well
                Process[] helperProc = Process.GetProcessesByName("SpotifyWebHelper");

                for (int i = 0; i < helperProc.Length; i++)
                {
                    // Kill all webhelpers
                    helperProc[i].Kill();
                }
                return false;
            }
            else
            {
                if (Process.GetProcessesByName("SpotifyWebHelper").Length < 1)
                {
                    Out.Log(Verbosity.WARNING, "SpotifyWebHelper is not running");
                    try
                    {
                        System.Diagnostics.Process.Start(procs[0].MainModule.FileName.ToLower().Replace("spotify.exe", "Data\\SpotifyWebHelper.exe"));
                    }
                    catch (Exception e)
                    {
                        Out.Log( Verbosity.DEBUG, "WebHelper not found in default path, checking old version");
                        try
                        {
                            System.Diagnostics.Process.Start(procs[0].MainModule.FileName.ToLower().Replace("spotify.exe", "SpotifyWebHelper.exe"));
                        }
                        catch
                        {
                            Out.Log(Verbosity.ERROR,"Can't find SpotifyWebHelper.exe");
                            return false;
                        }
                    }
                }
            }
            return true;
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
                Out.Log(Verbosity.WARNING, "Invalid Token");
                SetupAuthentication();
            }
        }

        private void SetupAuthentication()
        {
            Out.Log(Verbosity.DEBUG, "Fetching new token");
            // OAUTH
            string roauth = wc.DownloadString("https://open.spotify.com/token");
            oauth = JObject.Parse(roauth).GetValue("t").ToString();
            Out.Log(Verbosity.DEBUG, "Recieved token {0}", oauth);

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
                Out.ChrashDump(e);
            }
            finally
            {
                _active = false;
            }
        }
    }
}
