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

    public class YcSmsServiceTypeConfig
    {
        public YcSmsConfig OTP { get; set; }
    }

    public class YcSmsConfig
    {
        public string ApiPath { get; set; }
        public string Merchant { get; set; }
        public string Token { get; set; }
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
        public string reason { get; set; }
        public string[][] array { get; set; }
    }


    public class YcSmsSendOtpResult
    {
        public string status { get; set; }

        public string error_code { get; set; }

        public string res { get; set; }
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

    public class OTPLog
    {
        public int CurrencyId { get; set; }
        public int OTPType { get; set; }
        public int CustId { get; set; }
        public string UserName { get; set; }
        public string OTPProvider { get; set; }
        public string PhoneNo { get; set; }
        public string Content { get; set; }
        public string OTPNum { get; set; } = string.Empty;
        public string Request_id { get; set; } = string.Empty;
        public int OTPStatus { get; set; }
        public string OTPResponse { get; set; } = string.Empty;
        public int Verified { get; set; }
        public DateTime? VerifiedTime { get; set; }
    }
}
