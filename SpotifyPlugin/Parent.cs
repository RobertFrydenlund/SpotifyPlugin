using Rainmeter;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Threading;

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
                if (_timer != null)
                {
                    _timer.Interval = value;
                }
            }
        }

        private readonly System.Timers.Timer _timer;

        public PlaybackContext Status { get; private set; }
        public SpotifyWebAPI WebApi;

        private readonly string _clientSecret = APIKeys.ClientSecret;
        private readonly string _clientId = APIKeys.ClientId;
        private readonly Scope _scope = Scope.UserReadPlaybackState | Scope.UserModifyPlaybackState;

        private readonly int _timeout = 20;
        
        private bool _authenticating;

        private Token _token;
        public double SecondsToExpiration => (_token?.ExpiresIn - (DateTime.Now - _token?.CreateDate).GetValueOrDefault().TotalSeconds).GetValueOrDefault();

        public Parent()
        {
            CheckAuthentication();

            _timer = new System.Timers.Timer(RefreshRate);
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (SecondsToExpiration < 60)
            {
                Out.Log(API.LogType.Notice, "Token expires soon.");
                CheckAuthentication();
                return;
            }
            try
            {
                Status = WebApi.GetPlayback();
            }
            catch
            {
                CheckAuthentication();
            }
        }


        public void CheckAuthentication()
        {
            if (_authenticating) return;
            _authenticating = true;
            new Thread(Authenticate).Start();
            
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
            try
            {
                Out.Log(API.LogType.Notice, "Refreshing token.");
                _token = authentication.RefreshToken(Properties.Settings.Default.RefreshToken, _clientSecret);
                if (_token.Error == null)
                {
                    WebApi = ApiFromToken(_token);
                    _authenticating = false;
                    return;
                }
            }
            catch
            {
                Thread.Sleep(1000);
                _authenticating = false;
                return;
            }

            Out.Log(API.LogType.Notice, "Token refresh failed, opening authentication window.");
            AutoResetEvent authenticationWaitFlag = new AutoResetEvent(false);
            WebApi = null;
            authentication.OnResponseReceivedEvent += (response) =>
            {
                WebApi = HandleSpotifyResponse(response, authentication);
                authenticationWaitFlag.Set();
            };

            try
            {
                authentication.StartHttpServer(7476);

                authentication.DoAuth();

                authenticationWaitFlag.WaitOne(TimeSpan.FromSeconds(_timeout));
                if (WebApi == null)
                    throw new TimeoutException($"No valid response received for the last {_timeout} seconds");
            }
            finally
            {
                authentication.StopHttpServer();
            }
            _authenticating = false;
        }

        private SpotifyWebAPI HandleSpotifyResponse(AutorizationCodeAuthResponse response,
            AutorizationCodeAuth authentication)
        {
            if (response.State != "XSS")
                throw new SpotifyWebApiException($"Wrong state '{response.State}' received.");

            if (response.Error != null)
                throw new SpotifyWebApiException($"Error: {response.Error}");

            var code = response.Code;

            _token = authentication.ExchangeAuthCode(code, _clientSecret);

            Properties.Settings.Default.RefreshToken = _token.RefreshToken;
            Properties.Settings.Default.Save();

            return ApiFromToken(_token);
        }

        private static SpotifyWebAPI ApiFromToken(Token token)
        {
            var spotifyWebApi = new SpotifyWebAPI()
            {
                UseAuth = true,
                AccessToken = token.AccessToken,
                TokenType = token.TokenType
            };
            return spotifyWebApi;
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
            if (WebApi == null) return;
            ErrorResponse er = WebApi.ResumePlayback();
            if (CorrectResponse(er)) return;
            Out.Log(API.LogType.Warning, er.Error.Message);
        }

        public void Pause()
        {
            if (WebApi == null) return;
            ErrorResponse er = WebApi.PausePlayback();
            if (CorrectResponse(er)) return;
            Out.Log(API.LogType.Warning, er.Error.Message);
        }

        public void Next()
        {
            if (WebApi == null) return;
            ErrorResponse er = WebApi.SkipPlaybackToNext();
            if (CorrectResponse(er)) return;
        }

        public void Previous(double skipThreshold)
        {
            if (WebApi == null) return;
            double playingPosition = 1.0 * (Status?.ProgressMs).GetValueOrDefault() / 1000;
            if (playingPosition < skipThreshold)
            {
                ErrorResponse er = WebApi.SkipPlaybackToPrevious();
                if (CorrectResponse(er)) return;
            }
            else
            {
                Seek(0);
                ErrorResponse er = WebApi.SkipPlaybackToPrevious();
                if (CorrectResponse(er)) return;
            }
        }

        public void Seek(int positionMs)
        {
            if (WebApi == null) return;
            ErrorResponse er = WebApi.SeekPlayback(positionMs);
            if (CorrectResponse(er)) return;
        }

        public void SetVolume(int volume)
        {
            if (WebApi == null) return;
            ErrorResponse er = WebApi.SetVolume(volume);
            if (CorrectResponse(er)) return;
        }

        public void SetShuffle(bool shuffle)
        {
            if (WebApi == null) return;
            ErrorResponse er = WebApi.SetShuffle(shuffle);
            if (CorrectResponse(er)) return;
        }

        public void SetRepeat(RepeatState repeat)
        {
            if (WebApi == null) return;
            ErrorResponse er = WebApi.SetRepeatMode(repeat);
            if (CorrectResponse(er)) return;
        }

        private static bool CorrectResponse(ErrorResponse er)
        {
            if (!er.HasError()) return true;
            Out.Log(API.LogType.Warning, $"Error {er.Error.Status}: {er.Error.Message}");
            if (er.Error.Status == 401)
            {
                //CheckAuthentication();
            }
            return false;
        }
    }
}
