using SpotifyAPI.Local;
using SpotifyAPI.Local.Models;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System.Diagnostics;
using System.Threading;
using System;

namespace SpotifyPlugin
{
    public class Parent
    {
        public StatusResponse Status { get { return status; } }
        private StatusResponse status;

        public bool SpotifyIsRunning;
        
        private SpotifyLocalAPI LocalAPI;
        public SpotifyWebAPI WebAPI;
        Process Spotify;

        public Parent()
        {
            LocalAPI = new SpotifyLocalAPI();
            LocalAPI.Connect();
            WebAPIFactory waf = new WebAPIFactory("http://127.0.0.1", 7476, "82910a2ee5b34426b1a87e7c672e2284", SpotifyAPI.Web.Enums.Scope.UserReadPlaybackState | SpotifyAPI.Web.Enums.Scope.UserModifyPlaybackState);
            // TODO blocking
            
            Thread t = new Thread(() => WebAPI = waf.GetWebApi().Result);
            t.Start();

            
            System.Timers.Timer timer = new System.Timers.Timer(50);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            status = LocalAPI.GetStatus();
            if (status == null) return;

        }

        public bool Continue()
        {
            if (Spotify != null && !Spotify.HasExited) return true;

            var procs = Process.GetProcessesByName("Spotify");

            if (procs.Length < 1) return false;

            Spotify = procs[0];
            return true;
        }

        internal void Reconnect()
        {
            LocalAPI.Connect();
        }
    }
}
