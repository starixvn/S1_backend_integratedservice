using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiMegaBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;

        private class ErrorResponse
        {
            //public string result { get; set; }
            public Error error { get; set; }
            public string id { get; set; }
            public string jsonrpc { get; set; }
        }

        private class Error
        {
            public string code { get; set; }
            public string sn { get; set; }
            public string message { get; set; }
            public string reason { get; set; }
            public string action { get; set; }
        }

        public ApiMegaBLL(HttpHelper httpHelper, Common common)
        {
            this.httpHelper = httpHelper;
            this.common = common;
        }

        public async Task<WagerMegaReport> GetReportPlayerListByTime(string apiUrl, string sn, string secretCode, string agentLoginId, string startTime, string endTime)
        {
            // param
            var uuid = Guid.NewGuid();

            string sign = uuid + sn + agentLoginId + secretCode;

            var param = new
            {
                id = uuid,
                method = "open.mega.player.total.report",
                @params = new
                {
                    random = uuid,
                    digest = this.common.ToMD5(sign).ToUpper(),
                    sn = sn,
                    agentLoginId = agentLoginId,
                    type = 1,
                    startTime = startTime,
                    endTime = endTime
                },
                jsonrpc = "2.0",
            };

            // request
            string r = await DoHttpClientAsync(apiUrl, param);
            var error = JsonConvert.DeserializeObject<ErrorResponse>(r);
            if (error.error != null)
            {
                var result = new WagerMegaReport();
                result.code = error.error.code;
                result.message = error.error.message;
                return result;
            }
            else
            {
                var result = JsonConvert.DeserializeObject<WagerMegaReport>(r);
                return result;
            }
        }

        private async Task<string> DoHttpClientAsync(string url, object param)
        {
            var sParam = JsonConvert.SerializeObject(param);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(sParam, Encoding.UTF8, "application/json");
            var response = await httpHelper.GetClient().SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                return null;
            }
        }
    }
}
