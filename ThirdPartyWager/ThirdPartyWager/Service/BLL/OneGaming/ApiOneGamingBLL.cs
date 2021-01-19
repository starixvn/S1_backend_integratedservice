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
    public class ApiOneGamingBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;

        public ApiOneGamingBLL(HttpHelper httpHelper, Common common)
        {
            this.httpHelper = httpHelper;
            this.common = common;
        }

        public async Task<WagerOneGamingModel> GetBetListByTime(string apiUrl, string agentCode, string secretKey, string startTime, string endTime)
        {
            // param
            string uri = "/allPlayerRecord_Currency";
            string url = $"{apiUrl}{uri}";
            var param = new
            {
                agent_code = agentCode,
                date_start = startTime,
                date_end = endTime,
                bcode = common.HMACSHA256($"{agentCode}{startTime}{endTime}", secretKey)
            };

            // request
            var r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<WagerOneGamingModel>(r);
            return result;
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
