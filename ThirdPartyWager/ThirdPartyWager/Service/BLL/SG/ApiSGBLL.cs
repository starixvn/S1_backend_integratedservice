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
    public class ApiSGBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiSGBLL> _logger;
        private readonly string _tpName = Tp.SG.ToString();

        public ApiSGBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiSGBLL>>();
        }

        public async Task<WagerSGModel> GetBetListByTime(string apiUrl, string merchantCode, string startTime, string endTime, int page, string guid)
        {
            // param
            var uri = "getBetHistory";
            var url = $"{apiUrl}";
            var param = new
            {
                beginDate = startTime,
                endDate = endTime,
                pageIndex = page,
                merchantCode = merchantCode,
                serialNo = Guid.NewGuid().ToString(),
            };

            // request
            var r = await DoHttpClientAsync(url, uri, param);
            var result = JsonConvert.DeserializeObject<WagerSGModel>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string uri, object param)
        {
            var json = JsonConvert.SerializeObject(param);
            _log2File.InsertRequestLog(_tpName, url, json, "1");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("API", uri);
            request.Headers.Add("DataType", "JSON");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpHelper.GetClient().SendAsync(request);
            try
            {
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (HttpRequestException HREx)
            {
                _log2File.InsertResponseLog(_tpName, url, json, HREx.Message);
                return null;
            }
        }
    }
}
