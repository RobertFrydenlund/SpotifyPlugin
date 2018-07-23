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
        public string coverPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SpotifyPlugin\cover.png";

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
                    return parent.Status?.Item?.Name ?? "";

                case "artistname":
                case "artist":
                    var artists = parent.Status?.Item?.Artists;
                    if (artists == null) return "";
                    string result = "";
                    foreach (SimpleArtist artist in artists)
                    {
                        if (result.Length != 0)
                        {
                            result += ", ";
                        }
                        result += artist.Name;
                    }
                    return result;

                case "albumname":
                case "album":
                    return parent.Status?.Item?.Album?.Name ?? "";

                case "trackuri":
                    return parent.Status?.Item?.Uri ?? "";

                case "albumuri":
                    return parent.Status?.Item?.Album.Uri ?? "";

                case "artisturi":
                    // TODO
                    //return parent.Status?.Track?.ArtistResource?.Uri ?? "";
                    return "not implemented yet";

                case "position":
                    TimeSpan position = TimeSpan.FromMilliseconds((parent.Status?.ProgressMs).GetValueOrDefault());
                    return position.ToString(@"mm\:ss");

                case "duration":
                case "length":
                    TimeSpan duration = TimeSpan.FromMilliseconds((parent.Status?.Item?.DurationMs).GetValueOrDefault());
                    return duration.ToString(@"mm\:ss");

                // TODO
                case "albumart":
                case "cover":
                    return AlbumArt.getArt(parent.Status?.Item?.Album?.Uri, artResolution, defaultPath, coverPath);

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
                    return (parent.Status?.Device?.VolumePercent).GetValueOrDefault();

                case "repeat":
                    return (int)(parent.Status?.RepeatState).GetValueOrDefault();

                case "shuffle":
                    return (parent.Status?.ShuffleState).GetValueOrDefault() ? 1 : 0;

                case "position":
                    return (parent.Status?.ProgressMs).GetValueOrDefault();

                case "playing":
                    return (parent.Status?.IsPlaying).GetValueOrDefault() ? 1 : 0;

                case "length":
                    return (parent.Status?.Item?.DurationMs).GetValueOrDefault();

                case "progress":
                    double? o = parent.Status?.ProgressMs / parent.Status?.Item?.DurationMs;
                    return o.GetValueOrDefault();
            }
            //API.Log(API.LogType.Error, "SpotifyPlugin: Type=" + measureType + " not valid");
            return 0.0;
        }

        internal void ExecuteBang(string arg)
        {
            Thread t = new Thread(() => Execute(arg));
            t.Start();
        }
        
        public void Execute(string arg)
        {
            if(parent.Status == null) return;
            string[] args = Regex.Split(arg.ToLowerInvariant(), " ");
            if (args.Length == 0) { Out.Log(API.LogType.Warning, $"No command given"); return; }
            switch (args[0])
            {
                // Single commands
                case "playpause":
                    parent.PlayPause();
                    return;
                case "play":
                    parent.Play();
                    return;
                case "pause":
                    parent.Pause();
                    return;
                case "next":
                    parent.Next();
                    return;
                case "previous":
                    parent.Previous(skipThreshold);
                    return;
            }

            if (args.Length < 2) {Out.Log(API.LogType.Warning, $"Invalid amount of arguments for {args[9]}"); return;}
            switch (args[0])
            {
            // Double commands
                case "volume":
                    if (!Int32.TryParse(args[1], out int volume) && volume > 100 && volume < 0)
                    {
                        Out.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}. {args[1]} should be an integer between 0 and 100.");
                        return;
                    }
                    parent.SetVolume(volume);
                    return;
                case "seek":
                    if (!Int32.TryParse(args[1], out int positionMs))
                    {
                        Out.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}. {args[1]} should be an integer.");
                        return;
                    }
                    parent.Seek(positionMs);
                    return;
                case "seekpercent":
                case "setposition":
                    if (!float.TryParse(args[1], out float position))
                    {
                        Out.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}. {args[1]} should be a number from 0 to 100.");
                        return;
                    }
                    // TODO probably not correct
                    parent.Seek((int)(parent.Status.Item.DurationMs * position) / 100);
                    return;
                case "shuffle":
                case "setshuffle":
                    if (!ShuffleTryParse(args[1], out bool shuffle))
                    {
                        Out.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}. {args[1]} should be either -1, 0, 1, True or False");
                        return;
                    }
                    parent.SetShuffle(shuffle);
                    return;
                case "repeat":
                case "setrepeat":
                    if (!RepeatTryParse(args[1], out RepeatState repeat))
                    {
                        Out.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}. {args[1]} should be either Off, Track, Context, -1, 0, 1 or 2");
                        return;
                    }
                    parent.SetRepeat(repeat);
                    return;
                default:
                    Out.Log(API.LogType.Warning, $"Unknown command: {arg}");
                    break;
            }

        }

        private bool RepeatTryParse(string value, out RepeatState repeat)
        {
            switch (value)
            {
                case null:
                    repeat = RepeatState.Off;
                    return false;
                case "-1":
                    PlaybackContext pc = parent.WebApi.GetPlayback();
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
                    shuffle = !(parent.Status?.ShuffleState).GetValueOrDefault();
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
        public static IntPtr Rainmeter;

        static Parent parent;

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            Rainmeter = rm;
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
