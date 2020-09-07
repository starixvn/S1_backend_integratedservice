using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonService.Service.Model
{
    public class TwilioVerifySMSServices
    {
        public string AccountSID { get; set; }
        public string Authtoken { get; set; }
        public string ServiceID { get; set; }
        public string VerificationService { get; set; }
        public string Verifications { get; set; }
        public string VerificationChecks { get; set; }
    }
    public class TwilioVerificationsSMSReq
    {
        public int custId { get; set; }
    }
    public class TwilioVerificationCheckSMSReq
    {
        public string siteName { get; set; }
        public string sid { get; set; }
        public string code { get; set; }
    }
    public class TwilioVerificationsResult
    {
        public dynamic status { get; set; }
        public string valid { get; set; }
        public string sid { get; set; }
        public dynamic code { get; set; }
        public string message { get; set; }
        
            
    }
}
