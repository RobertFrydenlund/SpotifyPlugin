using Rainmeter;
using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

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
            // TODO Cheater
            Thread t = new Thread(() =>
            {
                string[] args = Regex.Split(arg, " ");
                switch (args[0].ToLowerInvariant())
                {
                    // Single commands
                    case "playpause":
                        if ((parent.Status?.Playing).GetValueOrDefault())
                        {
                            goto case "pause";
                        }
                        else
                        {
                            goto case "play";
                        }
                    case "play":
                        parent.WebAPI?.ResumePlayback();
                        return;
                    case "pause":
                        parent.WebAPI?.PausePlayback();
                        return;
                    case "next":
                        parent.WebAPI?.SkipPlaybackToNext();
                        return;
                    case "previous":
                        // TODO always skips to previous, should probably seek to 0 if progress > threshold
                        parent.WebAPI?.SkipPlaybackToPrevious();
                        return;

                    // Double commands
                    case "volume":
                        int volume;
                        if (!Int32.TryParse(args[1], out volume))
                        {
                            API.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}. {args[1]} should be an integer between 0-100.");
                            return;
                        }
                        parent.WebAPI?.SetVolume(volume);
                        return;
                    case "seek":
                        int seek;
                        if (!Int32.TryParse(args[1], out seek))
                        {
                            API.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}");
                            return;
                        }
                        parent.WebAPI?.SeekPlayback(seek);
                        return;
                    case "seekpercent":
                    case "setposition":
                        float position;
                        if (!float.TryParse(args[1], out position))
                        {
                            API.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}. {args[1]} should be a number from 0 to 100.");
                            return;
                        }
                        // TODO error 405
                        parent.WebAPI?.SeekPlayback((int)(parent.Status?.Track?.Length * position) / 100);
                        return;
                    case "shuffle":
                        bool shuffle;
                        if (!bool.TryParse(args[1], out shuffle))
                        {
                            API.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}. {args[1]} should be either True or False");
                            return;
                        }
                        parent.WebAPI?.SetShuffle(shuffle);
                        return;
                    case "repeat":
                        SpotifyAPI.Web.Enums.RepeatState repeat;
                        if (!Enum.TryParse(args[1], out repeat))
                        {
                            API.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}. {args[1]} should be either Track, Context or Off");
                            return;
                        }
                        parent.WebAPI?.SetRepeatMode(repeat);
                        return;
                }
                API.Log(API.LogType.Warning, $"Unknown command: {arg}");
            });

            t.Start();

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
