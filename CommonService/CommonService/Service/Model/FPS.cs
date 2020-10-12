using System;
using System.Collections.Generic;

namespace CommonService.Service.Model
{
    public class FPSServicesConfig
    {
        public string ResourceURL { get; set; }
        public string Subscriber { get; set; }

    }

    public class FPSChecksRequest
    {
        public int account { get; set; }
        public string action { get; set; }
        public string actionResult { get; set; }
        public string ip { get; set; }
        public string url { get; set; }
        public string platform { get; set; }
        public string userAgent { get; set; }
        public string screen { get; set; }
        public string di { get; set; }
    }

    public class FPSResult
    {
        public string message { get; set; }
    }
}
