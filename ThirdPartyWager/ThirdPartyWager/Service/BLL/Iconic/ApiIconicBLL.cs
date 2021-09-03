using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
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
    public class ApiIconicBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;
        private readonly Log2File log2File;
        private readonly ILogger<ApiIconicBLL> logger;
        private readonly string logName = Tp.Iconic.ToString();

        public ApiIconicBLL(
            HttpHelper httpHelper,
            Common common,
            Log2File log2File,
            ILogger<ApiIconicBLL> logger
        )
        {
            this.httpHelper = httpHelper;
            this.common = common;
            this.log2File = log2File;
            this.logger = logger;
        }

        public async Task<WagerIconicModel> GetBetListByTime(string apiUrl, string token, long startUnix, long endUnix, int page, int perPage, string guid)
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

            // log
            log2File.AddRequestLog(logName, guid, url, param);

            // request
            string r = await DoHttpClientAsync(url, param, "GET", true, token, guid);
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

        private async Task<string> DoHttpClientAsync(string url, object param, string method, bool header, string token = null, string guid = null)
        {
            HttpRequestMessage request;
            var dicP = common.FromJson<Dictionary<string, string>>(common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var tmpUrl = url;
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

            dynamic tmpHeader = null;
            if (header)
            {
                request.Headers.Add("Authorization", $"Bearer {token}");
                tmpHeader = request.Headers;
            }

            var response = await httpHelper.GetClient().SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                var result = response.Content.ReadAsStringAsync().Result;
                log2File.AddResponseLog(logName, guid, tmpUrl, sParam, response, "Error", tmpHeader);
                return result;
            }
        }
    }
}
