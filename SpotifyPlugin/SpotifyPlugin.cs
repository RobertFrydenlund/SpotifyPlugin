using Rainmeter;
using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace SpotifyPlugin
{
    public class Measure
    {
        /// <summary>
        /// Cover art save path.
        /// </summary>
        public string coverPath = "";

        /// <summary>
        /// Default path.
        /// </summary>
        public string defaultPath = "";

        /// <summary>
        /// Measure type.
        /// </summary>
        public string measureType = "";

        /// <summary>
        /// Cover image resolution.
        /// </summary>
        public int artResolution = 300;

        /// <summary>
        /// Manages actually talking to spotify.
        /// </summary>
        private Parent parent;

        /// <summary>
        /// If playing position is larger than this value, ExecuteBang("previous") will start song over instead of skipping to previous.
        /// </summary>
        public double skipThreshold = 4;

        public Measure(Parent parent)
        {
            this.parent = parent;
        }

        public void Reload(Rainmeter.API rm, ref double maxValue)
        {
            measureType = rm.ReadString("Type", "").ToLowerInvariant();
            if (measureType == "albumart")
            {
                // TODO get a proper default path
                coverPath = rm.ReadPath("CoverPath", "");
                defaultPath = rm.ReadPath("DefaultPath", "");
                artResolution = rm.ReadInt("Res", 300);
            }
        }

#if DEBUG
        public string GetString()
#else
        internal string GetString()
#endif
        {
            switch (measureType)
            {
                case "trackname":
                case "track":
                    return parent.Status?.Track?.TrackResource?.Name ?? "";

                case "artistname":
                case "artist":
                    return parent.Status?.Track?.ArtistResource?.Name ?? "";

                case "albumname":
                case "album":
                    return parent.Status?.Track?.AlbumResource?.Name ?? "";

                case "trackuri":
                    return parent.Status?.Track?.TrackResource?.Uri ?? "";

                case "albumuri":
                    return parent.Status?.Track?.AlbumResource?.Uri ?? "";

                case "artisturi":
                    return parent.Status?.Track?.ArtistResource?.Uri ?? "";

                case "position":
                    double playingPosition = (parent.Status?.PlayingPosition).GetValueOrDefault();
                    double sec = Math.Floor(playingPosition % 60);
                    double min = Math.Floor(playingPosition / 60);
                    return String.Format("{0}:{1}", min.ToString("#00"), sec.ToString("00"));

                case "duration":
                case "length":
                    double trackLength = (parent.Status?.Track?.Length).GetValueOrDefault();
                    double secl = Math.Floor(trackLength % 60);
                    double minl = Math.Floor(trackLength / 60);
                    return String.Format("{0}:{1}", minl.ToString("#00"), secl.ToString("00"));

                // TODO
                case "albumart":
                case "cover":
                    return AlbumArt.getArt(parent.Status?.Track?.AlbumResource?.Uri, artResolution, defaultPath, coverPath);
            }
            // MeasureType.Major, MeasureType.Minor, and MeasureType.Number are
            // numbers. Therefore, null is returned here for them. This is to
            // inform Rainmeter that it can treat those types as numbers.

            return null;
        }

#if DEBUG
        public double Update()
#else
        internal double Update()
#endif
        {
            switch (measureType)
            {
                case "volume":
                    return (parent.Status?.Volume).GetValueOrDefault();

                case "repeat":
                    return (parent.Status?.Repeat).GetValueOrDefault() ? 1 : 0;

                case "shuffle":
                    return (parent.Status?.Shuffle).GetValueOrDefault() ? 1 : 0;

                case "position":
                    return (parent.Status?.PlayingPosition).GetValueOrDefault();

                case "playing":
                    return (parent.Status?.Playing).GetValueOrDefault() ? 1 : 0;

                case "length":
                    return (parent.Status?.Track?.Length).GetValueOrDefault();

                case "progress":
                    double? o = parent.Status?.PlayingPosition / parent.Status?.Track?.Length;
                    return o.GetValueOrDefault();
            }
            //API.Log(API.LogType.Error, "SpotifyPlugin: Type=" + measureType + " not valid");
            return 0.0;
        }


        internal void ExecuteBang(string arg)
        {
            // TODO Thread race, fails command when authenticating, ignore for now
            parent.CheckAuthentication();

            // TODO Cheater
            Thread t = new Thread(() =>
            {
                // TODO Really?
                try
                {
                    string[] args = Regex.Split(arg.ToLowerInvariant(), " ");
                    switch (args[0])
                    {
                        // Single commands
                        case "playpause":
                            if (parent.Status.Playing)
                            {
                                goto case "pause";
                            }
                            else
                            {
                                goto case "play";
                            }
                        case "play":
                            parent.WebAPI.ResumePlayback();
                            return;
                        case "pause":
                            parent.WebAPI.PausePlayback();
                            return;
                        case "next":
                            parent.WebAPI.SkipPlaybackToNext();
                            return;
                        case "previous":
                            double playingPosition = (parent.Status?.PlayingPosition).GetValueOrDefault();
                            if (playingPosition < skipThreshold)
                            {
                                parent.WebAPI.SkipPlaybackToPrevious();
                            }
                            else
                            {
                                parent.WebAPI.SeekPlayback(0);
                            }
                            return;

                        // Double commands
                        case "volume":
                            if (!Int32.TryParse(args[1], out int volume) && volume > 100 && volume < 0)
                            {
                                API.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}. {args[1]} should be an integer between 0 and 100.");
                                return;
                            }
                            parent.WebAPI.SetVolume(volume);
                            return;
                        case "seek":
                            if (!Int32.TryParse(args[1], out int seek))
                            {
                                API.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}. {args[1]} should be an integer.");
                                return;
                            }
                            parent.WebAPI.SeekPlayback(seek);
                            return;
                        case "seekpercent":
                        case "setposition":
                            if (!float.TryParse(args[1], out float position))
                            {
                                API.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}. {args[1]} should be a number from 0 to 100.");
                                return;
                            }
                            // TODO error 405
                            parent.WebAPI.SeekPlayback((int)(parent.Status.Track.Length * position) / 100);
                            return;
                        case "shuffle":
                        case "setshuffle":
                            if (!ShuffleTryParse(args[1], out bool shuffle))
                            {
                                API.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}. {args[1]} should be either -1, 0, 1, True or False");
                                return;
                            }
                            parent.WebAPI.SetShuffle(shuffle);
                            return;
                        case "repeat":
                        case "setrepeat":
                            if (!RepeatTryParse(args[1], out RepeatState repeat))
                            {
                                API.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}. {args[1]} should be either Off, Track, Context, -1, 0, 1 or 2");
                                return;
                            }
                            parent.WebAPI.SetRepeatMode(repeat);
                            return;
                        default:
                            API.Log(API.LogType.Warning, $"Unknown command: {arg}");
                            break;
                    }
                }
                catch (Exception e)
                {
                    API.Log(API.LogType.Error, $"{e.Message} \n {e.StackTrace}");
                }
            });

            t.Start();

        }

        private bool RepeatTryParse(string value, out RepeatState repeat)
        {
            switch (value)
            {
                case null:
                    repeat = RepeatState.Off;
                    return false;
                case "-1":
                    PlaybackContext pc = parent.WebAPI.GetPlayback();
                    RepeatState repeatState = pc.RepeatState;
                    switch (repeatState)
                    {
                        case RepeatState.Track:
                            repeat = RepeatState.Context;
                            break;
                        case RepeatState.Context:
                            repeat = RepeatState.Off;
                            break;
                        case RepeatState.Off:
                            repeat = RepeatState.Track;
                            break;
                        default:
                            repeat = RepeatState.Off;
                            return false;
                    }
                    return true;
                case "0":
                    repeat = RepeatState.Off;
                    return true;
                default:
                    return Enum.TryParse(value, out repeat);
            }
        }

        private bool ShuffleTryParse(string value, out bool shuffle)
        {
            switch (value)
            {
                case null:
                    shuffle = false;
                    return false;
                case "-1":
                    shuffle = !parent.Status.Shuffle;
                    return true;
                case "0":
                    shuffle = false;
                    return true;
                case "1":
                    shuffle = true;
                    return true;
                default:
                    return bool.TryParse(value, out shuffle);
            }
        }
    }

    public static class Plugin
    {
        static IntPtr StringBuffer = IntPtr.Zero;

        static Parent parent;

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            if (parent == null) { parent = new Parent(); }
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure(parent)));
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            GCHandle.FromIntPtr(data).Free();

            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new Rainmeter.API(rm), ref maxValue);
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            return measure.Update();
        }

        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }

            string stringValue = measure.GetString();
            if (stringValue != null)
            {
                StringBuffer = Marshal.StringToHGlobalUni(stringValue);
            }

            return StringBuffer;
        }

        [DllExport]
        public static void ExecuteBang(IntPtr data, IntPtr args)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.ExecuteBang(Marshal.PtrToStringUni(args));
        }
    }
}
