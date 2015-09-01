using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SpotifyPlugin
{
    public class TimeoutWebClient : WebClient
    {
        private int timeout;
        public int Timeout
        {
            get
            {
                return timeout;
            }
            set
            {
                timeout = value;
            }
        }
        public TimeoutWebClient()
        {
            timeout = 600000;
        }
        protected override WebRequest GetWebRequest(Uri address)
        {
            var objWebRequest = base.GetWebRequest(address);
            objWebRequest.Timeout = this.timeout;
            return objWebRequest;
        }
    }
}
