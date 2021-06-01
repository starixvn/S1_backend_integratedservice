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
    public class ApiV8BLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;
        private readonly Log2File log2File;
        private readonly ILogger<ApiV8BLL> logger;
        private readonly string logName = Tp.V8.ToString();

        public ApiV8BLL(
            HttpHelper httpHelper,
            Common common,
            Log2File log2File,
            ILogger<ApiV8BLL> logger
        )
        {
            this.httpHelper = httpHelper;
            this.common = common;
            this.log2File = log2File;
            this.logger = logger;
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
            var dicP = common.FromJson<Dictionary<string, string>>(common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var wager = new
            {
                agent = agent,
                timestamp = timestamp,
                param = common.AesEncrypt(sParam, deskey),
                key = common.ToMD5($"{agent}{timestamp}{md5key}").ToLower()
            };
            dicP = common.FromJson<Dictionary<string, string>>(common.ToJson(wager));
            sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // log
            log2File.AddRequestLog(logName, guid, url, sParam);

            // request
            string r = await DoHttpClientAsync(url, sParam, guid);
            var result = JsonConvert.DeserializeObject<WagerV8Model>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param, string guid = null)
        {
            string tmpUrl = url;
            url = $"{url}?{param}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await httpHelper.GetClient().SendAsync(request);
            string result = null;
            if (response.IsSuccessStatusCode)
            {
                using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
                {
                    result = sr.ReadToEnd();
                }
                log2File.AddResponseLog(logName, guid, tmpUrl, param, result, "Info");
                return result;
            }
            else
            {
                LogContext.PushProperty("Tp", logName);
                logger.LogError($"URL {tmpUrl}");
                logger.LogError($"REQUEST: {param}");
                logger.LogError($"RESPONSE: {response}");
                log2File.AddResponseLog(logName, guid, tmpUrl, param, response, "Error");
                return null;
            }
        }
    }
}
