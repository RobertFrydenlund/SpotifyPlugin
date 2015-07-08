using Rainmeter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SpotifyPlugin
{
    public class Measure
    {
        Status Current_Status;
        int numDecimals = 0;

        enum MeasureType
        {
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
            Tags
        }

        MeasureType Type = MeasureType.Running;

        public Measure()
        {

        }
#if DEBUG
        public void Reload(string resolution, string type, ref double maxValue)
#else
        public void Reload(Rainmeter.API rm, ref double maxValue)
#endif
        {

            bool art = false;
#if DEBUG
#else
            string type = rm.ReadString("Type", "");
            int.TryParse(rm.ReadString("Decimals", "0"), out numDecimals);
#endif
            switch (type.ToLowerInvariant())
            {
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
                    // 60, 85, 120, 300, and 640.
                    art = true;
                    break;
                default:
                    API.Log(API.LogType.Error, "SpotifyPlugin.dll: Type=" + type + " not valid");
                    Type = MeasureType.Length;
                    break;
            }
            if (art)
            {
#if DEBUG
#else
                string resolution = rm.ReadString("Res", "");
#endif
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

        public double Update()
        {
            //API.Log(API.LogType.Error, StatusControl.Current_Status.rawData);
            // Update status
            Current_Status = StatusControl.Current_Status;
            switch (Type)
            {
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

            // MeasureType.MajorMinor is not a number and and therefore will be
            // returned in GetString.

            return 0.0;
        }

        public string GetString()
        {
            // Update status
            Current_Status = StatusControl.Current_Status;
            switch (Type)
            {
                case MeasureType.Tags:
                    
                    return StatusControl.GetTags();

                case MeasureType.Progress:
                    //return ((Current_Status.playing_position / Current_Status.track.length) * 100).ToString();//"##0"
                    return ((Current_Status.playing_position / Current_Status.track.length) * 100).ToString("N"+numDecimals);//"##0"


                case MeasureType.Volume:
                    double volume = 100 * Current_Status.volume;
                    return volume.ToString("##0");

                case MeasureType.Position:
                    double sec = Current_Status.playing_position;
                    double min = (int)Math.Floor(1.0 * sec / 60);
                    sec -= min * 60;
                    return min.ToString("#00") + ":" + sec.ToString("00");

                case MeasureType.Length:
                    int sec2 = Current_Status.track.length;
                    int min2 = (int)Math.Floor(1.0 * sec2 / 60);
                    sec2 -= min2 * 60;
                    return min2.ToString("#00") + ":" + sec2.ToString("00");

                case MeasureType.TrackName:
                    return toUTF8(Current_Status.track.track_resource.name);

                case MeasureType.ArtistName:
                    return toUTF8(Current_Status.track.artist_resource.name);

                case MeasureType.AlbumArt60:
                    return StatusControl.getArt(60);

                case MeasureType.AlbumArt85:
                    return StatusControl.getArt(85);

                case MeasureType.AlbumArt120:
                    return StatusControl.getArt(120);

                case MeasureType.AlbumArt300:
                    return StatusControl.getArt(300);

                case MeasureType.AlbumArt640:
                    return StatusControl.getArt(640);

                case MeasureType.AlbumName:
                    return toUTF8(Current_Status.track.album_resource.name);

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
            throw new NotImplementedException(p);
        }
    }

    public static class Plugin
    {
        internal static Dictionary<uint, Measure> Measures = new Dictionary<uint, Measure>();


        [DllExport]
        public unsafe static void ExecuteBang(void* data, char* args)
        {
            uint id = (uint)data;
            Measures[id].ExecuteBang(new string(args));
        }



        [DllExport]
        public unsafe static void Initialize(void** data, void* rm)
        {
            uint id = (uint)((void*)*data);
            Measures.Add(id, new Measure());
        }

        [DllExport]
        public unsafe static void Finalize(void* data)
        {
            uint id = (uint)data;
            Measures.Remove(id);
        }

        [DllExport]
        public unsafe static void Reload(void* data, void* rm, double* maxValue)
        {
            uint id = (uint)data;
#if DEBUG
#else
            Measures[id].Reload(new Rainmeter.API((IntPtr)rm), ref *maxValue);
#endif
        }

        [DllExport]
        public unsafe static double Update(void* data)
        {

            uint id = (uint)data;
            return Measures[id].Update();

        }

        [DllExport]
        public unsafe static char* GetString(void* data)
        {
            uint id = (uint)data;
            fixed (char* s = Measures[id].GetString()) return s;
        }
    }
}
