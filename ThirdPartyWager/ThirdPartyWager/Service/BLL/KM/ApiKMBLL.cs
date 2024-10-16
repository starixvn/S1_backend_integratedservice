using System.Net;
using System.Web;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiKMBLL
    {
        private readonly IConfiguration _configuration;
        private readonly HttpHelper _httpHelper;
        private readonly ILogger<ApiKMBLL> _logger;
        private readonly string _tpName = Tp.KM.ToString();
        private readonly Log2File _log2File;
        private readonly Dictionary<int, KMConfig> _siteConfigDic;


        public ApiKMBLL(IServiceProvider provider, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = provider.GetRequiredService<ILogger<ApiKMBLL>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _log2File = provider.GetRequiredService<Log2File>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<KMConfig>(Tp.KM.ToString());
        }
        public async Task<List<WagerKMModel>> QueryBetHistoryAsync(string startTime, string endTime, int siteId)
        {
            var historyBet = string.Empty;
            var _siteConfig = _siteConfigDic[siteId];
            try
            {
                var apiUrl = _siteConfig.ApiUrl;
                var clientId = _siteConfig.X_QM_ClientId;
                var clientSecret = _siteConfig.X_QM_ClientSecret;

                startTime = $"{DateTime.Parse(startTime).ToString("s")}+08:00";
                endTime = $"{DateTime.Parse(endTime).ToString("s")}+08:00";

                var encodingQueryStr = $"startdate={HttpUtility.UrlEncode(startTime)}&" +
                                             $"enddate={HttpUtility.UrlEncode(endTime)}&issettled=true";
                historyBet = $"{apiUrl}/api/history/bets?{encodingQueryStr}";

                var request = new HttpRequestMessage(HttpMethod.Get, historyBet);
                request.Headers.Add("X-QM-ClientId", clientId);
                request.Headers.Add("X-QM-ClientSecret", clientSecret);
                request.Headers.Add("X-QM-Accept", "json");
                request.Headers.Add("Accept-Encoding", "gzip");

                _log2File.InsertRequestLog(_tpName, historyBet, JsonConvert.SerializeObject(request.Headers), "1");

                var client = _httpHelper.GetClientWithHandler(CreateHandler());
                var response = await client.SendAsync(request);

                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"{historyBet} query history bets error:{content}");
                    return null;
                }
                var kmWagers = JsonConvert.DeserializeObject<List<WagerKMModel>>(content);
                return kmWagers;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{historyBet} query history bets failed:{e.Message}");
                return null;
            }

        }

        public async Task<List<CurrencyStats_KM>> GetBetSummaryByTime(string currency, string startTime, string endTime, int siteId)
        {
            try
            {
                var _siteConfig = _siteConfigDic[siteId];
                var apiUrl = _siteConfig.ApiUrl;
                var clientId = _siteConfig.X_QM_ClientId;
                var clientSecret = _siteConfig.X_QM_ClientSecret;

                startTime = $"{DateTime.Parse(startTime).ToString("s")}+00:00";
                endTime = $"{DateTime.Parse(endTime).ToString("s")}+00:00";

                var uri = "/api/history/bets/total?";
                var param = $"startdate={HttpUtility.UrlEncode(startTime)}&enddate={HttpUtility.UrlEncode(endTime)}&cur={currency}";
                var url = $"{apiUrl}{uri}{param}";

                _log2File.InsertRequestLog(_tpName, apiUrl + uri, param, "1");

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-QM-ClientId", clientId);
                request.Headers.Add("X-QM-ClientSecret", clientSecret);
                request.Headers.Add("X-QM-Accept", "json");
                request.Headers.Add("Accept-Encoding", "gzip");

                var client = _httpHelper.GetClientWithHandler(CreateHandler());
                var response = await client.SendAsync(request);

                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                    _log2File.InsertResponseLog(_tpName, url, param, content);
                    return null;
                }

                var result = JsonConvert.DeserializeObject<List<CurrencyStats_KM>>(content);
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"query history bets total failed:{e.Message}");
                return null;
            }
        }

        private HttpClientHandler CreateHandler()
        {
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip,
            };
            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("ProxyHost")))
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(_configuration.GetValue<string>("ProxyHost"),
                    _configuration.GetValue<int>("ProxyPort"));
            }

            return handler;
        }
    }
}