using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiSGBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;
        private readonly Log2File log2File;

        public ApiSGBLL(HttpHelper httpHelper, Common common, Log2File log2File)
        {
            this.httpHelper = httpHelper;
            this.common = common;
            this.log2File = log2File;
        }

        public async Task<WagerSGModel> GetBetListByTime(string apiUrl, string merchantCode, string startTime, string endTime, int page)
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

            // request
            string r = await DoHttpClientAsync(url, uri, param);
            //log2File.Info($"{JsonConvert.SerializeObject(param)}\r\n{r}", "ApiSGBLL");
            var result = JsonConvert.DeserializeObject<WagerSGModel>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string uri, object param)
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
                log2File.Error($"{HREx}\r\n{json}\r\n{response?.Content?.ReadAsStringAsync()?.Result}", "ApiSGBLL");
                return null;
            }
        }
    }
}
