
using System.Text;
namespace SpotifyPlugin
{
    public class Location
    {
        public string og { get; set; }
    }
    public class Track
    {
        public Track()
        {
            track_resource = new Resource();
            artist_resource = new Resource();
            album_resource = new Resource();
            length = 0;
            track_type = "";
        }

        public Resource track_resource { get; set; }
        public Resource artist_resource { get; set; }
        public Resource album_resource { get; set; }
        public int length { get; set; }
        public string track_type { get; set; }
    }

    public class Resource
    {
        public Resource()
        {
            name = "";
            uri = "";
        }
        public string name { get; set;}
        public string uri { get; set; }
        public Location location { get; set; }
    }
    public class Status
    {

        public string rawData { get; set; }
        public Status()
        {
            token = "t";
            error = "";
            rawData = "";
            version = 0;
            client_version = "";
            playing = false;
            shuffle = false;
            repeat = false;
            play_enabled = false;
            prev_enabled = false;
            track = new Track();
            playing_position = 0;

            volume = 0;
            online = false;

            running = false;
        }

        public string token { get; set;}
        public string error { get; set; }
        public Track track { get; set; }

        public int version { get; set; }
        public string client_version { get; set; }
        public bool playing { get; set; }
        public bool shuffle { get; set; }
        public bool repeat { get; set; }
        public bool play_enabled { get; set; }
        public bool prev_enabled { get; set; }
        public bool next_enabled { get; set; }

        public double playing_position { get; set; }
        public int server_time { get; set; }
        public double volume { get; set; }
        public bool online { get; set; }
        public bool running { get; set; }
    }

}