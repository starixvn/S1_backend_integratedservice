using System.Text;

using Newtonsoft.Json;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiTFGBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly ILogger<ApiTFGBLL> _logger;
        public ApiTFGBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _logger = provider.GetRequiredService<ILogger<ApiTFGBLL>>();
        }

        public async Task<Root> GetBetTransactionByTime(string apiUrl, string privateToken, string startTime, string endTime)
        {
            string url = $"https://{apiUrl}/api/v2/bet-transaction/?from_settlement_datetime={startTime}&to_settlement_datetime={endTime}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", privateToken);
            var response = await _httpHelper.GetClient().SendAsync(request);
            string r = null;
            using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
            {
                r = sr.ReadToEnd();
            }

            var result = JsonConvert.DeserializeObject<Root>(r);

            return result;
        }

        public async Task<Root> GetBetTransactionById(string apiUrl, string privateToken, string id)
        {
            string url = $"https://{apiUrl}/api/v2/bet-transaction/?id={id}&settlement_status=settled";
            _logger.LogError($"TFG url:{url}");
            _logger.LogError($"TFG privateToken:{privateToken}");
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", privateToken);
            var response = await _httpHelper.GetClient().SendAsync(request);
            string r = null;
            using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
            {
                r = sr.ReadToEnd();
            }
            _logger.LogError($"TFG respone:{r}");
            var result = JsonConvert.DeserializeObject<Root>(r);

            return result;
        }
    }
}
