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
        private readonly ICacheHelper _redisHelper;
        private readonly SkyLineBLL _skyLineBLL;
        private readonly Dictionary<OTPProvider, Func<OTPRequestObject, Task<ApiResult>>> _requestHandler;
        private readonly IEnumerable<ServerInfo> _serverInfo;
        private readonly string _environment;

        public OTPSMSController(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<OTPSMSController>>();
            _custInfoBLL = provider.GetRequiredService<CustInfoBLL>();
            _supportOTPConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SMSSupportConfig>("SMSAutoSupport");
            _oTPSMSBLL = provider.GetRequiredService<OTPSMSBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _serverInfo = provider.GetRequiredService<IEnumerable<ServerInfo>>();
            _skyLineBLL = provider.GetRequiredService<SkyLineBLL>();
            _requestHandler = new Dictionary<OTPProvider, Func<OTPRequestObject, Task<ApiResult>>>()
            {
                { OTPProvider.SkyLine, _skyLineBLL.SendOTP},
                { OTPProvider.NxCloud, provider.GetRequiredService<NxCloudSMSBLL>().SendOTP},
                //{ OTPProvider.YC, provider.GetRequiredService<YCBLL>().SendOTP},
                //{ OTPProvider.AWS, provider.GetRequiredService<AWSBLL>().SendOTP},
                //{ OTPProvider.SmsCloud, provider.GetRequiredService<SmsCloudBLL>().SendOTP},
            };
            _environment = provider.GetRequiredService<IConfiguration>().GetSection("env").Get<string>();
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
            var otpProvider = GetSupportOTP(custInfo.SiteId, custInfo.AreaCode, null, custInfo.CustId);
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
        public async Task<ApiResult<SendOTPSMSResult>> SendSignUpOTPSMSAsync(SignUpOTPRequest req)
        {
            // check PhoneNo
            if (string.IsNullOrEmpty(req.PhoneNo) || string.IsNullOrEmpty(req.AreaCode))
            {
                return new ApiResult<SendOTPSMSResult>()
                {
                    ErrorCode = ErrorCode.InvalidParameter
                };
            }

            var areaPhoneNo = $"{req.AreaCode}{req.PhoneNo}";
            var siteName = _serverInfo.FirstOrDefault(x => x.SiteId == req.SiteId)?.SiteName;
            
            // check OTP        
            var apiResult = _oTPSMSBLL.GetSignUpOTPVerifyInfo(areaPhoneNo);
            if (apiResult.ErrorCode == 0)
            {
                return apiResult;
            }
            
            // for singUp Info
            var signUpInfo = new CustInfoProfile
            {
                SiteId = req.SiteId,
                SiteName = siteName,
                UserName = "",
                CurrencyId = req.CurrencyId,
                UserLang = req.UserLang,
                AreaCode = req.AreaCode,
                PhoneNo = req.PhoneNo,
            };
            
            var request = new OTPRequestObject
            {
                CustInfoProfile = signUpInfo,
                OTPNum = _tools.GetRandomNumString(6)
            };
            var otpProvider = GetSupportOTP(req.SiteId ,req.AreaCode ,req.PhoneNo );
            var otpProviderResult = await _requestHandler[otpProvider].Invoke(request);
            
            if (otpProviderResult.ErrorCode != ErrorCode.Success)
            {
                return new ApiResult<SendOTPSMSResult>()
                {
                    ErrorCode = otpProviderResult.ErrorCode,
                    Message = otpProviderResult.Message
                };
            }
            
            return _oTPSMSBLL.SignUpOTPVerifyinfoInsAsync(areaPhoneNo, request.OTPNum);
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
        public ApiResult SignUpVerifyOTP(SignUpVerifyOTPReq req)
        {
            var apiResult = new ApiResult();

            try
            {
                _logger.LogInformation($"PhoneNo: {req.AreaPhoneNo}\r\nInput: {req.InputOTP}");

                if (req.InputOTP.Length >= 10)
                {
                    apiResult.ErrorCode = ErrorCode.Fail;
                    return apiResult;
                }

                var getOtpVerifyResult = _oTPSMSBLL.GetSignUpOTPVerifyInfo(req.AreaPhoneNo);

                if (getOtpVerifyResult.ErrorCode == 0)
                {
                    apiResult = _oTPSMSBLL.SignUpVerifyOTP(req);
                }
                else
                {
                    apiResult.ErrorCode = getOtpVerifyResult.ErrorCode;
                }
            }
            catch (Exception ex)
            {
                var msg = $"SignUpVerifyOTP || {ex.Message}";
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

        private OTPProvider GetSupportOTP(int siteId, string areaCode , string phoneNo, int custId = 0)
        {
            try
            {
                // 依Site取得簡訊設定
                var sMSSupport = _supportOTPConfig[siteId].SMSSupport;
                // 使用電話國碼取得簡訊供應商支援列表
                var otpProviderList = sMSSupport.TryGetValue(areaCode, out var areaSupportOTP) ? areaSupportOTP : sMSSupport["Default"];
                
                // 先使用第一個當預設值
                var result = otpProviderList[0];

                // 檢查玩家之前發送紀錄
                
                var key = custId == 0 ? $"UserGetSupportOTP:{areaCode}:{phoneNo}" : $"UserGetSupportOTP:{custId}";
                var alreadyTryProviderList = _redisHelper.GetListByRange<string>(key, 0, -1);

                // 比對發送紀錄與簡訊供應商支援列表，發送過就換下一間
                if (otpProviderList.Count != alreadyTryProviderList.Count)
                {
                    foreach (var otpProvider in otpProviderList)
                    {
                        if (!alreadyTryProviderList.Contains(otpProvider.ToString()))
                        {
                            result = otpProvider;
                            break;
                        }
                    }
                }
                else
                {   
                    // 全部試過一輪，清掉重來
                    _redisHelper.KeyDelete(key);
                }
                

                //記錄這一次選擇的結果
                _redisHelper.InsertList(key, TimeSpan.FromDays(1), new List<string> { result.ToString() });

                return result;
            }
            catch
            {
                return OTPProvider.NxCloud;
            }
        }
    }
}
