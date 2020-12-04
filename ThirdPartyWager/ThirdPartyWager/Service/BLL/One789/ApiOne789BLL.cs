using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiOne789BLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;

        public ApiOne789BLL(HttpHelper httpHelper, Common common)
        {
            this.httpHelper = httpHelper;
            this.common = common;
        }

        public async Task<dynamic> GetBetListByTime(string apiUrl, string apiKey, string startTime, string endTime, int page, int perPage, int gameType)
        {
            // param
            var uri = "";
            switch (gameType)
            {
                case (int)GameType.One789_Lottery:
                    uri = "/report/lottery/date-time-report";
                    break;
                case (int)GameType.One789_Lottery_789:
                    uri = "/report/lottery789/date-time-report";
                    break;
                case (int)GameType.One789_LiveCasino:
                    uri = "/report/789-casino/sedie/date-time-report";
                    break;
                default:
                    break;
            }

            string url = $"{apiUrl}{uri}";
            string param = $"?from={startTime}&to={endTime}&type=1&size={perPage}&page={page}";

            // request
            var r = await DoHttpClientAsync(url, param, apiKey);
            dynamic result = JsonConvert.DeserializeObject(r);

            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param, string apiKey)
        {
            var path = url + param;
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("x-one-api-key", HttpUtility.UrlEncode(apiKey));

            var response = await httpHelper.GetClient().SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                return $"[{response.Content.ReadAsStringAsync().Result}]";
            }
        }

    }
}
