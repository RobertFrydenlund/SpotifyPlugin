using SpotifyAPI.Local;
using SpotifyAPI.Local.Models;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System.Threading.Tasks;

namespace SpotifyPlugin
{
    public class Parent
    {
        public StatusResponse Status { get { return status; } }
        private StatusResponse status;

        public bool SpotifyIsRunning;

        private SpotifyLocalAPI LocalAPI;
        public SpotifyWebAPI WebAPI;

        public Parent()
        {
            LocalAPI = new SpotifyLocalAPI();
            LocalAPI.Connect();
            WebAPIFactory waf = new WebAPIFactory("http://127.0.0.1", 7476, "82910a2ee5b34426b1a87e7c672e2284", SpotifyAPI.Web.Enums.Scope.UserReadPlaybackState | SpotifyAPI.Web.Enums.Scope.UserModifyPlaybackState);

            // TODO
            Task.Run(() => WebAPI = waf.GetWebApi().Result);
            //Thread t = new Thread(() => WebAPI = waf.GetWebApi().Result);
            //t.Start();

            System.Timers.Timer timer = new System.Timers.Timer(50);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            status = LocalAPI.GetStatus();
            if (status == null) return; // Spotify is probably not running
            if (status.Track == null)
            {
                // Spotify is running, but we are not recieving track info, reconnect
                try
                {
                    LocalAPI.Connect();
                }
                catch (System.Net.WebException) { }
            }
        }
    }
}
