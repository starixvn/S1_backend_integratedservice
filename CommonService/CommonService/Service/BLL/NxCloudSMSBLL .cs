using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PaymentService.Service.Library;

namespace CommonService.Service.BLL
{
    public class NxCloudSMSBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly OTPSMSService sOTPSMSService;
        private readonly IOptions<Dictionary<string, NxcloudSMSServiceTypeConfig>> NxcloudSMSServiceTypeConfig;
        private readonly Tools tools;
        private readonly SelLangBLL selLangBLL;
        private readonly ILogger<OTPSMSService> logger;
        private readonly Log2File log2File;

        public NxCloudSMSBLL(HttpHelper httpHelper, OTPSMSService sOTPSMSService, IOptions<Dictionary<string, NxcloudSMSServiceTypeConfig>> NxcloudSMSServiceTypeConfig, Tools tools, SelLangBLL selLangBLL, ILogger<OTPSMSService> logger, Log2File log2File)
        {
            this.httpHelper = httpHelper;
            this.sOTPSMSService = sOTPSMSService;
            this.NxcloudSMSServiceTypeConfig = NxcloudSMSServiceTypeConfig;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
            this.logger = logger;
            this.log2File = log2File;
        }
        public ApiResult<SendOTPSMSResult> GetOTPVerifyInfo(int custId)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            SendOTPSMSResult result = sOTPSMSService.GetOTPVerifyinfo(custId);
            apiResult.ErrorCode = (ErrorCode)(result.Status == 1 ? 0 : 1000);
            apiResult.Data = result;

            return apiResult;
        }
        public ApiResult<SendOTPSMSResult> OTPVerifyinfoInsAsync(int custId, string sOTPNum)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            SendOTPSMSResult result = sOTPSMSService.OTPVerifyinfoIns(custId, sOTPNum);
            apiResult.ErrorCode = (ErrorCode)result.Status;
            apiResult.Data = result;

            return apiResult;
        }

        public ApiResult VerifyOPT(VerifyOTPReq verifyOTPReq)
        {
            var apiResult = new ApiResult();
            int result = sOTPSMSService.VerifyOPT(verifyOTPReq);

            apiResult.ErrorCode = (ErrorCode)(result);
            return apiResult;
        }
        public async Task<ApiResult<SendOTPSMSResult>> SendOTPSMSAsync(string PhoneNo, CustInfoProfile custInfo, int custId, string userLang)
        {
            string OTPNum = tools.GetRandomNumString(6);
            var apiResult = new ApiResult<SendOTPSMSResult>();
            ApiResult apiResult1 = await SendOTPSMSAsync(PhoneNo, OTPNum, custInfo, userLang);
            var apiResultSendOTP = apiResult1;
            //if send SMS Success
            if (apiResultSendOTP.ErrorCode == ErrorCode.Success)
            {
                apiResult = OTPVerifyinfoInsAsync(custId, OTPNum);
                //apiResult.ErrorCode = 0;
            }
            else
            {
                apiResult.ErrorCode = apiResultSendOTP.ErrorCode;
                apiResult.Message = apiResultSendOTP.Message;
            }
            return apiResult;
        }
        public string MakeSMSContext(CustInfoProfile custInfo, string OTPNum, string PhoneNo)
        {
            string siteName = custInfo.SiteName;
            string userLang = custInfo.UserLang;
            string smsMessage = $"[{siteName}]{selLangBLL.GetResourceValue("lbl_OTP", userLang)} {OTPNum}";
            string areaCode = custInfo.AreaCode;
            //lbl_OTP
            if (custInfo.SiteName.Equals("Bwing", StringComparison.OrdinalIgnoreCase) || custInfo.SiteName.Equals("Goal123", StringComparison.OrdinalIgnoreCase))
            {
                smsMessage = $"Ung dung xin thong bao ma OTP cua ban la {OTPNum}";
            }
            else if (userLang.Equals("id", StringComparison.OrdinalIgnoreCase) || areaCode.StartsWith("+62") || PhoneNo.StartsWith("62"))
            {
                smsMessage = string.Format(selLangBLL.GetResourceValue("lbl_OTP", "id"), OTPNum);
            }
            else if (userLang.Equals("hi", StringComparison.OrdinalIgnoreCase))
            {
                smsMessage = string.Format(selLangBLL.GetResourceValue("lbl_OTP", custInfo.UserLang), siteName, OTPNum);
            }
            return smsMessage;
        }
        public async Task<ApiResult> SendOTPSMSAsync(string PhoneNo, string OTPNum, CustInfoProfile custInfo, string userLang)
        {
            var apiResult = new ApiResult();
            var Config = NxcloudSMSServiceTypeConfig.Value[custInfo.SiteName].OTP;
            string smsMessage = MakeSMSContext(custInfo, OTPNum, PhoneNo);
            try
            {
                Encoding myEncoding = Encoding.GetEncoding("UTF-8");  //选择编码字符集
                string data = "appkey=" + Config.Appkey + "&secretkey=" + Config.Secretkey + "&phone=" + PhoneNo + "&content=" + smsMessage;
                byte[] bytesToPost = System.Text.Encoding.Default.GetBytes(data); //转换为bytes数据

                string responseResult = String.Empty;
                HttpWebRequest req = (HttpWebRequest)
                HttpWebRequest.Create(Config.ResourceURL);
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
                req.ContentLength = bytesToPost.Length;

                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(bytesToPost, 0, bytesToPost.Length);
                }
                HttpWebResponse cnblogsRespone = (HttpWebResponse)req.GetResponse();
                if (cnblogsRespone != null && cnblogsRespone.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader sr;
                    using (sr = new StreamReader(cnblogsRespone.GetResponseStream()))
                    {
                        responseResult = sr.ReadToEnd();
                    }
                    sr.Close();

                    Dictionary<string, object> responsjson = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseResult);

                    if (responsjson.TryGetValue("code", out var code))
                    {
                        responsjson.TryGetValue("messageid", out var messageid);
                        responsjson.TryGetValue("result", out var result);
                        if (code.Equals("0"))
                        {
                            log2File.Info($"NxCloud - {PhoneNo},{custInfo.UserName},{OTPNum},status:{code},reason:{result},messageid:{messageid}", $"{custInfo.SiteName}_OTP");
                            apiResult.ErrorCode = ErrorCode.Success;
                        }
                        else
                        {
                            apiResult.ErrorCode = ErrorCode.Fail;
                            if (code.Equals("6"))
                            {
                                apiResult.ErrorCode = ErrorCode.PhoneError;
                            }
                            log2File.Error($"NxCloud - {PhoneNo},{custInfo.UserName},{OTPNum},status:{code},reason:{result},messageid:{messageid}", $"{custInfo.SiteName}_OTP");
                        }

                    }
                    else
                    {
                        logger.LogError($"SkyLine - Error HttpStatus = {cnblogsRespone.StatusCode}\r\nRespstatus ={responseResult}");
                        log2File.Error($"SkyLine - Error HttpStatus = {cnblogsRespone.StatusCode}\r\nRespstatus ={responseResult}", $"{custInfo.SiteName}_OTP");

                    }

                }
                else
                {
                    apiResult.ErrorCode = ErrorCode.Fail;
                    log2File.Error($"NxCloud - Http error code :{cnblogsRespone.StatusCode}", $"{custInfo.SiteName}_OTP");
                }
                cnblogsRespone.Close();

            }
            catch (Exception ex)
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.RawMessage = ex.ToString();
                log2File.Error($"NxCloud - Exception error :{ex.ToString()}", $"{custInfo.SiteName}_OTP");
            }


            return apiResult;
        }
        
    }
}
