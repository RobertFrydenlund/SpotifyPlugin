using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SpotifyPluginDebugger
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#if DEBUG
            Application.Run(new DebugForm());
#endif
        }
    }
}
