using Rainmeter;
using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SpotifyPlugin
{
    public class Measure
    {
        public static string coverPath = "";
        public static string defaultPath = "";

        int numDecimals = 0;

        enum MeasureType
        {
            DEBUG,
            Running,
            Playing,
            Shuffle,
            Repeat,
            Volume,
            Online,
            Progress,
            Position,
            PositionSeconds,
            Length,
            LengthSeconds,
            TrackName,
            TrackURI,
            TrackURL,
            ArtistName,
            ArtistURI,
            ArtistURL,
            AlbumName,
            AlbumURI,
            AlbumURL,
            AlbumArt60,
            AlbumArt85,
            AlbumArt120,
            AlbumArt300,
            AlbumArt640,
            Tags,
            CoverPath,
            Data
        }
        private MeasureType Type = MeasureType.Running;

        Parent parent;

        public Measure(Parent parent)
        {
            this.parent = parent;
        }

        public void Reload(Rainmeter.API rm, ref double maxValue)
        {
            bool art = false;
            string type = rm.ReadString("Type", "");
            int.TryParse(rm.ReadString("Decimals", "0"), out numDecimals);
            switch (type.ToLowerInvariant())
            {
                case "data":
                    Type = MeasureType.Data;
                    break;
                case "trackuri":
                    Type = MeasureType.TrackURI;
                    break;
                case "albumuri":
                    Type = MeasureType.AlbumURI;
                    break;
                case "artisturi":
                    Type = MeasureType.ArtistURI;
                    break;
                case "tags":
                    Type = MeasureType.Tags;
                    break;
                case "shuffle":
                    Type = MeasureType.Shuffle;
                    break;
                case "repeat":
                    Type = MeasureType.Repeat;
                    break;
                case "position":
                    Type = MeasureType.Position;
                    break;
                case "playing":
                    Type = MeasureType.Playing;
                    break;
                case "progress":
                    Type = MeasureType.Progress;
                    break;
                case "length":
                    Type = MeasureType.Length;
                    break;
                case "volume":
                    Type = MeasureType.Volume;
                    break;
                case "trackname":
                    Type = MeasureType.TrackName;
                    break;
                case "artistname":
                    Type = MeasureType.ArtistName;
                    break;
                case "albumname":
                    Type = MeasureType.AlbumName;
                    break;
                case "albumart":
                    coverPath = rm.ReadPath("CoverPath", "");
                    defaultPath = rm.ReadPath("DefaultPath", "");
                    // 60, 85, 120, 300, and 640.
                    art = true;
                    break;
                case "coverpath":
                    Type = MeasureType.CoverPath;
                    break;
                default:
                    API.Log(API.LogType.Error, "SpotifyPlugin.dll: Type=" + type + " not valid");
                    Type = MeasureType.Length;
                    break;
            }
            if (art)
            {
                string resolution = rm.ReadString("Res", "");

                switch (resolution.ToLowerInvariant())
                {
                    case "60":
                        Type = MeasureType.AlbumArt60;
                        break;
                    case "85":
                        Type = MeasureType.AlbumArt85;
                        break;
                    case "120":
                        Type = MeasureType.AlbumArt120;
                        break;
                    case "640":
                        Type = MeasureType.AlbumArt640;
                        break;
                    default:
                        Type = MeasureType.AlbumArt300;
                        break;
                }
            }
        }
#if DEBUG
        public double Update()
#else
        internal double Update()
#endif
        {
            switch (Type)
            {
                case MeasureType.DEBUG:
                    return 0;
                case MeasureType.Repeat:
                    return (parent.Status?.Repeat).GetValueOrDefault() ? 1 : 0;

                case MeasureType.Shuffle:
                    return (parent.Status?.Shuffle).GetValueOrDefault() ? 1 : 0;

                case MeasureType.Position:
                    return (parent.Status?.PlayingPosition).GetValueOrDefault();

                case MeasureType.Playing:
                    return (parent.Status?.Playing).GetValueOrDefault() ? 1 : 0;

                case MeasureType.Length:
                    return (parent.Status?.Track?.Length).GetValueOrDefault();

                case MeasureType.Progress:
                    double? o = 100 * parent.Status?.PlayingPosition / parent.Status?.Track?.Length;
                    return o.GetValueOrDefault();
            }
            return 0.0;
        }

#if DEBUG
        public string GetString()
#else
        internal string GetString()
#endif
        {
            switch (Type)
            {
                case MeasureType.TrackURI:
                    return parent.Status?.Track?.TrackResource?.Uri;

                case MeasureType.AlbumURI:
                    return parent.Status?.Track?.AlbumResource?.Uri;

                case MeasureType.ArtistURI:
                    return parent.Status?.Track?.ArtistResource?.Uri;
                    
                case MeasureType.Volume:
                    return (parent.Status?.Volume * 100).ToString();

                case MeasureType.Position:
                    double playingPosition = (parent.Status?.PlayingPosition).GetValueOrDefault();
                    double sec = Math.Floor(playingPosition % 60);
                    double min = Math.Floor(playingPosition / 60);
                    return String.Format("{0}:{1}", min.ToString("#00"), sec.ToString("00"));

                case MeasureType.Length:
                    double trackLength = (parent.Status?.Track?.Length).GetValueOrDefault();
                    double secl = Math.Floor(trackLength % 60);
                    double minl = Math.Floor(trackLength / 60);
                    return String.Format("{0}:{1}", minl.ToString("#00"), secl.ToString("00"));

                case MeasureType.TrackName:
                    return parent.Status?.Track?.TrackResource?.Name;

                case MeasureType.ArtistName:
                    return parent.Status?.Track?.ArtistResource?.Name;

                // TODO
                case MeasureType.AlbumArt60:
                    return AlbumArt.getArt(parent.Status?.Track?.AlbumResource?.Uri, 60, defaultPath, coverPath);

                case MeasureType.AlbumArt85:
                    return AlbumArt.getArt(parent.Status?.Track?.AlbumResource?.Uri, 85, defaultPath, coverPath);

                case MeasureType.AlbumArt120:
                    return AlbumArt.getArt(parent.Status?.Track?.AlbumResource?.Uri, 120, defaultPath, coverPath);

                case MeasureType.AlbumArt300:
                    return AlbumArt.getArt(parent.Status?.Track?.AlbumResource?.Uri, 300, defaultPath, coverPath);

                case MeasureType.AlbumArt640:
                    return AlbumArt.getArt(parent.Status?.Track?.AlbumResource?.Uri, 640, defaultPath, coverPath);

                case MeasureType.AlbumName:
                    return parent.Status?.Track?.AlbumResource?.Name;

                case MeasureType.CoverPath:
                    return null;
                    //return StatusControl.CoverPath;
            }
            // MeasureType.Major, MeasureType.Minor, and MeasureType.Number are
            // numbers. Therefore, null is returned here for them. This is to
            // inform Rainmeter that it can treat those types as numbers.

            return null;
        }

        internal void ExecuteBang(string arg)
        {
            string[] args = Regex.Split(arg, " ");
            switch (args[0].ToLowerInvariant())
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
                        API.Log(API.LogType.Warning, $"Invalid arguments for command: {args[0]}. {args[1]} should be an integer between 0-100.");
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
