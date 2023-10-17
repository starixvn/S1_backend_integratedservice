using CommonService.Service.BLL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Microsoft.AspNetCore.Mvc;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class OTPSMSController
    {
        private readonly ILogger<OTPSMSController> _logger;
        private readonly CustInfoBLL _custInfoBLL;
        private readonly Dictionary<int, SMSSupportConfig> _supportOTPConfig;
        private readonly OTPSMSBLL _oTPSMSBLL;
        private readonly Tools _tools;

        private readonly SkyLineBLL _skyLineBLL;
        private readonly Dictionary<OTPProvider, Func<OTPRequestObject, Task<ApiResult>>> _requestHandler;

        public OTPSMSController(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<OTPSMSController>>();
            _custInfoBLL = provider.GetRequiredService<CustInfoBLL>();
            _supportOTPConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SMSSupportConfig>("SMSSupport");
            _oTPSMSBLL = provider.GetRequiredService<OTPSMSBLL>();
            _tools = provider.GetRequiredService<Tools>();

            _skyLineBLL = provider.GetRequiredService<SkyLineBLL>();
            _requestHandler = new Dictionary<OTPProvider, Func<OTPRequestObject, Task<ApiResult>>>()
            {
                { OTPProvider.SkyLine, _skyLineBLL.SendOTP},
                { OTPProvider.NxCloud, provider.GetRequiredService<NxCloudSMSBLL>().SendOTP},
                { OTPProvider.YC, provider.GetRequiredService<YCBLL>().SendOTP},
                { OTPProvider.AWS, provider.GetRequiredService<AWSBLL>().SendOTP},
            };
        }

        [HttpPost]
        public async Task<ApiResult<SendOTPSMSResult>> SendOTPSMSAsync(RequestObject req)
        {
            var custInfo = _custInfoBLL.GetCustInfoProfile(req.CustId);

            // check User
            if (custInfo == null)
            {
                return new ApiResult<SendOTPSMSResult>()
                {
                    ErrorCode = ErrorCode.UserNotExist
                };
            }

            // check PhoneNo
            if (string.IsNullOrEmpty(custInfo.PhoneNo))
            {
                return new ApiResult<SendOTPSMSResult>()
                {
                    ErrorCode = ErrorCode.InvalidParameter
                };
            }

            // check OTP        
            var apiResult = _oTPSMSBLL.GetOTPVerifyInfo(req.CustId);
            if (apiResult.ErrorCode == 0)
            {
                return apiResult;
            }

            var request = new OTPRequestObject
            {
                CustInfoProfile = custInfo,
                OTPNum = _tools.GetRandomNumString(6)
            };
            var otpProvider = GetSupportOTP(custInfo);
            var otpProviderResult = await _requestHandler[otpProvider].Invoke(request);
            if (otpProviderResult.ErrorCode != ErrorCode.Success)
            {
                return new ApiResult<SendOTPSMSResult>()
                {
                    ErrorCode = otpProviderResult.ErrorCode,
                    Message = otpProviderResult.Message
                };
            }

            return _oTPSMSBLL.OTPVerifyinfoInsAsync(req.CustId, request.OTPNum);
        }

        [HttpPost]
        public ApiResult VerifyOTP(VerifyOTPReq verifyOTPReq)
        {
            var apiResult = new ApiResult();

            try
            {
                var custInfo = _custInfoBLL.GetCustInfoProfile(verifyOTPReq.CustId);

                _logger.LogInformation($"UserName: {custInfo.UserName}\r\nPhoneNo: {custInfo.PhoneNo}\r\nInput: {verifyOTPReq.InputOTP}", $"{custInfo.SiteName}_Verify");

                if (verifyOTPReq.InputOTP.Length >= 10)
                {
                    apiResult.ErrorCode = ErrorCode.Fail;
                    return apiResult;
                }

                var GetOTPVerifyResult = _oTPSMSBLL.GetOTPVerifyInfo(verifyOTPReq.CustId);

                if (GetOTPVerifyResult.ErrorCode == 0)
                {
                    apiResult = _oTPSMSBLL.VerifyOPT(verifyOTPReq);
                }
                else
                {
                    apiResult.ErrorCode = GetOTPVerifyResult.ErrorCode;
                }
            }
            catch (Exception ex)
            {
                var msg = $"VerifyOTP || {ex.Message}";
                apiResult.RawMessage = msg;
                _logger.LogError(ex, msg);
            }

            return apiResult;
        }

        [HttpPost]
        public ApiResult<SendOTPSMSResult> GetOTPVerifyInfo(RequestObject req)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            try
            {
                var custInfo = _custInfoBLL.GetCustInfoProfile(req.CustId);

                if (custInfo != null && !string.IsNullOrEmpty(custInfo.PhoneNo))
                {
                    apiResult = _oTPSMSBLL.GetOTPVerifyInfo(req.CustId);
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
                var msg = $"GetOTPVerifyInfo || {ex.Message}";
                apiResult.RawMessage = msg;
                _logger.LogError(ex, msg);
            }
            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult> SendSMSAsync(RequestObject req)
        {
            var apiResult = new ApiResult();

            var custInfo = _custInfoBLL.GetCustInfoProfile(req.CustId);

            if (custInfo != null && !string.IsNullOrEmpty(custInfo.PhoneNo) && custInfo.PhoneVerified)
            {
                var phoneNo = custInfo.PhoneNo.StartsWith("0") ? custInfo.PhoneNo.Substring(1) : custInfo.PhoneNo;
                var areacode = custInfo.AreaCode.StartsWith("+") ? custInfo.AreaCode.Substring(1) : custInfo.AreaCode;
                var phone = $"{areacode}{phoneNo}";
                apiResult = await _skyLineBLL.SendSMS(phone, custInfo, custInfo.UserLang, req.CustId);
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

        private OTPProvider GetSupportOTP(CustInfoProfile custInfo)
        {
            try
            {
                var sMSSupport = _supportOTPConfig[custInfo.SiteId].SMSSupport;
                var otpProvider = sMSSupport.TryGetValue(custInfo.AreaCode, out var areaSupportOTP) ? areaSupportOTP : sMSSupport["Default"];
                return Enum.TryParse(otpProvider, out OTPProvider oTPProvider) ? oTPProvider : OTPProvider.NxCloud;
            }
            catch
            {
                return OTPProvider.NxCloud;
            }
        }
    }
}
