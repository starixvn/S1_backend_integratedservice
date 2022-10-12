using Newtonsoft.Json;
using System.Text;
using System.Web;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiYLBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiYLBLL> _logger;
        private readonly string _tpName = Tp.YL.ToString();

        public ApiYLBLL(
            IServiceProvider provider
        )
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiYLBLL>>();
        }

        public async Task<CurrencyStats_YL> GetBetSummaryByTime(dynamic setting, string startTime, string endTime)
        {
            var apiUrl = setting["ApiUrl"];
            var cert = setting["cert"];

            // param
            var url = $"{apiUrl}getSummarizedReportByBetTimeAndCurrency";
            var param = new
            {
                cert = cert,
                st = HttpUtility.UrlEncode(startTime),
                et = HttpUtility.UrlEncode(endTime),
            };

            // request
            var r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<CurrencyStats_YL>(r);
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
