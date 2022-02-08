using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiIconicBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiIconicBLL> _logger;
        private readonly string _tpName = Tp.Iconic.ToString();

        public ApiIconicBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiIconicBLL>>();
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

            // request
            string r = await DoHttpClientAsync(url, param, HttpMethod.Get, true, token);
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
            string r = await DoHttpClientAsync(url, param, HttpMethod.Post, false);
            var result = JsonConvert.DeserializeObject<JwtTokenIconic>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, object param, HttpMethod method, bool header, string token = null)
        {
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            _log2File.InsertRequestLog(_tpName, url, sParam, "1");

            HttpRequestMessage request;
            if (method == HttpMethod.Get)
            {
                url = $"{url}?{sParam}";
                request = new HttpRequestMessage(method, url);
            }
            else
            {
                request = new HttpRequestMessage(method, url);
                request.Content = new StringContent(sParam, Encoding.UTF8, "application/x-www-form-urlencoded");
            }

            if (header)
            {
                request.Headers.Add("Authorization", $"Bearer {token}");
            }

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, sParam, result);
            }

            return (response.StatusCode == HttpStatusCode.ServiceUnavailable) ? "{\"error\":{\"status\":503,\"message\":\"Service Unavailable\",\"ip\":\"-1\"}}" : result;
        }
    }
}
