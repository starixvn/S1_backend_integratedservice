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

            // request
            log2File.Info($"Guid:\r\n{guid}\r\nUrl:\r\n{url}\r\nRequest:\r\n{JsonConvert.SerializeObject(param)}", Tp.OneGaming.ToString());
            var r = await DoHttpClientAsync(url, param);
            log2File.Info($"Guid:\r\n{guid}\r\nUrl:\r\n{url}\r\nResponse:\r\n{r}", Tp.OneGaming.ToString());
            var result = JsonConvert.DeserializeObject<WagerOneGamingModel>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, object param)
        {
            var sParam = JsonConvert.SerializeObject(param);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(sParam, Encoding.UTF8, "application/json");

            LogContext.PushProperty("TpId", Tp.OneGaming.ToString());
            logger.LogInformation($"URL {url}");
            logger.LogInformation($"REQUEST: {sParam}");

            var response = await httpHelper.GetClient().SendAsync(request);
            var result = (response.IsSuccessStatusCode) ? response.Content.ReadAsStringAsync().Result : null;
            
            logger.LogInformation($"RESPONSE: {result}");
            return result;
        }
    }
}
