using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiMegaBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiMegaBLL> _logger;
        private readonly string _tpName = Tp.Mega.ToString();

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

        public ApiMegaBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiMegaBLL>>();
        }

        public async Task<WagerMegaReport> GetReportPlayerListByTime(string apiUrl, string sn, string secretCode, string agentLoginId, string startTime, string endTime, string guid)
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
                    digest = _common.ToMD5(sign).ToUpper(),
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

            _log2File.InsertRequestLog(_tpName, url, sParam, "1");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(sParam, Encoding.UTF8, "application/json");
            
            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, sParam, result);
            }

            return result;
        }
    }
}
