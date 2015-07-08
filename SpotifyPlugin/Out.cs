using System;
using System.Diagnostics;
using System.IO;

namespace SpotifyPlugin
{
    enum Verbosity { DEBUG, WARNING, ERROR }

    class Out
    {
        #region Settings
        private static Verbosity CurrentVerbosity = Verbosity.DEBUG;
        #endregion

        private static readonly object locker = new object();
        private static string lastPrint = "";
        private static Stopwatch sw;

        [Conditional("DEBUG")]
        public static void Log(Object data, Verbosity verbosity)
        {
            if ((int)verbosity >= (int)CurrentVerbosity)
            {
                // Write data to file
                if (data.ToString() != lastPrint)
                {
                    lastPrint = data.ToString();
                    lock (locker)
                    {
                        using (StreamWriter w = File.AppendText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\log.txt"))
                        {
                            Write(data.ToString(), w);
                        }
                    }
                }
            }
        }

        public static void Write(string logMessage, TextWriter w)
        {
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
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

    }
}