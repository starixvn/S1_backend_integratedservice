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
    public class ApiOneGamingBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly string _tpName = Tp.OneGaming.ToString();

        public ApiOneGamingBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        public async Task<WagerOneGamingModel> GetBetListByTime(string apiUrl, string agentCode, string secretKey, string startTime, string endTime, string guid)
        {
            // param
            var uri = "/allPlayerRecord_Currency";
            var url = $"{apiUrl}{uri}";
            var param = new
            {
                agent_code = agentCode,
                date_start = startTime,
                date_end = endTime,
                bcode = _common.HMACSHA256($"{agentCode}{startTime}{endTime}", secretKey)
            };

            // request
            var r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<WagerOneGamingModel>(r);
            return result;
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
                return result;
            }

            _log2File.InsertResponseLog(_tpName, url, sParam, result, false);
            return result;
        }
    }
}
