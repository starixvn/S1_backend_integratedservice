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
    public class ApiEbetBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiEbetBLL> _logger;
        private readonly string _tpName = Tp.EBet.ToString();

        public ApiEbetBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiEbetBLL>>();
        }

        public async Task<CurrencyStats_Ebet> GetBetSummaryByTime(dynamic setting, string currency, string startTime, string endTime)
        {
            var apiUrl = setting[$"ApiUrl_{currency}"];
            var channelId = setting[$"ChannelId_{currency}"];
            var privateKey = setting[$"PrivateKey_{currency}"];
            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            
            // param
            var url = $"{apiUrl}/api/userbetsummary";
            var param = new
            {
                timestamp = timestamp,
                channelId = channelId,
                signature = _common.RSA(timestamp.ToString(), privateKey),
                startTimeStr = startTime,
                endTimeStr = endTime,
                judgeTime = 0,// 0: 派彩时间、1: 投注时间
                currency = currency,
            };

            var sParam = JsonConvert.SerializeObject(param);

            // request
            var r = await DoHttpClientAsync(url, sParam, HttpMethod.Post);
            var result = JsonConvert.DeserializeObject<CurrencyStats_Ebet>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param, HttpMethod method)
        {
            HttpRequestMessage request;
            request = new HttpRequestMessage(method, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/json");

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, param, result);
            }

            return result;
        }
    }
}
