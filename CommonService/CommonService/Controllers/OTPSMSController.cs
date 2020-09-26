using CommonService.Service.BLL;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CommonService.Service.Model.OTPSMS;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class OTPSMSController
    {
        private readonly OTPSMSBLL sOTPSMSBLL;
        private readonly CustInfoBLL custInfoBLL;

        public OTPSMSController(OTPSMSBLL sOTPSMSBLL , CustInfoBLL custInfoBLL)
        {
            this.sOTPSMSBLL = sOTPSMSBLL;
            this.custInfoBLL = custInfoBLL;
        }
        
        public class Req33
        {
            public int custId { get; set; }
        }
        [HttpPost]
        public async Task<ApiResult<SendOTPSMSResult>> SendOTPSMSAsync(Req33 req)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();

            var custInfo = custInfoBLL.GetCustInfoProfile(req.custId);

            if (custInfo != null && !string.IsNullOrEmpty(custInfo.PhoneNo) && !custInfo.PhoneVerified)
            {
                var phoneNo = custInfo.PhoneNo.StartsWith("0") ? custInfo.PhoneNo.Substring(1) : custInfo.PhoneNo;
                apiResult = await sOTPSMSBLL.GetOTPVerifyInfoAsync(req.custId);
                if (apiResult.ErrorCode == 0)
                {
                    //send SMS 
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
