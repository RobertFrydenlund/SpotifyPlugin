using System;
using SpotifyPlugin;
using System.Threading;

namespace Debugger
{
    class Program
    {
        static void Main(string[] args)
        {
            string token = "";
            int timeout = 1000;


            int numArgs = 2;
            if (args.Length % 2 != 0 && args.Length > numArgs * 2)
            {
                Console.WriteLine("Incorrect number of arguments");
            }

            //Measure data = new Measure();

            while (true)
            {
                // Setup
                StatusControl.timeout = timeout;
                StatusControl.Current_Status.token = token;

                Console.WriteLine("{0} - {1} - uri: {2}", StatusControl.Current_Status.track.track_resource.name, StatusControl.Current_Status.track.artist_resource.name, StatusControl.Current_Status.track.track_resource.uri);
                Thread.Sleep(2000);
            }
        }
    }
}
