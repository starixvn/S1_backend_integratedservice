using Newtonsoft.Json;
using System.Text;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiAPBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiAPBLL> _logger;
        private readonly string _tpName = Tp.AP.ToString();
        private readonly int _timeZone = 0;

        public ApiAPBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiAPBLL>>();
        }

        public async Task<CurrencyStats_AP> GetBetSummaryByTime(dynamic setting, string startTime, string endTime)
        {
            var apiUrl = setting["ApiUrl"];
            var siteCode = setting["SiteCode"];

            // param
            var url = $"{apiUrl}GetTransSummary";
            string uuid = Guid.NewGuid().ToString();
            string Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffffzzz");

            var param = new
            {
                Timestamp = Timestamp,
                Seq = uuid,
                SiteCode = siteCode,
                DateFrom = startTime,
                DateTo = endTime
            };

            var sParam = JsonConvert.SerializeObject(param);

            // request
            var r = await DoHttpClientAsync(url, sParam, HttpMethod.Post);
            var result = JsonConvert.DeserializeObject<CurrencyStats_AP>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param, HttpMethod method)
        {
            _log2File.InsertRequestLog(_tpName, url, param, "1");

            HttpRequestMessage request;
            request = new HttpRequestMessage(method, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/json");

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
