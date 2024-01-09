using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Newtonsoft.Json;

namespace CommonService.Service.BLL
{
    public class IovationBLL
    {
        private readonly IovationServicesConfig _iovationServicesConfig;
        private readonly IovationService _iovationService;
        private readonly HttpHelper _httpHelper;
        private readonly IHostEnvironment _env;
        private readonly string _storagePath;

        public IovationBLL(IServiceProvider provider)
        {
            _env = provider.GetRequiredService<IHostEnvironment>();
            _storagePath = $@"{_env.ContentRootPath}\Storage\Iovation";
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _iovationService = provider.GetRequiredService<IovationService>();
            _iovationServicesConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<IovationServicesConfig>("Iovation");
        }

        public async Task<ApiResult<IovationCheckResult>> IovationChecksAsync(IovationChecksRequest iovationChecksRequest)
        {
            var apiResult = new ApiResult<IovationCheckResult>();

            var sendIovationChecksPara = new Dictionary<string, string>
            {
                { "accountCode", iovationChecksRequest.accountCode.ToString() },
                { "blackbox", iovationChecksRequest.blackbox },
                { "statedIp", iovationChecksRequest.statedIp },
                { "transactionInsight",iovationChecksRequest.transactionInsight.ToString()},
                { "type",iovationChecksRequest.type}
            };

            var authToken = $"{_iovationServicesConfig.SubscriberID}/{_iovationServicesConfig.SubscriberAccount}:{_iovationServicesConfig.Password}";
            var bytes = Encoding.GetEncoding("utf-8").GetBytes(authToken);
            //編成 Base64 字串
            var base64AuthToken = Convert.ToBase64String(bytes);
            var authorization = new Dictionary<string, string>
            {
                { "Authorization", $"Basic {base64AuthToken}" }
            };
            
            var certificate = GetCertificate();
            using var result = await _httpHelper.HttpPostWithX509(
                _iovationServicesConfig.ResourceURL, 
                sendIovationChecksPara , 
                "application/json",
                authorization,
                certificate
                );
            
            var resultResp = JsonConvert.DeserializeObject<IovationResult>(await result.Content.ReadAsStringAsync());
            var resultIns = _iovationService.IovationResultIns(resultResp);

            if (result.StatusCode == HttpStatusCode.OK && resultIns > 0)
            {
                var deviceId = resultResp?.details?.device?.alias;
                var deviceType = resultResp?.details?.device?.type;
                var score = resultResp?.details?.ruleResults?.score;

                apiResult.ErrorCode = ErrorCode.Success;
                apiResult.Message = resultResp.result;
                apiResult.Data = new IovationCheckResult
                {
                    Result = resultResp.result,
                    Reason = string.IsNullOrEmpty(resultResp.reason) ? "" : resultResp.reason,
                    DeviceId = string.IsNullOrEmpty(deviceId) ? "" : deviceId,
                    DeviceType = string.IsNullOrEmpty(deviceType) ? "" : deviceType,
                    Score = score ?? 0
                };
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Message = resultResp.message;
            }

            return apiResult;
        }
        
        private X509Certificate2 GetCertificate()
        {
            var pem = Path.Combine(_storagePath, "1054302-ci-cert.pem");
            var key = Path.Combine(_storagePath, "1054302-ci-cert.key");
            var cert = X509Certificate2.CreateFromPemFile(pem, key);
            return new X509Certificate2(cert.Export(X509ContentType.Pfx));
        }
    }
}
