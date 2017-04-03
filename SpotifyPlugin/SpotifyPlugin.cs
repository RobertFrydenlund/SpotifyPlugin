using Rainmeter;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SpotifyPlugin
{
    public class Measure
    {
        public static string coverPath = "";
        public static string defaultPath = "";

        bool DEBUG = false;
        Status Current_Status;
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

        public Measure()
        {

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
                case "debug":
                    EnableDebugMode(rm.ReadInt("Verbosity", 0));
                    Type = MeasureType.DEBUG;
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

        internal double Update()
        {
            //API.Log(API.LogType.Error, StatusControl.Current_Status.rawData);
            // Update status
            Current_Status = StatusControl.Current_Status;
            switch (Type)
            {
                case MeasureType.DEBUG:
                    return 0;
                case MeasureType.Repeat:
                    return Current_Status.repeat ? 1 : 0;

                case MeasureType.Shuffle:
                    return Current_Status.shuffle ? 1 : 0;

                case MeasureType.PositionSeconds:
                    return Current_Status.playing_position;

                case MeasureType.Playing:
                    return Current_Status.playing ? 1 : 0;

                case MeasureType.LengthSeconds:
                    return Current_Status.track.length;

                case MeasureType.Progress:
                    return Current_Status.playing_position / (Current_Status.track.length * 1.00);

            }
            return 0.0;
        }

        private void EnableDebugMode(int i)
        {
            if (DEBUG == true) return;

            DEBUG = true;
            AllocConsole();
            Console.WriteLine("Console window activated!");
        }

        // CONSOLE
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();


        public string GetString()
        {
            // Update status
            Current_Status = StatusControl.Current_Status;
            switch (Type)
            {

                case MeasureType.TrackURI:
                    return this.Current_Status.track.track_resource.uri;

                case MeasureType.AlbumURI:
                    return this.Current_Status.track.album_resource.uri;

                case MeasureType.ArtistURI:
                    return this.Current_Status.track.artist_resource.uri;

                case MeasureType.Progress:
                    //return ((Current_Status.playing_position / Current_Status.track.length) * 100).ToString();//"##0"
                    return ((Current_Status.playing_position / Current_Status.track.length) * 100).ToString("N"+numDecimals);//"##0"

                case MeasureType.Volume:
                    double volume = 100 * Current_Status.volume;
                    return volume.ToString("##0");

                case MeasureType.Position:
                    double playingPosition = Current_Status.playing_position;
                    double sec = Math.Floor(playingPosition % 60);
                    double min = Math.Floor(playingPosition / 60);
                    return String.Format("{0}:{1}", min.ToString("#00"), sec.ToString("00"));

                case MeasureType.Length:
                    double trackLength = Current_Status.track.length;
                    double secl = Math.Floor(trackLength % 60);
                    double minl = Math.Floor(trackLength / 60);
                    return String.Format("{0}:{1}", minl.ToString("#00"), secl.ToString("00"));

                case MeasureType.TrackName:
                    return toUTF8(Current_Status.track.track_resource.name);

                case MeasureType.ArtistName:
                    return toUTF8(Current_Status.track.artist_resource.name);

                case MeasureType.AlbumArt60:
                    return StatusControl.getArt(60, defaultPath, coverPath);

                case MeasureType.AlbumArt85:
                    return StatusControl.getArt(85, defaultPath, coverPath);

                case MeasureType.AlbumArt120:
                    return StatusControl.getArt(120, defaultPath, coverPath);

                case MeasureType.AlbumArt300:
                    return StatusControl.getArt(300, defaultPath, coverPath);

                case MeasureType.AlbumArt640:
                    return StatusControl.getArt(640, defaultPath, coverPath);

                case MeasureType.AlbumName:
                    return toUTF8(Current_Status.track.album_resource.name);

                case MeasureType.CoverPath:
                    return StatusControl.CoverPath;
            }

            // MeasureType.Major, MeasureType.Minor, and MeasureType.Number are
            // numbers. Therefore, null is returned here for them. This is to
            // inform Rainmeter that it can treat those types as numbers.

            return null;
        }

        private string toUTF8(string s)
        {
            byte[] bytes = Encoding.Default.GetBytes(s);
            return Encoding.UTF8.GetString(bytes);
        }

        internal void ExecuteBang(string p)
        {
            //throw new NotImplementedException(p);
        }
    }

    public static class Plugin
    {
        static IntPtr StringBuffer = IntPtr.Zero;

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));
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

    }
}
