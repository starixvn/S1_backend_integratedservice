using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
    public class ApiMegaBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiMegaBLL> _logger;
        private readonly string _tpName = Tp.Mega.ToString();

        private class ErrorResponse
        {
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

        public async Task<ReportDataMega> GetReportPlayerListByTime(dynamic setting, string currency, string startTime, string endTime)
        {
            // param
            var contentType = "application/json";
            var guid = Guid.NewGuid().ToString();
            var apiUrl = setting["ApiUrl"];
            var sn = setting["sn"];
            var agentLoginId = setting[$"agentLoginId_{currency}"];
            var secretCode = setting[$"secretCode_{currency}"];
            string sign = guid + sn + agentLoginId + secretCode;

            var param = new
            {
                id = guid,
                method = "open.mega.player.total.report",
                @params = new
                {
                    random = guid,
                    digest = _common.ToMD5(sign).ToUpper(),
                    sn = sn,
                    agentLoginId = agentLoginId,
                    type = 1,
                    startTime = startTime,
                    endTime = endTime
                },
                jsonrpc = "2.0",
            };
            var sParam = JsonConvert.SerializeObject(param);

            // request
            var r = await DoHttpClientAsync(apiUrl, sParam, contentType);
            var error = JsonConvert.DeserializeObject<ErrorResponse>(r);
            if (error.error != null)
            {
                var result = new ReportDataMega();
                result.code = error.error.code;
                result.message = error.error.message;
                return result;
            }
            else
            {
                var result = JsonConvert.DeserializeObject<ReportDataMega>(r);
                return result;
            }
        }


        public GameLogUrl GetGameLogUrlByPlayer(string apiUrl, string sn, string secretCode, string loginId, string startTime, string endTime, string guid)
        {
            // param
            var contentType = "application/json";
            string sign = guid + sn + loginId + secretCode;

            var param = new
            {
                id = guid,
                method = "open.mega.player.game.log.url.get",
                @params = new
                {
                    random = guid,
                    digest = _common.ToMD5(sign).ToUpper(),
                    sn = sn,
                    loginId = loginId,
                    startTime = startTime,
                    endTime = endTime
                },
                jsonrpc = "2.0",
            };
            var sParam = JsonConvert.SerializeObject(param);

            // request
            var r = DoHttpClientAsync(apiUrl, sParam, contentType).GetAwaiter().GetResult();
            var error = JsonConvert.DeserializeObject<ErrorResponse>(r);
            if (error.error != null)
            {
                var result = new GameLogUrl();
                result.code = error.error.code;
                result.message = error.error.message;
                return result;
            }
            else
            {
                var result = JsonConvert.DeserializeObject<GameLogUrl>(r);
                result.code = "0";
                return result;
            }
        }

        public WagerMega GetBetListByTime(string wagerUrl, dynamic parameter, string guid, int page)
        {
            // param
            var contentType = "application/x-www-form-urlencoded";
            string uri = "/ashx/log/GameLog.ashx";
            string url = $"{wagerUrl}{uri}";
            var param = new
            {
                action = "playerGameLog",
                user = parameter["playername"],
                sDate = parameter["logdate_begin"],
                eDate = parameter["logdate_end"],
                time = parameter["time"],
                sign = parameter["sign"],
                pageIndex = page,
            };

            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // request
            string r = DoHttpClientAsync(url, sParam, contentType).GetAwaiter().GetResult();
            var result = JsonConvert.DeserializeObject<WagerMega>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param, string contentType)
        {
            var httpContent = new StringContent(param, Encoding.UTF8, contentType);
            var response = await _httpHelper.GetClient().PostAsync(url, httpContent);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, param, result);
                return result;
            }

            _log2File.InsertRequestLog(_tpName, url, param, "1");
            return result;
        }
    }
}
