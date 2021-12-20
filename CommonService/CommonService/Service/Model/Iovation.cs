using System;
using System.Collections.Generic;

namespace CommonService.Service.Model
{
    public class IovationServicesConfig
    {
        public string ResourceURL { get; set; }
        public string SubscriberID { get; set; }
        public string SubscriberAccount { get; set; }
        public string Password { get; set; }
    }

    public class IovationChecksRequest
    {
        public int accountCode { get; set; }
        public string blackbox { get; set; }
        public string statedIp { get; set; }
        public Dictionary<string, string> transactionInsight { get; set; }
        public string type { get; set; }
    }

    public class IovationResult
    {

        public int accountCode { get; set; }
        public Dictionary<string, dynamic> details { get; set; }
        public string id { get; set; }
        public string reason { get; set; }
        public string result { get; set; }
        public string statedIp { get; set; }
        public long trackingNumber { get; set; }
        public string transactionId { get; set; }
        public string timestamp { get; set; }
        public string path { get; set; }
        public string type { get; set; }
        public int code { get; set; }
        public string message { get; set; }
    }
}
