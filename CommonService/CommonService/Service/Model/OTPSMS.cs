using System;
using System.Collections.Generic;

namespace CommonService.Service.Model
{
    public class OTPSMSServiceConfig
    {
        public string ResourceURL { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }
    }
    public class SendOTPSMSResult
    {
        public int Status { get; set; }
        public long StartUnixTime { get; set; }
    }
    public class VerifyOTPReq
    {
        public int CustId { get; set; }
        public string InputOTP { get; set; }
    }
   
    public class SKYLineSMSResult
    {
        public int status { get; set; }
        public string success { get; set; }
        public string fail { get; set; }
        //public string array { get; set; }
        public string reason { get; set; }
    }

}
