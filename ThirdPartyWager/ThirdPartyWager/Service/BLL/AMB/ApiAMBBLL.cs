using Newtonsoft.Json;
using System.Text;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiAMBBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiAMBBLL> _logger;
        private readonly string _tpName = Tp.AMB.ToString();
        private readonly int _timeZone = 8;

        public ApiAMBBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiAMBBLL>>();
        }

        public async Task<CurrencyStats_AMB> GetBetSummaryByTime(dynamic setting, string currency, int gameType, string startTime, string endTime)
        {
            var settingByGameType = GetTpListByGameType(setting, currency, gameType);
            var apiUrl = settingByGameType["ApiUrl"];
            var agent = settingByGameType["Agent"];
            var secretKey = settingByGameType["SecretKey"];
            var headerKey = settingByGameType["HeaderKey"];

            // param
            var url = $"{apiUrl}/seamless/report/date";
            var param = new
            {
                agent = agent,
                startDate = startTime,
                endDate = endTime,
            };

            var sParam = JsonConvert.SerializeObject(param);

            // request
            var r = await DoHttpClientAsync(url, sParam, secretKey, headerKey, HttpMethod.Post);
            var result = JsonConvert.DeserializeObject<CurrencyStats_AMB>(r);
            return result;
        }

        private Dictionary<string, string> GetTpListByGameType(dynamic setting, string currency, int gameType)
        {
            var apiUrl = setting["ApiUrl"];
            var agentId = setting["Agent"];
            var secretKey = setting["SecretKey"];
            var headerKey = "x-amb-signature";

            if (gameType == (int)GameType.AMB_RNG)
            {
                apiUrl = setting[$"SlotApiUrl{currency}"];
                agentId = setting[$"SlotAgent{currency}"];
                secretKey = setting[$"SlotSecretKey{currency}"];
                headerKey = "x-ambslot-signature";
            }

            return new Dictionary<string, string>
            {
                { "ApiUrl", apiUrl },
                { "Agent", agentId },
                { "SecretKey", secretKey },
                { "HeaderKey", headerKey }
            };
        }

        private async Task<string> DoHttpClientAsync(string url, string param, string secretKey, string headerKey, HttpMethod method)
        {
            HttpRequestMessage request;
            request = new HttpRequestMessage(method, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/json");
            request.Headers.Add(headerKey, _common.HMACSHA512(param, secretKey));
            _log2File.InsertRequestLog(_tpName, url, $"{param}\r\n{JsonConvert.SerializeObject(request.Headers)}", "1");

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, param, result);
            }

            return result;
        }
    }
}
