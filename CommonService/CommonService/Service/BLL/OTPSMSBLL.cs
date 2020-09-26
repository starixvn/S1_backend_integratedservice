using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CommonService.Service.Model.OTPSMS;

namespace CommonService.Service.BLL
{
    public class OTPSMSBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly OTPSMSService sOTPSMSService;

        public OTPSMSBLL( HttpHelper httpHelper, OTPSMSService sOTPSMSService)
        {
            this.httpHelper = httpHelper;
            this.sOTPSMSService = sOTPSMSService;
        }
        protected string GetRandomString(int length)
        {
            // var str = "0123456789abcdefghijklmnopqrstuvwxyz";//ABCDEFGHIJKLMNOPQRSTUVWXYZ
            var str = "0123456789";
            var next = new Random();
            var builder = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                builder.Append(str[next.Next(0, str.Length)]);
            }
            return builder.ToString();
        }

        public async Task<ApiResult<SendOTPSMSResult>> GetOTPVerifyInfoAsync(int custId)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            string sOTPNum = GetRandomString(6);
            SendOTPSMSResult result = sOTPSMSService.GetOTPVerifyinfo(custId,sOTPNum);
            apiResult.ErrorCode = (ErrorCode)result.Status;
            apiResult.Data = result;


            return apiResult;
        }

        public async Task<ApiResult> VerifyOPTAsync(VerifyOTPReq verifyOTPReq)
        {
            var apiResult = new ApiResult();
            int? result = sOTPSMSService.VerifyOPT(verifyOTPReq);
            apiResult.ErrorCode = (ErrorCode)(result==0?0:1000);
            return apiResult;
        }
    }
}
