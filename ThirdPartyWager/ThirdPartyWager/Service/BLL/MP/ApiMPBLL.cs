using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
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
        private readonly int _tpId = (int)Tp.MP;
        private readonly string _logName = Tp.MP.ToString();

        public ApiMPBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiMPBLL>>();
        }

        public async Task<WagerMPModel> GetBetListByTime(string wagerUrl, string agent, string deskey, string md5key, long startUnix, long endUnix, string guid)
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

            var wager = new
            {
                agent = agent,
                timestamp = timestamp,
                param = _common.AesEncrypt(sParam, deskey),
                key = _common.ToMD5($"{agent}{timestamp}{md5key}").ToLower()
            };
            dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(wager));
            sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // log
            _log2File.AddRequestLog(_logName, guid, url, sParam);

            // request
            string r = await DoHttpClientAsync(url, sParam, guid);
            var result = JsonConvert.DeserializeObject<WagerMPModel>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param, string guid = null)
        {
            string tmpUrl = url;
            url = $"{url}?{param}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpHelper.GetClient().SendAsync(request);
            string result = null;
            if (response.IsSuccessStatusCode)
            {
                using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
                {
                    result = sr.ReadToEnd();
                }
                _log2File.AddResponseLog(_logName, guid, tmpUrl, param, result, "Info");
                return result;
            }
            else
            {
                _log2File.AddResponseLog(_logName, guid, tmpUrl, param, response, "Error");
                return null;
            }
        }
    }
}
