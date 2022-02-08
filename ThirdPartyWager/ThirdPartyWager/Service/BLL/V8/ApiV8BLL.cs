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
    public class ApiV8BLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiV8BLL> _logger;
        private readonly string _tpName = Tp.V8.ToString();

        public ApiV8BLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiV8BLL>>();
        }

        public async Task<WagerV8Model> GetBetListByTime(string wagerUrl, string agent, string deskey, string md5key, long startUnix, long endUnix, string guid)
        {
            var current = new DateTimeOffset(DateTime.UtcNow);
            var timestamp = current.ToUnixTimeMilliseconds();
            // param
            string url = wagerUrl;
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
            string r = await DoHttpClientAsync(url, sParam);
            var result = JsonConvert.DeserializeObject<WagerV8Model>(r);
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
