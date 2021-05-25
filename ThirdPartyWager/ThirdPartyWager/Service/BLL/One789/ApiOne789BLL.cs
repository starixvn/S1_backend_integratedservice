using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiOne789BLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;
        private readonly Log2File log2File;
        private readonly ILogger<ApiOne789BLL> logger;
        private readonly string logName = Tp.One789.ToString();

        public ApiOne789BLL(
            HttpHelper httpHelper,
            Common common,
            Log2File log2File,
            ILogger<ApiOne789BLL> logger
        )
        {
            this.httpHelper = httpHelper;
            this.common = common;
            this.log2File = log2File;
            this.logger = logger;
        }

        public async Task<dynamic> GetBetListByTime(string apiUrl, string apiKey, string startTime, string endTime, int page, int perPage, int gameType, string guid)
        {
            // param
            var uri = "";
            switch (gameType)
            {
                case (int)GameType.One789_Lottery:
                    uri = "/report/lottery/date-time-report";
                    break;
                case (int)GameType.One789_Lottery_789:
                    uri = "/report/lottery789/date-time-report";
                    break;
                case (int)GameType.One789_LiveCasino:
                    uri = "/report/789-casino/sedie/date-time-report";
                    break;
                default:
                    break;
            }

            string url = $"{apiUrl}{uri}";
            string param = $"?from={startTime}&to={endTime}&type=1&size={perPage}&page={page}";

            // log
            log2File.AddRequestLog(logName, guid, url, param);

            // request
            var r = await DoHttpClientAsync(url, param, apiKey, guid);
            dynamic result = JsonConvert.DeserializeObject(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param, string apiKey, string guid = null)
        {
            var path = url + param;
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("x-one-api-key", HttpUtility.UrlEncode(apiKey));

            var response = await httpHelper.GetClient().SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                var result = $"[{response.Content.ReadAsStringAsync().Result}]";
                LogContext.PushProperty("TpId", logName);
                logger.LogError($"URL {url}");
                logger.LogError($"REQUEST: {param}");
                logger.LogError($"HEADER: {request.Headers}");
                logger.LogError($"RESPONSE: {result}");
                log2File.AddResponseLog(logName, guid, url, param, response, "Error", request.Headers);
                return result;
            }
        }
    }
}
