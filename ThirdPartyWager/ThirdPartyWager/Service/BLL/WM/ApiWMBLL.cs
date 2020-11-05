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
    public class ApiWMBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;

        public ApiWMBLL(HttpHelper httpHelper, Common common)
        {
            this.httpHelper = httpHelper;
            this.common = common;
        }

        public async Task<WagerWMModel> GetBetListByTime(string apiUrl, string vendorId, string signature, string startTime, string endTime)
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

            // request
            string r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<WagerWMModel>(r);
            return result;

        }

        private async Task<string> DoHttpClientAsync(string url, object param)
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
                return null;
            }
        }
    }
}
