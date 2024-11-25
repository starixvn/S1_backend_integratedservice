using System.Text;
using System.Web;

using Newtonsoft.Json;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiSexyBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiSexyBLL> _logger;
        private readonly string _tpName = Tp.AESexy.ToString();

        public ApiSexyBLL(
            IServiceProvider provider
        )
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiSexyBLL>>();
        }

        public async Task<CurrencyStats_Sexy> GetBetSummaryByTime(dynamic setting, string startTime, string endTime)
        {
            var apiUrl = setting["WagerUrl"];
            var agentId = setting["AgentId"];
            var cert = setting["cert2"];
            var platform = setting["Platform"];
            var gameType = setting["GameType"];

            // param
            var url = $"{apiUrl}/fetch/getSummaryByBetTimeHour";
            var param = new
            {
                cert = cert,
                agentId = agentId,
                startTime = HttpUtility.UrlEncode(DateTime.Parse(startTime).ToString("yyyy-MM-dd'T'HHzzz")),
                endTime = HttpUtility.UrlEncode(DateTime.Parse(endTime).AddSeconds(1).ToString("yyyy-MM-dd'T'HHzzz")),
                platform = platform,
                gameType = gameType
            };

            // request
            var r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<CurrencyStats_Sexy>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, object param)
        {
            _log2File.InsertRequestLog(_tpName, url, JsonConvert.SerializeObject(param), "1");

            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(sParam, Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, JsonConvert.SerializeObject(param), result);
            }

            return result;
        }

    }
}
