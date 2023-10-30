namespace CommonService.Service.Model
{
    public class OTPEmailReq
    {
        public int custId { get; set; }
        public string OTPEmailUrl { get; set; }
    }
    public class SendOTPEmailResult
    {
        public int Status { get; set; }
        public long StartUnixTime { get; set; }
        public string Request_id { get; set; }
    }

    public class VerifyOTPEmailReq
    {
        public int CustId { get; set; }
        public string InputOTP { get; set; }
    }
}