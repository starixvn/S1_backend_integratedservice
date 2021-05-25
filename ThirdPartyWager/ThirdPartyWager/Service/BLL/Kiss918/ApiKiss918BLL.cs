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
    public class ApiKiss918BLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;
        private readonly Log2File log2File;
        private readonly ILogger<ApiKiss918BLL> logger;
        private readonly string logName = Tp.Kiss918.ToString();

        public ApiKiss918BLL(
            HttpHelper httpHelper,
            Common common,
            Log2File log2File,
            ILogger<ApiKiss918BLL> logger
        )
        {
            this.httpHelper = httpHelper;
            this.common = common;
            this.log2File = log2File;
            this.logger = logger;
        }

        public async Task<WagerKiss918Model> GetBetListByTime(string wagerUrl, string authcode, string secretkey, string userName, string player, string startTime, string endTime, int page, int perPage, string guid)
        {
            // param
            long current = common.GetTimestemp1();
            string sign = authcode + player + current + secretkey;

            string url = $"{wagerUrl}/GameLog.ashx";
            var param = new
            {
                userName = player,
                sDate = startTime,
                eDate = endTime,
                authcode = authcode,
                pageIndex = page,
                pageSize = perPage,
                time = current,
                sign = common.ToMD5(sign.ToLower()).ToUpper()
            };

            // log
            log2File.AddRequestLog(logName, guid, url, param);

            // request
            string r = await DoHttpClientAsync(url, param, guid);
            var result = JsonConvert.DeserializeObject<WagerKiss918Model>(r);
            return result;

        }

        public async Task<WagerKiss918ReportPlayer> GetReportPlayerListByTime(string wagerUrl, string authcode, string secretkey, string userName, string startTime, string endTime, string guid)
        {
            // param
            long current = common.GetTimestemp1();
            string sign = authcode + userName + current + secretkey;

            string url = $"{wagerUrl}/AgentTotalReport.ashx";
            var param = new
            {
                userName = userName,
                sDate = startTime,
                eDate = endTime,
                authcode = authcode,
                Type = "ServerTotalReport",
                time = current,
                sign = common.ToMD5(sign.ToLower()).ToUpper()
            };

            // log
            log2File.AddRequestLog(logName, guid, url, param);

            // request
            string r = await DoHttpClientAsync(url, param, guid);
            var result = JsonConvert.DeserializeObject<WagerKiss918ReportPlayer>(r);
            return result;

        }

        private async Task<string> DoHttpClientAsync(string url, object param, string guid = null)
        {
            var dicP = common.FromJson<Dictionary<string, string>>(common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(sParam, Encoding.UTF8, "application/x-www-form-urlencoded");
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
                logger.LogError($"REQUEST: {sParam}");
                logger.LogError($"RESPONSE: {result}");
                log2File.AddResponseLog(logName, guid, url, sParam, response, "Error");
                return result;
                //return null;
            }
        }
    }
}
