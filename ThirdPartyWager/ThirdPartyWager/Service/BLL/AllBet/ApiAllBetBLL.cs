using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiAllBetBLL
    {
        private readonly string _seamlessUrl;
        private readonly byte[] NULL_IV = Convert.FromBase64String("AAAAAAAAAAA=");
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiAllBetBLL> _logger;
        private readonly string tpName = Tp.Allbet.ToString();

        public ApiAllBetBLL(
            IServiceProvider provider,
            IHostEnvironment environment,
            IConfiguration configuration
        )
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiAllBetBLL>>();
            _seamlessUrl = configuration.GetValue<string>("TpSWserviceUrl");
        }


        public async Task<WagerAllBetModel> GetBetListByTime(string apiUrl, string desKey, string md5Key, string propertyId, string startTime, string endTime, int pageIndex, string guid)
        {
            // param
            string uri = "/betlog_histories";
            string url = $"{apiUrl}{uri}";
            int pageSize = 1000;
            string parameter = $"random={_common.random()}&startTime={startTime}&endTime={endTime}&pageIndex={pageIndex}&pageSize={pageSize}";

            // encrype param
            string data = _common.encrypt(parameter, desKey, null, NULL_IV);
            string sign = _common.base64edMd5($"{data}{md5Key}");
            string param = $"data={HttpUtility.UrlEncode(data)}&sign={HttpUtility.UrlEncode(sign)}&propertyId={propertyId}";

            // log
            _log2File.AddRequestLog(tpName, guid, url, parameter, param);

            // request
            var r = await DoHttpClientAsync(url, param, guid);
            if (string.IsNullOrEmpty(r))
            {
                return null;
            }
            else
            {
                var result = JsonConvert.DeserializeObject<WagerAllBetModel>(r);
                return result;
            }
        }

        public async Task<WagerAllBetModel> GetModifiedBetListByTime(string apiUrl, string desKey, string md5Key, string propertyId, string startTime, string endTime, int pageIndex, string guid)
        {
            // param
            string uri = "/betlog_modified_histories";
            string url = $"{apiUrl}{uri}";
            int pageSize = 1000;
            string parameter = $"random={_common.random()}&startTime={startTime}&endTime={endTime}&pageIndex={pageIndex}&pageSize={pageSize}";

            // encrype param
            string data = _common.encrypt(parameter, desKey, null, NULL_IV);
            string sign = _common.base64edMd5($"{data}{md5Key}");
            string param = $"data={HttpUtility.UrlEncode(data)}&sign={HttpUtility.UrlEncode(sign)}&propertyId={propertyId}";

            // log
            _log2File.AddRequestLog(tpName, guid, url, parameter, param);

            // request
            var r = await DoHttpClientAsync(url, param, guid);
            if (string.IsNullOrEmpty(r))
            {
                return null;
            }
            else
            {
                var result = JsonConvert.DeserializeObject<WagerAllBetModel>(r);
                return result;
            }
        }

        public async Task<RewardAllBetModel> GetRewardByTime(string apiUrl, string desKey, string md5Key, string propertyId, string startTime, string endTime, int pageIndex, string guid)
        {
            // param
            string uri = "/query_event_records";
            string url = $"{apiUrl}{uri}";
            int pageSize = 1000;
            string parameter = $"random={_common.random()}&startTime={startTime}&endTime={endTime}&pageIndex={pageIndex}&pageSize={pageSize}";

            // encrype param
            string data = _common.encrypt(parameter, desKey, null, NULL_IV);
            string sign = _common.base64edMd5($"{data}{md5Key}");
            string param = $"data={HttpUtility.UrlEncode(data)}&sign={HttpUtility.UrlEncode(sign)}&propertyId={propertyId}";

            // log
            _log2File.AddRequestLog(tpName, guid, url, parameter, param);

            // request
            var r = await DoHttpClientAsync(url, param, guid);
            if (string.IsNullOrEmpty(r))
            {
                return null;
            }
            else
            {
                var result = JsonConvert.DeserializeObject<RewardAllBetModel>(r);
                return result;
            }
        }

        private async Task<string> DoHttpClientAsync(string url, string param, string guid = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await _httpHelper.GetClient().SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                LogContext.PushProperty("Tp", tpName);
                _logger.LogError($"StatusCode : {response.StatusCode}");
                _logger.LogError($"ReasonPhrase : {response.ReasonPhrase}");
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    return "{\"error_code\":\"SYSTEM_MATAINING\",\"message\":\"System under maintenance\"}";
                }

                _log2File.AddResponseLog(tpName, guid, url, param, response);
                return null;
            }
        }

        public async Task<string> doReward(object parameter, string sha1key, string propertyId)
        {
            // param
            string url = $"{_seamlessUrl}/Allbet";
            string uri = "/transfer";

            // request
            return await DoSeamlessHttpClientAsync(url, uri, parameter, sha1key, propertyId);
        }

        public async Task<string> DoSeamlessHttpClientAsync(string url, string uri, object param, string sha1key, string propertyId)
        {
            try
            {
                var json = JsonConvert.SerializeObject(param);
                var date = DateTime.UtcNow.AddHours(8).ToString("ddd, dd MMM yyy HH:mm:ss 'GMT'", CultureInfo.CreateSpecificCulture("en-US"));
                var md5 = "";
                var str = $"POST\n{md5}\napplication/json; charset=utf-8\n{date}\n{uri}";
                var sign = _common.Sign(str, sha1key);
                var seamlessApi = $"{url}{uri}";
                HttpRequestMessage request;
                request = new HttpRequestMessage(HttpMethod.Post, seamlessApi);

                request.Content = new StringContent(json, Encoding.UTF8, "application/json");//, Encoding.UTF8, "application/json"
                request.Headers.Add("Authorization", $"AB {propertyId}:{sign}");
                request.Headers.Add("Date", date);

                var response = await _httpHelper.GetClient().SendAsync(request);
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
