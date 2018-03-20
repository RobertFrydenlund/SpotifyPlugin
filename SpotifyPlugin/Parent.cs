using SpotifyAPI.Local;
using SpotifyAPI.Local.Models;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Threading;
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

        string _clientSecret = "c0d21ff7fb8641398f7ccb5b864b9971";
        string _clientId = "82910a2ee5b34426b1a87e7c672e2284";

        int _timeout = 20;
        SpotifyAPI.Web.Enums.Scope _scope = SpotifyAPI.Web.Enums.Scope.UserReadPlaybackState | SpotifyAPI.Web.Enums.Scope.UserModifyPlaybackState;

        public Parent()
        {
            // Offline only for now, connect when you need to

            LocalAPI = new SpotifyLocalAPI();
            LocalAPI.Connect();
            
            System.Timers.Timer timer = new System.Timers.Timer(50);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            timer.Start();
        }

        public void CheckAuthentication()
        {
            // Set up web API in different thread
            new Thread(Authenticate).Start();
        }

        private void Authenticate()
        {
            AutorizationCodeAuth authentication = new AutorizationCodeAuth
            {
                RedirectUri = new UriBuilder("http://127.0.0.1") { Port = 7476 }.Uri.OriginalString.TrimEnd('/'),
                ClientId = _clientId,
                Scope = _scope,
                State = "XSS"
            };

            // Try refreshing
            var token = authentication.RefreshToken(Properties.Settings.Default.RefreshToken, _clientSecret);
            if (token.Error == null)
            {
                WebAPI = ApiFromToken(token);
                return;
            }

            AutoResetEvent authenticationWaitFlag = new AutoResetEvent(false);
            WebAPI = null;
            authentication.OnResponseReceivedEvent += (response) =>
            {
                WebAPI = HandleSpotifyResponse(response, authentication);
                authenticationWaitFlag.Set();
            };

            try
            {
                authentication.StartHttpServer(7476);

                authentication.DoAuth();

                authenticationWaitFlag.WaitOne(TimeSpan.FromSeconds(_timeout));
                if (WebAPI == null)
                    throw new TimeoutException($"No valid response received for the last {_timeout} seconds");
            }
            finally
            {
                authentication.StopHttpServer();
            }
        }

        private SpotifyWebAPI HandleSpotifyResponse(AutorizationCodeAuthResponse response, AutorizationCodeAuth authentication)
        {
            if (response.State != "XSS")
                throw new SpotifyWebApiException($"Wrong state '{response.State}' received.");

            if (response.Error != null)
                throw new SpotifyWebApiException($"Error: {response.Error}");

            var code = response.Code;

            var token = authentication.ExchangeAuthCode(code, _clientSecret);

            Properties.Settings.Default.RefreshToken = token.RefreshToken;
            Properties.Settings.Default.Save();

            return ApiFromToken(token);
        }

        private SpotifyWebAPI ApiFromToken(SpotifyAPI.Web.Models.Token token)
        {
            var spotifyWebAPI = new SpotifyWebAPI()
            {
                UseAuth = true,
                AccessToken = token.AccessToken,
                TokenType = token.TokenType
            };
            return spotifyWebAPI;
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
