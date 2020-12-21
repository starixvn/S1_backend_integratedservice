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
    public class ApiIconicBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;

        public ApiIconicBLL(HttpHelper httpHelper, Common common)
        {
            this.httpHelper = httpHelper;
            this.common = common;
        }

        public async Task<WagerIconicModel> GetBetListByTime(string apiUrl, string token, long startUnix, long endUnix, int page, int perPage)
        {
            // param
            string url = $"{apiUrl}/api/v1/profile/sets";
            var param = new
            {
                updatedStart = startUnix * 1000,
                updatedEnd = endUnix * 1000,
                lang = "en",
                pageSize = perPage,
                page = page
            };

            // request
            string r = await DoHttpClientAsync(url, param, "GET", true, token);
            var result = JsonConvert.DeserializeObject<WagerIconicModel>(r);
            return result;
        }

        public async Task<JwtTokenIconic> GetJwtToken(string apiUrl, string username, string password)
        {
            // param
            string url = $"{apiUrl}/login";
            var param = new
            {
                username = username,
                password = password
            };

            // request
            string r = await DoHttpClientAsync(url, param, "POST", false);
            var result = JsonConvert.DeserializeObject<JwtTokenIconic>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, object param, string method, bool header, string token = null)
        {
            HttpRequestMessage request;
            var dicP = common.FromJson<Dictionary<string, string>>(common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            if (method == "GET")
            {
                url = $"{url}?{sParam}";
                request = new HttpRequestMessage(HttpMethod.Get, url);
            }
            else
            {
                request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(sParam, Encoding.UTF8, "application/x-www-form-urlencoded");
            }

            if (header == true)
            {
                request.Headers.Add("Authorization", $"Bearer {token}");
            }

            var response = await httpHelper.GetClient().SendAsync(request);
            return response.Content.ReadAsStringAsync().Result;
        }
    }
}
