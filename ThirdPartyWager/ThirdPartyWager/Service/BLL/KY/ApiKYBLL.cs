using System.Text;
using Newtonsoft.Json;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.KY;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiKYBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiKYBLL> _logger;
        private readonly string _tpName = Tp.KY.ToString();

        public ApiKYBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _logger = provider.GetRequiredService<ILogger<ApiKYBLL>>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        public async Task<WagerKYModel> GetBetListByTime(KYConfig setting, long startUnix, long endUnix, string tpCurrencyId)
        {
            var wagerUrl = setting.WagerUrl;
            var deskey = setting.CurrencyData[tpCurrencyId].Deskey;
            var md5key = setting.CurrencyData[tpCurrencyId].Md5key;
            var agent = setting.CurrencyData[tpCurrencyId].Agent;

            var current = new DateTimeOffset(DateTime.UtcNow);
            var timestamp = current.ToUnixTimeMilliseconds();

            var url = wagerUrl;
            var dicP = new Dictionary<string, string>()
            {
                { "s", "6" },
                { "startTime", (startUnix * 1000).ToString() },
                { "endTime", (endUnix * 1000).ToString() }
            };

            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}"));

            _logger.LogInformation(JsonConvert.SerializeObject(sParam));

            var wager = new
            {
                agent = agent,
                timestamp = timestamp,
                param = _common.AesEncrypt(sParam, deskey),
                key = _common.ToMD5($"{agent}{timestamp}{md5key}").ToLower()
            };
            dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(wager));
            sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var r = await DoHttpClientAsync(url, sParam);
            var result = JsonConvert.DeserializeObject<WagerKYModel>(r);
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
                _log2File.InsertResponseLog(_tpName, url, param, "");
                return result;
            }

            using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
            {
                result = sr.ReadToEnd();
            }
            _log2File.InsertResponseLog(_tpName, url, param, result, false);
            return result;
        }
    }
}
