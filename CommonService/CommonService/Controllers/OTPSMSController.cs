using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonService.Service.BLL;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PaymentService.Service.Library;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class OTPSMSController
    {
        private readonly OTPSMSBLL sOTPSMSBLL;
        private readonly MoviderSMSBLL moviderSMSBLL;
        private readonly NxCloudSMSBLL nxCloudSMSBLL;
        private readonly Log2File log2File;
        private readonly CustInfoBLL custInfoBLL;
        private readonly SelLangBLL selLangBLL;
        private readonly Dictionary<int, string> perCurrency;
        private readonly IOptions<Dictionary<string, Dictionary<string, string>>> supportOTPConfig;

        public OTPSMSController(OTPSMSBLL sOTPSMSBLL, CustInfoBLL custInfoBLL, MoviderSMSBLL moviderSMSBLL, Log2File log2File, SelLangBLL selLangBLL, NxCloudSMSBLL nxCloudSMSBLL, IOptions<Dictionary<string, Dictionary<string, string>>> supportOTPConfig)
        {
            this.sOTPSMSBLL = sOTPSMSBLL;
            this.moviderSMSBLL = moviderSMSBLL;
            this.nxCloudSMSBLL = nxCloudSMSBLL;

            this.custInfoBLL = custInfoBLL;
            this.log2File = log2File;
            this.selLangBLL = selLangBLL;
            this.supportOTPConfig = supportOTPConfig;
            perCurrency = new Dictionary<int, string>()
            {
              {2,"MYR" },{4,"THB" },{13,"CNY" },{15,"IDR" },{61,"INR" },{51,"VND" },{96,"USDT" }
            };
        }
        private string GetSupportOTP(CustInfoProfile custInfo) //"Movider","SkyLine","NxCloud"
        {
            string SupportOTP = "SkyLine"; // Default
            var OTPConfig = supportOTPConfig.Value[custInfo.SiteName];
            SupportOTP = OTPConfig["Default"];
            if (perCurrency.TryGetValue(custInfo.CurrencyId, out var Currency))
            {
                if(OTPConfig.ContainsKey(Currency))
                    OTPConfig.TryGetValue(Currency, out SupportOTP);
            }
            return SupportOTP;
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

            if (custInfo != null && !string.IsNullOrEmpty(custInfo.PhoneNo))
            {
                var phoneNo = custInfo.PhoneNo.StartsWith("0") ? custInfo.PhoneNo.Substring(1) : custInfo.PhoneNo;
                var areacode = custInfo.AreaCode.StartsWith("+") ? custInfo.AreaCode.Substring(1) : custInfo.AreaCode;
                var phone = $"{areacode}{phoneNo}";
                apiResult = sOTPSMSBLL.GetOTPVerifyInfo(req.CustId);
                if (apiResult.ErrorCode != 0)//OTP not Exist   send SMS
                {
                    var supportOTP = GetSupportOTP(custInfo);
                    apiResult.Message = supportOTP;
                    if (supportOTP.Equals("NxCloud", StringComparison.OrdinalIgnoreCase))
                    {
                        apiResult = await nxCloudSMSBLL.SendOTPSMSAsync(phone, custInfo, req.CustId, custInfo.UserLang);
                    }
                    //else if(supportOTP.Equals("Movider", StringComparison.OrdinalIgnoreCase)) //not support Movider now
                    //{
                    //    var resultResp = await moviderSMSBLL.SendMoviderVerifySMSAsync(custInfo, phone, req.CustId);
                    //    apiResult.ErrorCode = resultResp.ErrorCode;
                    //    apiResult.Data = resultResp.Data;
                    //    apiResult.Message = resultResp.Message;
                    //    apiResult.RawMessage = resultResp.RawMessage;
                    //}
                    else //default SkyLine
                    {
                        apiResult = await sOTPSMSBLL.SendSkyLineOTPSMSAsync(phone, custInfo, req.CustId, custInfo.UserLang);
                    }
                }
            }
            else if (custInfo != null && string.IsNullOrEmpty(custInfo.PhoneNo))
            {
                apiResult.ErrorCode = ErrorCode.InvalidParameter;
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
                
                log2File.Info($"UserName: {custInfo.UserName}\r\nPhoneNo: {custInfo.PhoneNo}\r\nInput: {verifyOTPReq.InputOTP}", $"{custInfo.SiteName}_Verify");

                if (verifyOTPReq.InputOTP.Length >= 10)
                {
                    apiResult.ErrorCode = ErrorCode.Fail;
                    return apiResult;
                }

                var GetOTPVerifyResult = sOTPSMSBLL.GetOTPVerifyInfo(verifyOTPReq.CustId);

                if (GetOTPVerifyResult.ErrorCode == 0)
                {

                    var supportOTP = GetSupportOTP(custInfo);

                   if (supportOTP.Equals("NxCloud", StringComparison.OrdinalIgnoreCase))
                    {
                        apiResult = nxCloudSMSBLL.VerifyOPT(verifyOTPReq);
                    }
                    //else if (supportOTP.Equals("Movider", StringComparison.OrdinalIgnoreCase))
                    //{
                    //    var resultResp = await moviderSMSBLL.AcknowledgeVerifySMSAsync(custInfo.SiteName, GetOTPVerifyResult.Data.Request_id, verifyOTPReq.InputOTP);
                    //    apiResult.ErrorCode = resultResp.ErrorCode;
                    //    apiResult.Message = resultResp.Message;
                    //    apiResult.RawMessage = resultResp.RawMessage;
                    //}
                    else//default SkyLine
                    {
                        apiResult = sOTPSMSBLL.VerifyOPT(verifyOTPReq);
                    }
                }
                else
                {
                    apiResult.ErrorCode = GetOTPVerifyResult.ErrorCode;
                }
                
            }
            catch (Exception ex)
            {
                apiResult.RawMessage = ex.ToString();
                log2File.Error(ex.ToString(), "VerifyOTP", ex);
            }

            return apiResult;
        }
        [HttpPost]
        public ApiResult<SendOTPSMSResult> GetOTPVerifyInfo(Req req)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            try
            {
                var custInfo = custInfoBLL.GetCustInfoProfile(req.CustId);

                if (custInfo != null && !string.IsNullOrEmpty(custInfo.PhoneNo))
                {
                    apiResult = sOTPSMSBLL.GetOTPVerifyInfo(req.CustId);
                    apiResult.ErrorCode = ErrorCode.Success;
                }
                else if (custInfo != null && string.IsNullOrEmpty(custInfo.PhoneNo))
                {
                    apiResult.ErrorCode = ErrorCode.InvalidParameter;
                }
                else
                {
                    apiResult.ErrorCode = ErrorCode.UserNotExist;
                }
            }
            catch (Exception ex)
            {
                apiResult.RawMessage = ex.ToString();
                log2File.Error(ex.ToString(), "GetOTPVerifyInfo", ex);
            }
            return apiResult;
        }
        [HttpPost]
        public async Task<ApiResult> SendSMSAsync(Req req)
        {
            var apiResult = new ApiResult();

            var custInfo = custInfoBLL.GetCustInfoProfile(req.CustId);

            if (custInfo != null && !string.IsNullOrEmpty(custInfo.PhoneNo) && custInfo.PhoneVerified)
            {
                var phoneNo = custInfo.PhoneNo.StartsWith("0") ? custInfo.PhoneNo.Substring(1) : custInfo.PhoneNo;
                var areacode = custInfo.AreaCode.StartsWith("+") ? custInfo.AreaCode.Substring(1) : custInfo.AreaCode;
                var phone = $"{areacode}{phoneNo}";
                apiResult = await sOTPSMSBLL.SendSMSAsync(phone, custInfo, custInfo.UserLang);
            }
            else if (custInfo != null && string.IsNullOrEmpty(custInfo.PhoneNo))
            {
                apiResult.ErrorCode = ErrorCode.InvalidParameter;
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.UserNotExist;
            }
            return apiResult;
        }
        
    }
}
