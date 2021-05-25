using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiDGBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;
        private readonly Log2File log2File;
        private readonly ILogger<ApiDGBLL> logger;
        private readonly string logName = Tp.DG.ToString();

        public ApiDGBLL(
            HttpHelper httpHelper,
            Common common,
            Log2File log2File,
            ILogger<ApiDGBLL> logger
        )
        {
            this.httpHelper = httpHelper;
            this.common = common;
            this.log2File = log2File;
            this.logger = logger;
        }
        public async Task<WagerDGModel> GetBetList(string apiUrl, string agentName, string apiKey, string guid)
        {
            // param
            string uri = "/game/getReport/";
            string random = common.GetRandomString(8);
            string url = $"{apiUrl}{uri}{agentName}";
            string token = common.ToMD5($"{agentName}{apiKey}{random}").ToLower();
            var param = new
            {
                token = token,
                random = random
            };

            // log
            log2File.AddRequestLog(logName, guid, url, param);

            // request
            var r = await DoHttpClientAsync(url, param, guid);
            var result = JsonConvert.DeserializeObject<WagerDGModel>(r);
            return result;
        }

        public async Task MarkGotBetList(string apiUrl, string agentName, string apiKey, IEnumerable<long> wagerIds, string guid)
        {
            // param
            string uri = "/game/markReport/";
            string random = common.GetRandomString(8);
            string url = $"{apiUrl}{uri}{agentName}";
            string token = common.ToMD5($"{agentName}{apiKey}{random}").ToLower();
            var param = new
            {
                token = token,
                random = random,
                list = wagerIds
            };

            // log
            log2File.AddRequestLog(logName, guid, url, param);

            // request
            var r = await DoHttpClientAsync(url, param, guid);
            var result = JsonConvert.DeserializeObject<WagerDGModel>(r);

            if (result.codeId != 0)
            {
                throw new Exception("Error.");
            }
            return;
        }

        public async Task<WagerRedoDGModel> GetBetListByTime(string apiUrl, string agentName, string apiKey, string beginTime, string endTime, int pageNow, string guid)
        {
            // param
            string uri = "/game/getReport";
            string url = $"{apiUrl}{uri}";
            string token = common.ToMD5($"{agentName}{apiKey}").ToLower();

            var param = new
            {
                token = token,
                beginTime = beginTime,
                endTime = endTime,
                agentName = agentName,
                pageNow = pageNow
            };

            // log
            log2File.AddRequestLog(logName, guid, url, param);

            // request
            var r = await DoHttpClientAsync(url, param, guid);
            var result = JsonConvert.DeserializeObject<WagerRedoDGModel>(r);
            return result;
        }
        private async Task<string> DoHttpClientAsync(string url, object param, string guid = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var json = JsonConvert.SerializeObject(param);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpHelper.GetClient().SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                var result = response.Content.ReadAsStringAsync().Result;
                LogContext.PushProperty("TpId", logName);
                logger.LogError($"URL {url}");
                logger.LogError($"REQUEST: {json}");
                logger.LogError($"RESPONSE: {result}");
                log2File.AddResponseLog(logName, guid, url, json, response);
                return result;
            }
        }
    }
}
