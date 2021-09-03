using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiSGBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;
        private readonly Log2File log2File;
        private readonly ILogger<ApiSGBLL> logger;
        private readonly string logName = Tp.SG.ToString();

        public ApiSGBLL(
            HttpHelper httpHelper,
            Common common,
            Log2File log2File,
            ILogger<ApiSGBLL> logger
        )
        {
            this.httpHelper = httpHelper;
            this.common = common;
            this.log2File = log2File;
            this.logger = logger;
        }

        public async Task<WagerSGModel> GetBetListByTime(string apiUrl, string merchantCode, string startTime, string endTime, int page, string guid)
        {
            // param
            string uri = "getBetHistory";
            string url = $"{apiUrl}";
            var param = new
            {
                beginDate = startTime,
                endDate = endTime,
                pageIndex = page,
                merchantCode = merchantCode,
                serialNo = Guid.NewGuid().ToString(),
            };

            // log
            log2File.AddRequestLog(logName, guid, url, param, uri);

            // request
            string r = await DoHttpClientAsync(url, uri, param, guid);
            var result = JsonConvert.DeserializeObject<WagerSGModel>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string uri, object param, string guid = null)
        {
            var json = JsonConvert.SerializeObject(param);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("API", uri);
            request.Headers.Add("DataType", "JSON");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpHelper.GetClient().SendAsync(request);
            try
            {
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (HttpRequestException HREx)
            {
                log2File.AddResponseLog(logName, guid, url, param, response, "Error", request.Headers, HREx.Message);
                return null;
            }
        }
    }
}
