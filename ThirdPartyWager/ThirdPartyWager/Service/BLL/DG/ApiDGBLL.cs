using System.Net;
using System.Text;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiDGBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly string _tpName = Tp.DG.ToString();

        public ApiDGBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        public async Task<WagerDGModel> GetBetList(DGConfig setting)
        {
            // param
            var uri = "/game/getReport/";
            var random = _common.GetRandomString(8);
            var url = $"{setting.ApiUrl}{uri}{setting.AgentAccount}";
            var token = _common.ToMD5($"{setting.AgentAccount}{setting.APIkey}{random}").ToLower();
            var param = new
            {
                token = token,
                random = random
            };

            // request
            var r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<WagerDGModel>(r);
            return result;
        }

        public async Task MarkGotBetList(DGConfig setting, IEnumerable<long> wagerIds)
        {
            // param
            var uri = "/game/markReport/";
            var random = _common.GetRandomString(8);
            var url = $"{setting.ApiUrl}{uri}{setting.AgentAccount}";
            var token = _common.ToMD5($"{setting.AgentAccount}{setting.APIkey}{random}").ToLower();
            var param = new
            {
                token = token,
                random = random,
                list = wagerIds
            };

            // request
            var r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<WagerDGModel>(r);

            if (result.codeId != 0)
            {
                throw new Exception("Error.");
            }
            return;
        }

        public async Task<WagerRedoDGModel> GetBetListByTime(DGConfig setting, string beginTime, string endTime, int pageNow)
        {
            // param
            var uri = "/game/getReport";
            var url = $"{setting.WagerUrl}{uri}";
            var token = _common.ToMD5($"{setting.AgentAccount}{setting.APIkey}").ToLower();

            var param = new
            {
                token = token,
                beginTime = beginTime,
                endTime = endTime,
                agentName = setting.AgentAccount,
                pageNow = pageNow
            };

            // request
            var r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<WagerRedoDGModel>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, object param)
        {
            var json = JsonConvert.SerializeObject(param);

            _log2File.InsertRequestLog(_tpName, url, json, "1");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, json, result);
            }

            return (response.StatusCode == HttpStatusCode.ServiceUnavailable) ? "{\"codeId\": 300}" : result;
        }
    }
}
