using CommonService.Service.BLL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class OTPSMSController
    {
        private readonly OTPSMSBLL sOTPSMSBLL;
        private readonly CustInfoBLL custInfoBLL;
        private readonly Tools tools;

        public OTPSMSController(OTPSMSBLL sOTPSMSBLL , CustInfoBLL custInfoBLL, Tools tools)
        {
            this.sOTPSMSBLL = sOTPSMSBLL;
            this.custInfoBLL = custInfoBLL;
            this.tools = tools;
        }
        
        public class Req
        {
            public int custId { get; set; }
        }
        [HttpPost]
        public async Task<ApiResult<SendOTPSMSResult>> SendOTPSMSAsync(Req req)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();

            var custInfo = custInfoBLL.GetCustInfoProfile(req.custId);

            if (custInfo != null && !string.IsNullOrEmpty(custInfo.PhoneNo) && !custInfo.PhoneVerified)
            {
                var phoneNo = custInfo.PhoneNo.StartsWith("0") ? custInfo.PhoneNo.Substring(1) : custInfo.PhoneNo;
                var phone = $"{custInfo.AreaCode}{phoneNo}";
                apiResult = await sOTPSMSBLL.GetOTPVerifyInfoAsync(req.custId);
                if (apiResult.ErrorCode != 0)//OTP not Exist   send SMS
                {
                    string sOTPNum = tools.GetRandomNumString(6);
                    //send SMS 
                    var apiResultSendOTP = await sOTPSMSBLL.SendOTPSMSAsync(phone, sOTPNum, custInfo.UserLang);
                    //if send SMS Success
                    if (apiResultSendOTP.ErrorCode == ErrorCode.Success)
                    {
                        apiResult = await sOTPSMSBLL.OTPVerifyinfoInsAsync(req.custId, sOTPNum);
                    }
                    else
                    {
                        apiResult.ErrorCode = apiResultSendOTP.ErrorCode;
                        apiResult.Message = apiResultSendOTP.Message;
                    }
                }
            }
            else if (custInfo != null && string.IsNullOrEmpty(custInfo.PhoneNo))
            {
                apiResult.ErrorCode = ErrorCode.InvalidParameter;
            }
            else if (custInfo.PhoneVerified)
            {
                apiResult.ErrorCode = ErrorCode.Verified;
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.UserNotExist;
            }


            return apiResult;
        }
        [HttpPost]
        public async Task<ApiResult> VerifyOTPAsync(VerifyOTPReq verifyOTPReq)
        {
            ApiResult apiResult = new ApiResult();
            return await sOTPSMSBLL.VerifyOPTAsync(verifyOTPReq);

        }
    }
    
}
