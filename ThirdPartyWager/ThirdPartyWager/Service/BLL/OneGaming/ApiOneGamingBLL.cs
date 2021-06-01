using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiOneGamingBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;
        private readonly Log2File log2File;
        private readonly ILogger<ApiOneGamingBLL> logger;
        private readonly string logName = Tp.OneGaming.ToString();

        public ApiOneGamingBLL(
            HttpHelper httpHelper,
            Common common,
            Log2File log2File,
            ILogger<ApiOneGamingBLL> logger
        )
        {
            this.httpHelper = httpHelper;
            this.common = common;
            this.log2File = log2File;
            this.logger = logger;
        }

        public async Task<WagerOneGamingModel> GetBetListByTime(string apiUrl, string agentCode, string secretKey, string startTime, string endTime, string guid)
        {
            // param
            string uri = "/allPlayerRecord_Currency";
            string url = $"{apiUrl}{uri}";
            var param = new
            {
                agent_code = agentCode,
                date_start = startTime,
                date_end = endTime,
                bcode = common.HMACSHA256($"{agentCode}{startTime}{endTime}", secretKey)
            };

            // log
            log2File.AddRequestLog(logName, guid, url, param);

            // request
            var r = await DoHttpClientAsync(url, param, guid);
            var result = JsonConvert.DeserializeObject<WagerOneGamingModel>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, object param, string guid = null)
        {
            var sParam = JsonConvert.SerializeObject(param);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(sParam, Encoding.UTF8, "application/json");

            var response = await httpHelper.GetClient().SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                log2File.AddResponseLog(logName, guid, url, sParam, response.Content.ReadAsStringAsync().Result, "Info");
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                LogContext.PushProperty("Tp", logName);
                logger.LogError($"URL {url}");
                logger.LogError($"REQUEST: {sParam}");
                logger.LogError($"RESPONSE: {response}");
                log2File.AddResponseLog(logName, guid, url, sParam, response, "Error");
                return null;
            }
        }
    }
}
