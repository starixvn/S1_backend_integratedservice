using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonService.Service.Model
{
    public class MoviderVerifySMSConfig
    {
        public string SendVerifyUrl { get; set; }
        public string AcknowledgeVerifyUrl { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string CodeLength { get; set; }
        public string From { get; set; }
        public string NextEventWait { get; set; }
        public string PinExpire { get; set; }
    }
    public class SendVerifySMSReq
    {
        public int custId { get; set; }
    }
    public class AcknowledgeVerifySMSReq
    {
        public string siteName { get; set; }
        public string request_id { get; set; }
        public string code { get; set; }
    }
    public class MoviderResult
    {
        public string number { get; set; }
        public string price { get; set; }
        public string request_id { get; set; }
        public MoviderError error { get; set; }
    }

    public class MoviderError
    {
        public string code { get; set; }
        public string name { get; set; }
        public string description { get; set; }
    }
}
