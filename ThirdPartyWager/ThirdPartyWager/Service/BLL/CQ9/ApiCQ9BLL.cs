using System.Text;
using System.Web;

using Newtonsoft.Json;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiCQ9BLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiCQ9BLL> _logger;
        private readonly string _tpName = Tp.CQ9.ToString();

        public ApiCQ9BLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiCQ9BLL>>();
        }

        public async Task<CurrencyStats_CQ9> GetBetSummaryByTime(dynamic setting, string token, string gametype, string startTime, string endTime)
        {
            var apiUrl = setting["ApiUrl"];

            // param
            var url = $"{apiUrl}/gameboy/report/ss";
            var param = new
            {
                starttime = HttpUtility.UrlEncode(startTime),
                endtime = HttpUtility.UrlEncode(endTime),
                groupby = "day",
                page = 1,
                gametype = gametype
            };

            // request
            var r = await DoHttpClientAsync(url, token, param, HttpMethod.Get);
            var result = JsonConvert.DeserializeObject<CurrencyStats_CQ9>(r);
            return result;
        }
        
        public async Task<CurrencyStats_CQ9> GetLiveBetSummaryByTime(dynamic setting, string token, string gametype, string startTime, string endTime)
        {
            var apiUrl = setting["ApiUrl"];
            // param
            var url = $"{apiUrl}/gameboy/report/ss/live";
            var param = new
            {
                starttime = HttpUtility.UrlEncode(startTime),
                endtime = HttpUtility.UrlEncode(endTime),
                groupby = "day",
                page = 1,
                gamecode = gametype
            };

            // request
            var r = await DoHttpClientAsync(url, token, param, HttpMethod.Get);
            var result = JsonConvert.DeserializeObject<CurrencyStats_CQ9>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string token, object param, HttpMethod method)
        {
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            _log2File.InsertRequestLog(_tpName, url, sParam, "1");

            HttpRequestMessage request;
            if (method == HttpMethod.Get)
            {
                url = $"{url}?{sParam}";
                request = new HttpRequestMessage(method, url);
            }
            else
            {
                request = new HttpRequestMessage(method, url);
                request.Content = new StringContent(sParam, Encoding.UTF8, "application/x-www-form-urlencoded");
            }

            request.Headers.Add("Authorization", token);

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, sParam, result);
            }

            return result;
        }

    }
}
