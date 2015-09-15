using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace SpotifyPlugin
{
    enum Verbosity { DEBUG, WARNING, ERROR }

    class Out
    {
        #region Settings
        public static Verbosity CurrentVerbosity = Verbosity.DEBUG;
        #endregion

        private static readonly object locker = new object();
        //private static string lastPrint = "";
        private static Stopwatch sw;

        public static void Log(Verbosity verbosity, string value)
        {
            if ((int)verbosity >= (int)CurrentVerbosity)
            {
                Console.WriteLine("{0}: {1}", verbosity.ToString(), value);
            }
        }

        public static void Log(Verbosity verbosity,string format, params object[] arg0)
        {
            if ((int)verbosity >= (int)CurrentVerbosity)
            {
                Console.WriteLine(String.Format("{0}: ", verbosity.ToString()) + format, arg0);
            }
        }

        public static void Write(string logMessage, TextWriter w)
        {
            w.WriteLine("{0} {1} {2}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString(), System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString());
            w.WriteLine("{0}", logMessage);
            w.WriteLine("-------------------------------");
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
            string chrash = String.Format("\n--------");
            chrash += String.Format("\n{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
            chrash += String.Format("\nSpotifyPlugin version {0}", System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString());
            chrash += String.Format("\nCulture: {0}", CultureInfo.InstalledUICulture.ToString());
            chrash += String.Format("\nOSVersion: {0}", Environment.OSVersion.ToString());
            chrash += String.Format("\n----");
            chrash += String.Format("\n {0}", e.Message);
            chrash += String.Format("\n----");
            chrash += String.Format("\n {0}", e.StackTrace);

            Console.WriteLine(chrash);
        }

    }
}