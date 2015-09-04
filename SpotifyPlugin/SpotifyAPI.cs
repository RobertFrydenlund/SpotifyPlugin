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
            Out.Log("SpotifyAPI constructor", Verbosity.WARNING);
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
                SetupProcesses();

                // Web Client config
                wc = new TimeoutWebClient();
                wc.Timeout = 1000;
                wc.Headers.Add("Origin", "https://embed.spotify.com");
                wc.Headers[HttpRequestHeader.UserAgent] = String.Format("SpotifyPlugin {0}", System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString());

                // Authenticate
                CheckAuthentication();

                // start gathering loop
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


        private void SetupProcesses()
        {
            // Dont know if i need this, but just in case
            Process[] procs = Process.GetProcessesByName("Spotify");
            if (procs.Length < 1)
            {
                Out.Log("Spotify is not running", Verbosity.WARNING);

                // Shut down web helper as well
                Process[] helperProc = Process.GetProcessesByName("SpotifyWebHelper");
                if (helperProc.Length > 0)
                {
                    helperProc[0].Kill();
                }
                _active = false;
                return;
            }
            else
            {
                if (Process.GetProcessesByName("SpotifyWebHelper").Length < 1)
                {
                    Out.Log("SpotifyWebHelper is not running", Verbosity.WARNING);
                    try
                    {
                        System.Diagnostics.Process.Start(procs[0].MainModule.FileName.ToLower().Replace("spotify.exe", "Data\\SpotifyWebHelper.exe"));
                    }
                    catch (Exception e)
                    {
                        Out.Log("WebHelper not found in default path, checking old version", Verbosity.DEBUG);
                        try
                        {
                            System.Diagnostics.Process.Start(procs[0].MainModule.FileName.ToLower().Replace("spotify.exe", "SpotifyWebHelper.exe"));
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
                _active = false;
            }
        }
    }
}
