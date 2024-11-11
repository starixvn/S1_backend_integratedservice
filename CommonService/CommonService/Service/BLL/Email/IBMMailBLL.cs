using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Newtonsoft.Json;
using System.Text;
using System.Xml.Serialization;

namespace CommonService.Service.BLL
{
    public class IBMMailBLL
    {
        private readonly Dictionary<int, IBMMail> _iBMMailConfig;
        private readonly HttpHelper _httpHelper;
        private readonly ILogger<IBMMailBLL> _logger;

        public IBMMailBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _iBMMailConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<IBMMail>("IBM");
            _logger = provider.GetRequiredService<ILogger<IBMMailBLL>>();
        }

        public async Task<ApiResult> SendMailAsync(CustInfoProfile custInfo, dynamic dynamicTemplateData)
        {
            var apiResult = new ApiResult();

            if (!_iBMMailConfig.TryGetValue(custInfo.SiteId, out var config))
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                return apiResult;
            }
            if (config is null)
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                return apiResult;
            }

            if (!ParameterCheck(custInfo))
            {
                apiResult.Message = $"parameter check fail. parameter={JsonConvert.SerializeObject(custInfo)}";
            }

            //step 1 oauth : get access token
            var oauthResult = await OauthAsync($"{config.BaseUrl}{config.OathPath}", config.ClientId, config.ClientSecret, config.RefreshToken);

            if (oauthResult.ErrorCode != ErrorCode.Success)
                return new ApiResult(oauthResult.ErrorCode, oauthResult.Message);

            //step 2 add contact : add member email to contact
            var addContactResult = await AddContactAsync($"{config.BaseUrl}{config.AddContactPath}", oauthResult.Data.token_type, oauthResult.Data.access_token, custInfo);

            if (addContactResult.ErrorCode != ErrorCode.Success)
                return new ApiResult(addContactResult.ErrorCode, addContactResult.Message);

            //step3 send : send to member
            var mailingId = config.CurrencySettingList.FirstOrDefault(x => x.CurrencyId == custInfo.CurrencyId).MailingId;
            var sendResult = await SendAsync($"{config.BaseUrl}{config.SendPath}", oauthResult.Data.token_type, oauthResult.Data.access_token, custInfo, mailingId);

            if (sendResult.ErrorCode != ErrorCode.Success)
                return new ApiResult(sendResult.ErrorCode, sendResult.Message);

            apiResult.ErrorCode = ErrorCode.Success;

            return apiResult;
        }

        #region Private Methods

        private bool ParameterCheck(CustInfoProfile custInfo)
        {
            if (string.IsNullOrWhiteSpace(custInfo.UserName)
                || string.IsNullOrWhiteSpace(custInfo.Email)
                || string.IsNullOrWhiteSpace(custInfo.IsoCode)
                || string.IsNullOrWhiteSpace(custInfo.CountryCode))
                return false;

            return true;
        }

        private async Task<ApiResult<OauthResponse>> OauthAsync(string url, string clientId, string clientSecret, string refreshToken)
        {
            var result = new ApiResult<OauthResponse>();
            try
            {
                var request = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "client_id", clientId },
                    { "client_secret", clientSecret},
                    { "refresh_token", refreshToken }
                };

                var parameters = JsonConvert.SerializeObject(request);
                var apiResult = await _httpHelper.HttpPostAsync(url, request, "application/x-www-form-urlencoded");

                if (!IsJSON(apiResult))
                {
                    result.Message = "No Result, response is not json.";
                    return result;
                }

                var jsonResult = JsonConvert.DeserializeObject<OauthResponse>(apiResult);

                if (jsonResult?.error is not null)
                {
                    result.Message = $"response error:{apiResult}";
                    return result;
                }

                if (jsonResult is null || string.IsNullOrWhiteSpace(jsonResult?.access_token))
                {
                    result.Message = $"response don't have access token:{apiResult}";
                    return result;
                }

                result.ErrorCode = ErrorCode.Success;
                result.Data = jsonResult;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                result.Message = $"{ex.Message}";
                return result;
            }
        }

        private async Task<ApiResult<AddContactAndSendResponse>> AddContactAsync(string url, string tokenType, string accessToken, CustInfoProfile custInfo)
        {
            var result = new ApiResult<AddContactAndSendResponse>();
            try
            {
                var header = new Dictionary<string, string>
                {
                    { "Authorization", $"{tokenType} {accessToken}" }
                };
                var request = new AddContactRequest(5730358, 3, custInfo.UserName, custInfo.Email, custInfo.IsoCode, custInfo.CountryCode);

                string xmlContent = SerializeToXml(request);
                var apiResult = await _httpHelper.HttpPostAsync(url, xmlContent, "text/xml", header);

                if (!IsXML(apiResult))
                {
                    result.Message = "No Result, response is not xml.";
                    return result;
                }

                var jsonResult = XMLDeserialize<AddContactAndSendResponse>(apiResult);

                if (jsonResult?.Body.Fault is not null)
                {
                    result.Message = $"response error:{JsonConvert.SerializeObject(jsonResult)}";
                    return result;
                }

                if (jsonResult is null || jsonResult?.Body?.Result?.Success?.ToUpper() == "FALSE")
                {
                    result.Message = $"response don't have access token:{JsonConvert.SerializeObject(jsonResult)}";
                    return result;
                }

                result.ErrorCode = ErrorCode.Success;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                result.Message = $"{ex.Message}";
                return result;
            }
        }

        private async Task<ApiResult<AddContactAndSendResponse>> SendAsync(string url, string tokenType, string accessToken, CustInfoProfile custInfo, int mailingId)
        {
            var result = new ApiResult<AddContactAndSendResponse>();
            try
            {
                var header = new Dictionary<string, string>
                {
                    { "Authorization", $"{tokenType} {accessToken}" }
                };
                var request = new SendRequest(mailingId, custInfo.Email);

                string xmlContent = SerializeToXml(request);
                var apiResult = await _httpHelper.HttpPostAsync(url, xmlContent, "text/xml", header);

                if (!IsXML(apiResult))
                {
                    result.Message = "No Result, response is not xml.";
                    return result;
                }

                var jsonResult = XMLDeserialize<AddContactAndSendResponse>(apiResult);

                if (jsonResult?.Body.Fault is not null)
                {
                    result.Message = $"response error:{JsonConvert.SerializeObject(jsonResult)}";
                    return result;
                }

                if (jsonResult is null || jsonResult.Body.Result.Success.ToUpper() == "FALSE")
                {
                    result.Message = $"response don't have access token:{JsonConvert.SerializeObject(jsonResult)}";
                    return result;
                }

                result.ErrorCode = ErrorCode.Success;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                result.Message = $"{ex.Message}";
                return result;
            }
        }

        #endregion

        protected bool IsJSON(string jsonString)
        {
            var value = jsonString.Trim();

            return (value.StartsWith("{") && value.EndsWith("}")) || (value.StartsWith("[") && value.EndsWith("]"));
        }

        protected string SerializeToXml<T>(T obj)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            using (var stringWriter = new Utf8StringWriter())
            {
                xmlSerializer.Serialize(stringWriter, obj);
                return stringWriter.ToString();
            }
        }

        protected class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }

        protected bool IsXML(string xmlString)
        {
            var value = xmlString.Trim();

            return (value.StartsWith("<") && value.EndsWith(">"));
        }

        protected T XMLDeserialize<T>(string xmlString) where T : class
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(T));
            using (TextReader reader = new StringReader(xmlString))
            {
                var deserializationObj = (T)deserializer.Deserialize(reader);
                return deserializationObj;
            };
        }

    }
}
