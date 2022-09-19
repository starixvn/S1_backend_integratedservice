using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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

        public async Task<WagerMPModel> GetBetListByTime(string wagerUrl, string agent, string deskey, string md5key, long startUnix, long endUnix, string guid)
        {
            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            // param
            var url = wagerUrl;
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
                agent = agent,
                timestamp = timestamp,
                param = _common.AesEncrypt(sParam, deskey),
                key = _common.ToMD5($"{agent}{timestamp}{md5key}").ToLower()
            };
            dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(wager));
            sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // request
            var r = await DoHttpClientAsync(url, sParam);
            var result = JsonConvert.DeserializeObject<WagerMPModel>(r);
            return result;
        }

        public async Task<CurrencyStats_MP> GetBetSummaryByTime(dynamic setting, string currency, string startTime, string endTime)
        {
            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            var wagerUrl = setting["WagerUrl"];
            var deskey = setting["Deskey"];
            var agent = setting["Agent"];
            var md5key = setting["Md5key"];

            // param
            var url = wagerUrl;
            var param = new
            {
                s = 61,
                startTime = startTime,
                endTime = endTime,
                currency = currency
            };
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // log
            _logger.LogInformation(JsonConvert.SerializeObject(param));

            var report = new
            {
                agent = agent,
                timestamp = timestamp,
                param = _common.AesEncrypt(sParam, deskey),
                key = _common.ToMD5($"{agent}{timestamp}{md5key}").ToLower()
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
