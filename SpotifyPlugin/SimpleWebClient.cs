using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace SpotifyPlugin
{
    class SimpleWebClient
    {
        public SimpleWebClient()
        {

        }

        public string DownloadString(string uri)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
                req.UserAgent = "SpotifyPlugin 1.2.1.0";
                req.ServicePoint.Expect100Continue = false;
                req.Timeout = 1000;
                WebResponse response = req.GetResponse();
                Stream dataStream = response.GetResponseStream();
                byte[] reply = ReadStream(dataStream);
                return System.Text.Encoding.Default.GetString(reply);
            } 
            catch (Exception e)
            {
                Console.WriteLine("HttpWebRequest exception:");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }





            return null;
        }

        private static byte[] ReadStream(Stream input)
        {
            byte[] buffer = new byte[1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
