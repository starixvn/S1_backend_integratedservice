using System.Text;

using Newtonsoft.Json;

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
        private readonly Dictionary<int, EBetConfig> _siteEBetConfig;
        public ApiEbetBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiEbetBLL>>();
            _siteEBetConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<EBetConfig>(_tpName);
        }

        public async Task<CurrencyStats_Ebet> GetBetSummaryByTime(int siteId ,string currency, string startTime, string endTime)
        {
            var siteConfig = _siteEBetConfig[siteId];
            var currencyConfig = siteConfig.Currency.FirstOrDefault(x => x.CurrencyId == currency);
            var apiUrl = siteConfig.ApiUrl;
            var channelId = currencyConfig.ChannelId;
            var privateKey = currencyConfig.PrivateKey;
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
            _log2File.InsertRequestLog(_tpName, url, param, "1");

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

        #region Model
        
        public class EBetConfig
        {
            public string ApiUrl { get; set; }
            public List<Currency> Currency { get; set; }
        }
        
        public class Currency
        {
            public string CurrencyId { get; set; }
            public string GameUrl { get; set; }
            public int ChannelId { get; set; }
            public string PublicKey { get; set; }
            public string PrivateKey { get; set; }
        }

        #endregion 
    }
}
