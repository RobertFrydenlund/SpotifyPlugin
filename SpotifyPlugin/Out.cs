using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Rainmeter;

namespace SpotifyPlugin
{
    class Out
    {
        //private static string lastPrint = "";
        private static Stopwatch sw;

        public static void Log(API.LogType verbosity, string value)
        {
            API.Log(verbosity, value);
        }

        public static void Log(API.LogType verbosity, string format, params object[] arg0)
        {
            API.Log(verbosity, String.Format(format, arg0));
        }

        public static void Start()
        {
            sw = new Stopwatch();
            sw.Start();
        }

        public long Stop()
        {
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        public static void ChrashDump(Exception e)
        {
            string chrash = String.Format("\n{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
            chrash += String.Format("\nSpotifyPlugin version {0}", System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString());
            chrash += String.Format("\nCulture: {0}", CultureInfo.InstalledUICulture.ToString());
            chrash += String.Format("\nOSVersion: {0}", Environment.OSVersion.ToString());
            chrash += String.Format("\n----");
            chrash += String.Format("\n {0}", e.Message);
            chrash += String.Format("\n----");
            chrash += String.Format("\n {0}", e.StackTrace);
            Log(API.LogType.Error, chrash);
        }

    }
}