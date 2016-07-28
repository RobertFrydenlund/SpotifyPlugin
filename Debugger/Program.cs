using System;
using SpotifyPlugin;
using System.Threading;

namespace Debugger
{
    class Program
    {
        static void Main(string[] args)
        {
            string token = "No token";
            int timeout = 1000;
            
            int numArgs = 2;
            if (args.Length % 2 != 0 && args.Length > numArgs * 2) 
            { 
                Console.WriteLine("Incorrect number of arguments"); 
            }

            for (int i = 0; i < args.Length; i += 2)
            {
                switch(args[i]){
                    case "-token":
                        token = args[i + 1];
                        Console.WriteLine("Starting with token: {0}", token);
                        break;
                    case "-timeout":
                        if (!int.TryParse(args[i + 1], out timeout)) Console.WriteLine("Timeout must be an integer (A whole number). Using default 1000 instead.");
                        break;
                    default:
                        Console.WriteLine("Unrecognized option.");
                        return;
                }
            }
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
