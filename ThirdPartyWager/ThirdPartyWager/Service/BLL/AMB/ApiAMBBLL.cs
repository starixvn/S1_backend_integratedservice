using System.Text;

using Newtonsoft.Json;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiAMBBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiAMBBLL> _logger;
        private readonly string _tpName = Tp.AMB.ToString();
        private readonly AMBConfig _siteAMBConfig;

        public ApiAMBBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiAMBBLL>>();
            _siteAMBConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<AMBConfig>(_tpName);
        }

        public async Task<Wager_AMB> GetBetListByTime(string startTime, string endTime,string currency,int gameType)
        {
            var settingByGameType = GetTpListByGameType(currency, gameType);
            var apiUrl = settingByGameType["ApiUrl"];
            var agent = settingByGameType["Agent"];
            var secretKey = settingByGameType["SecretKey"];
            var headerKey = settingByGameType["HeaderKey"];

            // param
            var url = $"{apiUrl}/seamless/report/time/ten-minutes";
            var param = new
            {
                agent = agent,
                startDate = startTime,
                endDate = endTime,
                limit = 1000,
            };
            
            var sParam = JsonConvert.SerializeObject(param);

            // request
            var r = await DoHttpClientAsync(url, sParam, secretKey, headerKey, HttpMethod.Post);
            var result = JsonConvert.DeserializeObject<Wager_AMB>(r);
            
            return result;
        }
        
        public async Task<CurrencyStats_AMB> GetBetSummaryByTime(string currency, int gameType, string startTime, string endTime)
        {
            var settingByGameType = GetTpListByGameType(currency, gameType);
            var apiUrl = settingByGameType["ApiUrl"];
            var agent = settingByGameType["Agent"];
            var secretKey = settingByGameType["SecretKey"];
            var headerKey = settingByGameType["HeaderKey"];

            // param
            var url = $"{apiUrl}/seamless/report/date";
            var param = new
            {
                agent = agent,
                startDate = startTime,
                endDate = endTime,
            };

            var sParam = JsonConvert.SerializeObject(param);

            // request
            var r = await DoHttpClientAsync(url, sParam, secretKey, headerKey, HttpMethod.Post);
            var result = JsonConvert.DeserializeObject<CurrencyStats_AMB>(r);
            return result;
        }

        private Dictionary<string, string> GetTpListByGameType(string currency, int gameType)
        {
            var result = new Dictionary<string, string>();
            
            switch (gameType)
            {
                case (int)GameType.AMB_Poker:
                    result = new Dictionary<string, string>
                    {
                        { "ApiUrl", _siteAMBConfig.PokerApiUrl },
                        { "Agent", _siteAMBConfig.PokerAgent },
                        { "SecretKey", _siteAMBConfig.PokerSecretKey },
                        { "HeaderKey", "x-amb-signature" }
                    };
                    break;
                case (int)GameType.AMB_RNG:
                    var slotCurrencyConfig = _siteAMBConfig.SlotCurrencyAgent.FirstOrDefault(x => x.CurrencyId == currency);
                    result = new Dictionary<string, string>
                    {
                        { "ApiUrl", slotCurrencyConfig.SlotApiUrl },
                        { "Agent", slotCurrencyConfig.Agent },
                        { "SecretKey", slotCurrencyConfig.SecretKey },
                        { "HeaderKey", "x-ambslot-signature" } 
                    };
                    break;
            }
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param, string secretKey, string headerKey, HttpMethod method)
        {
            HttpRequestMessage request;
            request = new HttpRequestMessage(method, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/json");
            request.Headers.Add(headerKey, _common.HMACSHA512(param, secretKey));
            _log2File.InsertRequestLog(_tpName, url, $"{param}\r\n{JsonConvert.SerializeObject(request.Headers)}", "1");

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
