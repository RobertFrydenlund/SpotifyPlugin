using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Rainmeter;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace SpotifyPlugin
{
    public class Parent
    {
        private int _refreshRate = 500;
        public int RefreshRate
        {
            get => _refreshRate;
            set
            {
                _refreshRate = value;
                if (timer != null)
                {
                    timer.Interval = value;
                }
            }
        }

        private System.Timers.Timer timer;

        public SpotifyAPI.Web.Models.PlaybackContext Status { get; private set; }
        public SpotifyWebAPI WebAPI;

        private readonly string _clientSecret = APIKeys.ClientSecret;
        private readonly string _clientId = APIKeys.ClientId;
        private readonly Scope _scope = Scope.UserReadPlaybackState | Scope.UserModifyPlaybackState;

        private readonly int _timeout = 20;

        private Task<SpotifyWebAPI> authenticationTask;

        public Parent()
        {
            WebAPIFactory webApiFactory = new WebAPIFactory("http://127.0.0.1", 7476, APIKeys.ClientId, _scope);
            authenticationTask = webApiFactory.GetWebApi();
            WebAPI = authenticationTask.Result;
            
            
            timer = new System.Timers.Timer(RefreshRate);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Status = WebAPI.GetPlayback();
        }


        public void CheckAuthentication()
        {
            // Set up web API in different thread
            if (WebAPI == null)
            {
                Out.Log(API.LogType.Notice, "Setting up web API.");
                new Thread(Authenticate).Start();
            }
        }

        public void Authenticate()
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

        private SpotifyWebAPI HandleSpotifyResponse(AutorizationCodeAuthResponse response,
            AutorizationCodeAuth authentication)
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

        public void PlayPause()
        {
            if (Status.IsPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        public void Play()
        {
            ErrorResponse er = WebAPI.ResumePlayback();
            if (CorrectResponse(er)) return;
        }

        public void Pause()
        {
            ErrorResponse er = WebAPI.PausePlayback();
            if (CorrectResponse(er)) return;
        }

        public void Next()
        {
            ErrorResponse er = WebAPI.SkipPlaybackToNext();
            if (CorrectResponse(er)) return;
        }

        public void Previous(double skipThreshold)
        {
            double playingPosition = (Status?.ProgressMs).GetValueOrDefault()/1000;
            if (playingPosition < skipThreshold)
            {
                ErrorResponse er = WebAPI.SkipPlaybackToPrevious();
                if (CorrectResponse(er)) return;
            }
            else
            {
                Seek(0);
                ErrorResponse er = WebAPI.SkipPlaybackToPrevious();
                if (CorrectResponse(er)) return;
            }
        }

        public void Seek(int positionMs)
        {
            ErrorResponse er = WebAPI.SeekPlayback(positionMs);
            if (CorrectResponse(er)) return;
        }

        public void SetVolume(int volume)
        {
            ErrorResponse er = WebAPI.SetVolume(volume);
            if (CorrectResponse(er)) return;
        }

        public void SetShuffle(bool shuffle)
        {
            ErrorResponse er = WebAPI.SetShuffle(shuffle);
            if (CorrectResponse(er)) return;
        }

        public void SetRepeat(RepeatState repeat)
        {
            ErrorResponse er = WebAPI.SetRepeatMode(repeat);
            if (CorrectResponse(er)) return;
        }

        private bool CorrectResponse(ErrorResponse er)
        {
            if (!er.HasError()) return true;
            Out.Log(API.LogType.Notice, $"Error {er.Error.Status}: {er.Error.Message}");
            if (er.Error.Status == 401)
            {
                //CheckAuthentication();
            }
            return false;
        }
    }
}
