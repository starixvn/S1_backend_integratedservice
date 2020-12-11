using CommonService.Service.BLL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Service.Library;
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
        private readonly MoviderSMSBLL moviderSMSBLL;
        private readonly Log2File log2File;
        private readonly CustInfoBLL custInfoBLL;
        public OTPSMSController(OTPSMSBLL sOTPSMSBLL, CustInfoBLL custInfoBLL, MoviderSMSBLL moviderSMSBLL, Log2File log2File)
        {
            this.sOTPSMSBLL = sOTPSMSBLL;
            this.custInfoBLL = custInfoBLL;
            this.moviderSMSBLL = moviderSMSBLL;
            this.log2File = log2File;
        }

        public class Req
        {
            public int CustId { get; set; }
        }
        [HttpPost]
        public async Task<ApiResult<SendOTPSMSResult>> SendOTPSMSAsync(Req req)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();

            var custInfo = custInfoBLL.GetCustInfoProfile(req.CustId);

            if (custInfo != null && !string.IsNullOrEmpty(custInfo.PhoneNo) && !custInfo.PhoneVerified)
            {
                var phoneNo = custInfo.PhoneNo.StartsWith("0") ? custInfo.PhoneNo.Substring(1) : custInfo.PhoneNo;
                var areacode = custInfo.AreaCode.StartsWith("+") ? custInfo.AreaCode.Substring(1) : custInfo.AreaCode;
                var phone = $"{areacode}{phoneNo}";
                apiResult = sOTPSMSBLL.GetOTPVerifyInfo(req.CustId);
                if (apiResult.ErrorCode != 0)//OTP not Exist   send SMS
                {
                    apiResult = await sOTPSMSBLL.SendSkyLineOTPSMSAsync(phone, custInfo, req.CustId, custInfo.UserLang);
                    //if (custInfo.SiteName.Equals("Bwing", StringComparison.OrdinalIgnoreCase) || custInfo.SiteName.Equals("Goal123", StringComparison.OrdinalIgnoreCase))
                    //{
                    //    apiResult = await sOTPSMSBLL.SendSkyLineOTPSMSAsync(phone, custInfo, req.CustId);
                    //}
                    //else
                    //{
                    //    var resultResp = await moviderSMSBLL.SendMoviderVerifySMSAsync(custInfo, phone, req.CustId);
                    //    apiResult.ErrorCode = resultResp.ErrorCode;
                    //    apiResult.Data = resultResp.Data;
                    //    apiResult.Message = resultResp.Message;
                    //    apiResult.RawMessage = resultResp.RawMessage;
                    //}
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
        public ApiResult VerifyOTP(VerifyOTPReq verifyOTPReq)
        {
            ApiResult apiResult = new ApiResult();
            try
            {
                var custInfo = custInfoBLL.GetCustInfoProfile(verifyOTPReq.CustId);
                if (custInfo.PhoneVerified)
                {
                    apiResult.ErrorCode = 0;
                }
                else
                {
                    log2File.Info($"UserName: {custInfo.UserName}\r\nPhoneNo: {custInfo.PhoneNo}\r\nInput: {verifyOTPReq.InputOTP}", $"{custInfo.SiteName}_Verify");

                    if (verifyOTPReq.InputOTP.Length >= 10)
                    {
                        apiResult.ErrorCode = ErrorCode.Fail;
                        return apiResult;
                    }

                    var GetOTPVerifyResult = sOTPSMSBLL.GetOTPVerifyInfo(verifyOTPReq.CustId);

                    if (GetOTPVerifyResult.ErrorCode == 0)
                    {
                        apiResult = sOTPSMSBLL.VerifyOPT(verifyOTPReq);
                        //if (custInfo.SiteName.Equals("Bwing", StringComparison.OrdinalIgnoreCase) || custInfo.SiteName.Equals("Goal123", StringComparison.OrdinalIgnoreCase))
                        //{
                        //    apiResult = sOTPSMSBLL.VerifyOPTAsync(verifyOTPReq);
                        //}
                        //else
                        //{
                        //    var resultResp = await moviderSMSBLL.AcknowledgeVerifySMSAsync(custInfo.SiteName, GetOTPVerifyResult.Data.Request_id, verifyOTPReq.InputOTP);
                        //    apiResult.ErrorCode = resultResp.ErrorCode;
                        //    apiResult.Message = resultResp.Message;
                        //    apiResult.RawMessage = resultResp.RawMessage;
                        //}
                    }
                    else
                    {
                        apiResult.ErrorCode = GetOTPVerifyResult.ErrorCode;
                    }
                }
            }
            catch (Exception ex)
            {
                apiResult.RawMessage = ex.ToString();
                log2File.Error(ex.ToString(), "VerifyOTP");
            }

            return apiResult;
        }
    }

}
