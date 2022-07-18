using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using System.Data;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiJILIBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiJILIBLL> _logger;
        private readonly string _tpName = Tp.JILI.ToString();
        private readonly int _timeZone = -4;

        public ApiJILIBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiJILIBLL>>();
        }

        public async Task<CurrencyStats_JILI> GetBetSummaryByTime(dynamic setting, string startTime, string endTime)
        {
            var apiUrl = setting["ApiUrl"];
            var agentId = setting["AgentId"];

            // param
            var url = $"{apiUrl}/GetBetRecordSummary";
            var encryptParam = new
            {
                StartTime = DateTime.Parse(startTime).ToString("yyyy-MM-dd'T'HH:mm:ss"),
                EndTime = DateTime.Parse(endTime).ToString("yyyy-MM-dd'T'HH:mm:ss"),
                AgentId = agentId,
            };

            var param = new
            {
                StartTime = encryptParam.StartTime,
                EndTime = encryptParam.EndTime,
                AgentId = agentId,
                Key = DoEncrypt(setting, encryptParam)
            };
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // request
            var r = await DoHttpClientAsync(url, sParam, HttpMethod.Get);
            var result = JsonConvert.DeserializeObject<CurrencyStats_JILI>(r);
            return result;
        }

        private string DoEncrypt(dynamic setting, object param)
        {
            var agentId = setting["AgentId"];
            var agentKey = setting["AgentKey"];

            var now = new DateTimeOffset(DateTime.UtcNow);
            var current = now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToString("yyMMd");
            string g = _common.ToMD5($"{current}{agentId}{agentKey}").ToLower();
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            string md5Param = _common.ToMD5($"{sParam}{g}").ToLower();
            string key = _common.GetRandomString(6) + md5Param + _common.GetRandomString(6);
            return key;
        }

        private async Task<string> DoHttpClientAsync(string url, string param, HttpMethod method)
        {
            HttpRequestMessage request;
            url = $"{url}?{param}";
            request = new HttpRequestMessage(method, url);

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, param, result);
            }

            return result;
        }
    }
}
