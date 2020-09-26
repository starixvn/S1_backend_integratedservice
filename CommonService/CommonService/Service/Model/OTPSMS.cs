using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonService.Service.Model
{
    public class OTPSMS
    {
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
    }
}
