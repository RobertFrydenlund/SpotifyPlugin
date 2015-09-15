using System;
using SpotifyPlugin;
using System.Threading;

namespace Debugger
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("{0} - {1}", StatusControl.Current_Status.track.track_resource.name, StatusControl.Current_Status.track.artist_resource.name);
                Thread.Sleep(5000);
            }
        }
    }
}
