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
    public class ApiWMBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;
        private readonly Log2File log2File;
        private readonly ILogger<ApiWMBLL> logger;
        private readonly string logName = Tp.WM.ToString();

        public ApiWMBLL(
            HttpHelper httpHelper,
            Common common,
            Log2File log2File,
            ILogger<ApiWMBLL> logger
        )
        {
            this.httpHelper = httpHelper;
            this.common = common;
            this.log2File = log2File;
            this.logger = logger;
        }

        public async Task<WagerWMModel> GetBetListByTime(string apiUrl, string vendorId, string signature, string startTime, string endTime, string guid)
        {
            // param
            string url = $"{apiUrl}";
            var param = new
            {
                cmd = "GetDateTimeReport",
                vendorId = vendorId,
                signature = signature,
                startTime = startTime,
                endTime = endTime,
                timetype = 1,
                datatype = 0
            };

            // log
            log2File.AddRequestLog(logName, guid, url, param);

            // request
            string r = await DoHttpClientAsync(url, param, guid);
            var result = JsonConvert.DeserializeObject<WagerWMModel>(r);
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
                LogContext.PushProperty("TpId", logName);
                logger.LogError($"URL {url}");
                logger.LogError($"REQUEST: {sParam}");
                logger.LogError($"RESPONSE: {response}");
                log2File.AddResponseLog(logName, guid, url, sParam, response, "Error");
                return null;
            }
        }
    }
}
