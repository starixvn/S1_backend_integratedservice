using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
    public class ApiWMBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiWMBLL> _logger;
        private readonly string _tpName = Tp.WM.ToString();

        public ApiWMBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiWMBLL>>();
        }

        public async Task<WagerWMModel> GetBetListByTime(string apiUrl, string vendorId, string signature, string startTime, string endTime, string guid)
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
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            _log2File.InsertRequestLog(_tpName, url, sParam, "1");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(sParam, Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, sParam, result);
                return null;
            }

            return result;
        }
    }
}
