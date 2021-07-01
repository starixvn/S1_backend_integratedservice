using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonService.Service.Model
{
    public class FireBase
    {
    }
    public class FireBaseConfig
    {
        public string ApplicationID { get; set; }
        public string SenderId { get; set; }
    }
    public class FireBaseMessage
    {
        public int FireBaseId { get; set; }
        public int CustId { get; set; }
        public string SiteName { get; set; }
        public string Token { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public int Status { get; set; }
    }
    public class FireBaseResponse
    {
        public string multicast_id { get; set; }
        public int success { get; set; }
        public int failure { get; set; }
        public int canonical_ids { get; set; }
        //public string results { get; set; }
    }
    public class FireBaseResults
    {
        public string message_id { get; set; }
        public string error { get; set; }
    }
    public class FireBaseReturn
    {
        public int FireBaseId { get; set; }
        
        public int Status { get; set; }
        public int Success { get; set; }
        public int Fail { get; set; }
    }
}
