using Amazon.Runtime.Internal.Transform;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Newtonsoft.Json;
using NuGet.Common;
using System.Security.Cryptography;
using System.Text;

namespace CommonService.Service.BLL
{
    public class CRMBLL
    {
        private readonly ILogger<CRMBLL> _logger;
        private readonly HttpHelper _httpHelper;
        private readonly Dictionary<int, CRMConfig> _cRMConfig;
        public CRMBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<CRMBLL>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _cRMConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<CRMConfig>("CRMConfig");
        }

        public async Task<ApiResult> CRMEventSendAsync(CRMRequest request)
        {
            var apiResult = new ApiResult();
            try
            {
                var config = _cRMConfig[request.SiteId];
                var para = new CRMEventData()
                {
                    Tenant = config.TenantID,
                    Event = request.Event.ToString(),
                    Customer = request.CustId.ToString(),
                    Timestamp = DateTime.Now,
                    Visitor = String.Empty,
                    Context = new Context()
                    {
                        OrderAmount = request.OrderAmount,
                    }
                };
                var signature = GenerateHash(config.Token, para);
                var header = new Dictionary<string, string> {
                    { "X-Optimove-Signature-Version", "1"},
                    { "X-Optimove-Signature-Content", signature }
                };

                var result = await _httpHelper.HttpPost1Async(config.ApiUrl, header, JsonConvert.SerializeObject(para), "application/json");

                if (!result.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"StatusCode={result.StatusCode}, ReasonPhrase={result.ReasonPhrase}");
                    apiResult.ErrorCode = ErrorCode.Fail;
                }
                else
                {
                    apiResult.ErrorCode = ErrorCode.Success;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                apiResult.ErrorCode = ErrorCode.Fail;
            }

            return apiResult;
        }
        private static string GenerateHash(string trackAndTriggerToken, object rawTextBody)
        {
            string serializedBody = JsonConvert.SerializeObject(rawTextBody);
            byte[] encodedBody = Encoding.UTF8.GetBytes(serializedBody);
            byte[] tokenBytes = Encoding.UTF8.GetBytes(trackAndTriggerToken);
            using (var hmac = new HMACSHA256(tokenBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(encodedBody);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
