using System;
using System.Collections.Generic;

namespace CommonService.Service.Model
{
    public class SkyLineSMSServiceTypeConfig
    {
        public SkyLineSMSServiceConfig OTP { get; set; }
        public SkyLineSMSServiceConfig SMS { get; set; }
    }

    public class SkyLineSMSServiceConfig
    {
        public string ResourceURL { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }
    }
    public class SendOTPSMSResult
    {
        public int Status { get; set; }
        public long StartUnixTime { get; set; }
        public string Request_id { get; set; }
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

    public class NxcloudSMSServiceTypeConfig
    {
        public NxcloudSMSServiceConfig OTP { get; set; }
    }
    public class NxcloudSMSServiceConfig
    {
        public string ResourceURL { get; set; }
        public string Appkey { get; set; }
        public string Secretkey { get; set; }
    }
    public class NxcloudSMSResponse
    {
        public string result { get; set; }
        public string messageid { get; set; }
        public string code { get; set; }
    }
}
