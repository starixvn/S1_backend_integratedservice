using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiMGBLL : IApiMGBLL
    {
        private readonly ILogger<ApiMGBLL> _logger;
        private readonly Log2File _log2File;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _client;
        private readonly string _tpName = Tp.MG.ToString();
        private readonly Dictionary<int, MGConfig> _siteConfigDic;

        public ApiMGBLL(IServiceProvider provider, IConfiguration configuration)
        {
            var httpHelper = provider.GetRequiredService<HttpHelper>();
            _configuration = configuration;
            _logger = provider.GetRequiredService<ILogger<ApiMGBLL>>();
            _log2File = provider.GetRequiredService<Log2File>();
            _client = httpHelper.GetClientWithHandler(CreateHandler());
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MGConfig>(_tpName);
        }

        public async Task<List<MGFailedTransactions>> GetFailedTransactionsAsync(string token, string currencyId, int siteId)
        {
            var config = _siteConfigDic[siteId];
            var apiUrl = config.ApiUrl;
            var currencyData = config.CurrencyData[currencyId];

            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _client.GetAsync($"{apiUrl}/agents/{currencyData.AgentCode}/failedTransactions");
                var result = await response.Content.ReadAsStringAsync();
                if (result != "[]")
                {
                    _logger.LogInformation($"{currencyData.AgentCode} get failedTransactions:{result}");
                }
                var failedTransactions = JsonConvert.DeserializeObject<List<MGFailedTransactions>>(result);
                return failedTransactions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "get mg failed transactions failed");
                return null;
            }
        }

        public async Task UpdateFailedTransactionToResolvedState(string token, string txnId, string currencyId, int siteId)
        {
            var config = _siteConfigDic[siteId];
            var apiUrl = config.ApiUrl;
            var currencyData = config.CurrencyData[currencyId];

            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _client.PatchAsync($"{apiUrl}/agents/{currencyData.AgentCode}/failedTransactions/{txnId}", null);
                _logger.LogInformation($"update mg failed transactions {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update failed transaction to resolved state failed");
            }
        }

        public async Task<string> GetToken(string tpCurrencyId, int siteId)
        {
            try
            {
                var config = _siteConfigDic[siteId];
                var tokenUrl = config.TokenUrl;
                var currencyData = config.CurrencyData[tpCurrencyId];

                var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", currencyData.AgentCode },
                    { "client_secret", currencyData.SecretKey },
                    { "grant_type", "client_credentials" }
                });
                var response = await _client.PostAsync($"{tokenUrl}/connect/token", httpContent);
                var tokenResult = await response.Content.ReadAsStringAsync();
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult);
                return dict.ContainsKey("access_token") ? dict["access_token"] : string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "get mg token failed");
                return string.Empty;
            }
        }

        public async Task<IEnumerable<MGPlayerGameAnalysis>> GetPlayerGameAnalysisAsync(string token, string currencyId, int siteId, DateTime date)
        {
            try
            {
                var config = _siteConfigDic[siteId];
                var apiUrl = config.ApiUrl;
                var currencyData = config.CurrencyData[currencyId];

                var url = $"{apiUrl}/agents/{currencyData.AgentCode}/reports/playerGameAnalysis";
                var parameters = new
                {
                    fromDate = date.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    toDate = date.AddDays(1).AddMilliseconds(-1).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    timeAggregation = "Daily",
                    utcOffset = "0"
                };
                _log2File.InsertRequestLog(_tpName, url, JsonConvert.SerializeObject(parameters), "1");
                var httpContent = new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json");
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _client.PostAsync(url, httpContent);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<IEnumerable<MGPlayerGameAnalysis>>(content);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET MG playerGameAnalysis failed");
                return null;
            }
        }

        public async Task<IEnumerable<MGFortuneRewards>> GetFortuneRewardsAsync(string token, string currencyId, int siteId, DateTime date)
        {
            try
            {
                var config = _siteConfigDic[siteId];
                var apiUrl = config.ApiUrl;
                var currencyData = config.CurrencyData[currencyId];

                var url = $"{apiUrl}/agents/{currencyData.AgentCode}/reports/fortuneRewards";
                var parameters = new
                {
                    fromDate = date.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    toDate = date.AddDays(1).AddMilliseconds(-1).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    utcOffset = "0"
                };
                _log2File.InsertRequestLog(_tpName, url, JsonConvert.SerializeObject(parameters), "1");
                var httpContent = new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json");
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _client.PostAsync(url, httpContent);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<IEnumerable<MGFortuneRewards>>(content);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET MG FortuneRewards failed");
                return null;
            }
        }

        public class MGConfig
        {
            public string ApiUrl { get; set; }
            public string TokenUrl { get; set; }
            public Dictionary<string, CurrencyDataMG> CurrencyData { get; set; }
        }

        public class CurrencyDataMG
        {
            public string MasterAgent { get; set; }
            public string AgentCode { get; set; }
            public string SecretKey { get; set; }
        }

        private HttpClientHandler CreateHandler()
        {
            var handler = new HttpClientHandler();
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