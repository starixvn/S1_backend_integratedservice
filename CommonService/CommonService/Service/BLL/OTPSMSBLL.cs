using CommonService.Service.DAL;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

namespace CommonService.Service.BLL
{
    public class OTPSMSBLL
    {
        private readonly OTPSMSService _sOtpSmsService;

        public OTPSMSBLL(IServiceProvider provider)
        {
            _sOtpSmsService = provider.GetRequiredService<OTPSMSService>();
        }

        public ApiResult<SendOTPSMSResult> GetOTPVerifyInfo(int custId)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            var result = _sOtpSmsService.GetOTPVerifyinfo(custId);
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
            var result = _sOtpSmsService.OTPVerifyinfoIns(custId, sOTPNum);
            apiResult.ErrorCode = (ErrorCode)result.Status;
            apiResult.Data = result;

            return apiResult;
        }

        public ApiResult VerifyOPT(VerifyOTPReq verifyOtpReq)
        {
            var apiResult = new ApiResult();
            var result = _sOtpSmsService.VerifyOTP(verifyOtpReq);
            apiResult.ErrorCode = (ErrorCode)result;
            return apiResult;
        }

        public int InsertOTPLog(OTPLog oTPLog)
        {
            return _sOtpSmsService.InsertOTPLog(oTPLog);
        }
    }
}
