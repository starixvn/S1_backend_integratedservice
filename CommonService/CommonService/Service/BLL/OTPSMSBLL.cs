using CommonService.Service.DAL;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

namespace CommonService.Service.BLL
{
    public class OTPSMSBLL
    {
        private readonly OTPSMSService _otpSmsService;

        public OTPSMSBLL(IServiceProvider provider)
        {
            _otpSmsService = provider.GetRequiredService<OTPSMSService>();
        }

        public ApiResult<SendOTPSMSResult> GetOTPVerifyInfo(int custId)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            var result = _otpSmsService.GetOTPVerifyinfo(custId);
            apiResult.ErrorCode = (ErrorCode)(result.Status == 1 ? 0 : 1000);
            apiResult.Data = result;

            return apiResult;
        }
        
        public ApiResult<SendOTPSMSResult> GetSignUpOTPVerifyInfo(string areaPhoneNo)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            var result = _otpSmsService.GetSignUpOTPVerifyinfo(areaPhoneNo ,(int)OtpType.SignUp);
            apiResult.ErrorCode = (ErrorCode)(result.Status == 1 ? 0 : 1000);
            apiResult.Data = result;

            return apiResult;
        }

        /// <summary>
        /// 新增OTP紀錄到DB
        /// </summary>
        /// <param name="custId"></param>
        /// <param name="sOTPNum"></param>
        /// <returns></returns>
        public ApiResult<SendOTPSMSResult> OTPVerifyinfoInsAsync(int custId, string sOTPNum)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            var result = _otpSmsService.OTPVerifyinfoIns(custId, sOTPNum);
            apiResult.ErrorCode = (ErrorCode)result.Status;
            apiResult.Data = result;

            return apiResult;
        }

        public ApiResult<SendOTPSMSResult> SignUpOTPVerifyinfoInsAsync(string areaPhoneNo, string sOTPNum)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            var result = _otpSmsService.SignUpOTPVerifyinfoIns(areaPhoneNo, sOTPNum , (int)OtpType.SignUp);
            apiResult.ErrorCode = (ErrorCode)result.Status;
            apiResult.Data = result;

            return apiResult;
        }
        
        public ApiResult VerifyOPT(VerifyOTPReq verifyOtpReq)
        {
            var apiResult = new ApiResult();
            var result = _otpSmsService.VerifyOTP(verifyOtpReq);
            apiResult.ErrorCode = (ErrorCode)result;
            return apiResult;
        }

        public int InsertOTPLog(OTPLog oTPLog)
        {
            return _otpSmsService.InsertOTPLog(oTPLog);
        }
    }
}
