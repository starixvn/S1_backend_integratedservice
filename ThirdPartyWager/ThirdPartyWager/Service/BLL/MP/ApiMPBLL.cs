using System.Text;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiMPBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiMPBLL> _logger;
        private readonly string _tpName = Tp.MP.ToString();

        public ApiMPBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiMPBLL>>();
        }

        public async Task<WagerMPModel> GetBetListByTime(MPConfig setting, long startUnix, long endUnix)
        {
            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            // param
            var url = setting.WagerUrl;
            var param = new
            {
                s = 6,
                startTime = startUnix * 1000,
                endTime = endUnix * 1000
            };
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // log
            _logger.LogInformation(JsonConvert.SerializeObject(param));

            var wager = new
            {
                agent = setting.Agent,
                timestamp,
                param = _common.AesEncrypt(sParam, setting.Deskey),
                key = _common.ToMD5($"{setting.Agent}{timestamp}{setting.Md5key}").ToLower()
            };
            dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(wager));
            sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // request
            var r = await DoHttpClientAsync(url, sParam);
            var result = JsonConvert.DeserializeObject<WagerMPModel>(r);
            return result;
        }

        public async Task<CurrencyStats_MP> GetBetSummaryByTime(MPConfig setting, string startTime, string endTime)
        {
            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            // param
            var url = setting.WagerUrl;
            var param = new
            {
                s = 61,
                startTime,
                endTime,
            };
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // log
            _logger.LogInformation(JsonConvert.SerializeObject(param));

            var report = new
            {
                agent = setting.Agent,
                timestamp,
                param = _common.AesEncrypt(sParam, setting.Deskey),
                key = _common.ToMD5($"{setting.Agent}{timestamp}{setting.Md5key}").ToLower()
            };
            dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(report));
            sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // request
            var r = await DoHttpClientAsync(url, sParam);
            var result = JsonConvert.DeserializeObject<CurrencyStats_MP>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param)
        {
            _log2File.InsertRequestLog(_tpName, url, param, "1");

            url = $"{url}?{param}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpHelper.GetClient().SendAsync(request);
            string result = null;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, param, result);
                return result;
            }

            using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
            {
                result = sr.ReadToEnd();
            }

            return result;
        }
    }
}
