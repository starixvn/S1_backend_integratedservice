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
        public int CustId { get; set; }
        public string SiteName { get; set; }
        public string Token { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public int Status { get; set; }
    }
}
