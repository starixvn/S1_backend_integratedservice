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
        private readonly NxCloudSMSBLL nxCloudSMSBLL;
        private readonly Log2File log2File;
        private readonly CustInfoBLL custInfoBLL;
        private readonly SelLangBLL selLangBLL;
        private readonly Dictionary<string, SMSSupportConfig> _supportOTPConfig;

        public OTPSMSController(IServiceProvider provider)
        {
            sOTPSMSBLL = provider.GetRequiredService<OTPSMSBLL>();
            nxCloudSMSBLL = provider.GetRequiredService<NxCloudSMSBLL>();
            custInfoBLL = provider.GetRequiredService<CustInfoBLL>();
            log2File = provider.GetRequiredService<Log2File>();
            selLangBLL = provider.GetRequiredService<SelLangBLL>();
            var configBLL = provider.GetRequiredService<ConfigBLL>();
            _supportOTPConfig = configBLL.GetConfigValuesBySiteName<SMSSupportConfig>("SMSSupport");
        }

        private string GetSupportOTP(CustInfoProfile custInfo)
        {
            var otpConfig = _supportOTPConfig[custInfo.SiteName];
            var supportOTP = otpConfig.SMSSupport["Default"];
            if (otpConfig.SMSSupport.ContainsKey(custInfo.AreaCode))
                otpConfig.SMSSupport.TryGetValue(custInfo.AreaCode, out supportOTP);
            return supportOTP;
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
                var areaCode = custInfo.AreaCode.StartsWith("+") ? custInfo.AreaCode.Substring(1) : custInfo.AreaCode;
                var phone = $"{areaCode}{phoneNo}";
                apiResult = sOTPSMSBLL.GetOTPVerifyInfo(req.CustId);

                if (apiResult.ErrorCode != 0)//OTP not Exist   send SMS
                {
                    var supportOTP = GetSupportOTP(custInfo);
                    apiResult.Message = supportOTP;

                    if (supportOTP.Equals("AWS", StringComparison.OrdinalIgnoreCase))
                    {
                        apiResult = await sOTPSMSBLL.SendAwsOtpAsync(phone, custInfo, req.CustId, custInfo.UserLang);
                    }
                    else if (supportOTP.Equals("SkyLine", StringComparison.OrdinalIgnoreCase))
                    {
                        apiResult = await sOTPSMSBLL.SendSkyLineOTPSMSAsync(phone, custInfo, req.CustId, custInfo.UserLang);
                    }
                    else if (supportOTP.Equals("YC", StringComparison.OrdinalIgnoreCase))
                    {
                        apiResult = await sOTPSMSBLL.SendYcOTPMSAsync(phoneNo, custInfo, req.CustId, custInfo.UserLang); // CNY OTP
                    }
                    else //default NxCloud
                    {
                        apiResult = await nxCloudSMSBLL.SendOTPSMSAsync(phone, custInfo, req.CustId, custInfo.UserLang);
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
            var apiResult = new ApiResult();

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
                    else
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
                apiResult = await sOTPSMSBLL.SendSkyLineSMSAsync(phone, custInfo, custInfo.UserLang, req.CustId);
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
