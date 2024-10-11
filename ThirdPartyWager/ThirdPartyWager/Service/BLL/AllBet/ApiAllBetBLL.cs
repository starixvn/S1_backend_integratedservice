using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;

using Newtonsoft.Json;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiAllBetBLL
    {
        private readonly string _seamlessUrl;
        private readonly HttpHelper _httpHelper;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiAllBetBLL> _logger;
        private readonly string _tpName = Tp.Allbet.ToString();
        private readonly IConfiguration _configuration;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Dictionary<int, AllBetConfig> _siteConfigDic;

        public ApiAllBetBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiAllBetBLL>>();
            _configuration = provider.GetRequiredService<IConfiguration>();
            _seamlessUrl = provider.GetRequiredService<ConfigBLL>().GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").SeamlessService;
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<AllBetConfig>(Tp.Allbet.ToString());
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        public WagerAllBetModel<BetAllBet> GetBetListByTime(string agent, string startDateTime, string endDateTime, int pageNum, int siteId)
        {
            var _siteAllBetConfig = _siteConfigDic[siteId];
            // param
            var apiUrl = _siteAllBetConfig.ApiUrl;
            var path = "PagingQueryBetRecords";
            var param = new
            {
                agent,
                startDateTime,
                endDateTime,
                pageNum,
                pageSize = 1000
            };
            var parameters = JsonConvert.SerializeObject(param);
            // log
            _logger.LogInformation(parameters);
            var header = GetAuthData(path, parameters, _siteAllBetConfig, out var contentType);
            // request
            var r = HttpPostClient($"{apiUrl}/{path}", parameters, contentType, header);
            if (string.IsNullOrEmpty(r))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<WagerAllBetModel<BetAllBet>>(r);
        }

        public SettleCheckAllBetModel GetBetByBetNum(string betNum, string client)
        {
            TpCustInfo tpCustInfo = _thirdPartyBLL.GetTpCustInfoWithCache((int)Tp.Allbet, client);
            var _siteAllBetConfig = _siteConfigDic[tpCustInfo.SiteId];
            // param
            var apiUrl = _siteAllBetConfig.ApiUrl;
            var path = "QueryBetRecordByBetNum";
            var param = new
            {
                betNum
            };
            var parameters = JsonConvert.SerializeObject(param);
            // log
            _logger.LogInformation(parameters);
            var header = GetAuthData(path, parameters, _siteAllBetConfig, out var contentType);
            // request
            var r = HttpPostClient($"{apiUrl}/{path}", parameters, contentType, header);
            if (string.IsNullOrEmpty(r))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<SettleCheckAllBetModel>(r);
        }

        public WagerAllBetModel<BetAllBet> GetModifiedBetListByTime(string agent, string startDateTime, string endDateTime, int siteId)
        {
            var _siteAllBetConfig = _siteConfigDic[siteId];
            // param
            var apiUrl = _siteAllBetConfig.ApiUrl;
            var path = "QueryModifiedBetRecords";
            var param = new
            {
                agent,
                startDateTime,
                endDateTime
            };
            var parameters = JsonConvert.SerializeObject(param);
            // log
            _logger.LogInformation(parameters);
            var header = GetAuthData(path, parameters, _siteAllBetConfig, out var contentType);
            // request
            var r = HttpPostClient($"{apiUrl}/{path}", parameters, contentType, header);
            if (string.IsNullOrEmpty(r))
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<WagerAllBetModel<BetAllBet>>(r);
            return result;
        }

        public WagerAllBetModel<RewardAllBet> GetRewardByTime(string agent, string startDateTime, string endDateTime, int pageNum, int siteId)
        {
            var _siteAllBetConfig = _siteConfigDic[siteId];
            var apiUrl = _siteAllBetConfig.ApiUrl;
            var path = "PagingQueryEventRecords";
            var param = new
            {
                agent,
                startDateTime,
                endDateTime,
                pageNum,
                pageSize = 1000
            };
            var parameters = JsonConvert.SerializeObject(param);
            // log
            _logger.LogInformation(parameters);
            var header = GetAuthData(path, parameters, _siteAllBetConfig, out var contentType);
            // request
            var r = HttpPostClient($"{apiUrl}/{path}", parameters, contentType, header);
            if (string.IsNullOrEmpty(r))
            {
                return null;
            }
            else
            {
                var result = JsonConvert.DeserializeObject<WagerAllBetModel<RewardAllBet>>(r);
                return result;
            }
        }

        public CurrencyStats_AllBet GetBetSummaryByTime(string agent, string startTime, int siteId)
        {
            var _siteAllBetConfig = _siteConfigDic[siteId];
            // param
            var apiUrl = _siteAllBetConfig.ApiUrl;
            var path = "QueryOneDaySumHistories";
            var param = new
            {
                agent,
                date = startTime,
                status = 111,
            };

            var parameters = JsonConvert.SerializeObject(param);
            // log
            _logger.LogInformation(parameters);
            var header = GetAuthData(path, parameters, _siteAllBetConfig, out var contentType);
            // request
            var r = HttpPostClient($"{apiUrl}/{path}", parameters, contentType, header);
            if (string.IsNullOrEmpty(r))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<CurrencyStats_AllBet>(r);
        }

        public async Task<string> CallEventSettleApi(RewardAllBet reward)
        {
            try
            {
                var url = $"{_seamlessUrl}Allbet/transfer";

                var rewardAllBetRequest = new RewardAllBetRequest()
                {
                    tranId = reward.eventRecordNum,
                    type = 40,
                    player = reward.player,
                    amount = reward.amount,
                    isRetry = true,
                    currency = reward.currency,
                    details = new List<RewardAllBet>() { reward },
                };
                var param = JsonConvert.SerializeObject(rewardAllBetRequest);

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(param, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("Internal", $"Wager");

                var response = await _httpHelper.GetClient().SendAsync(request);
                var result = response.Content.ReadAsStringAsync().Result;
                if (!response.IsSuccessStatusCode)
                {
                    _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                    _log2File.InsertResponseLog(_tpName, url, param, result);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return ex.Message;
            }
        }

        private Dictionary<string, string> GetAuthData(string path, string parameters, AllBetConfig config, out string contentType)
        {
            var md5 = Base64Md5(parameters);
            contentType = "application/json";
            var date = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm:ss z", new CultureInfo("en-US"));
            var signString = $"POST\n{md5}\n{contentType}\n{date}\n/{path}";
            var signature = Sign(signString, config.AllbetKey);
            return new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"AB {config.OperatorId}:{signature}" },
                { "Content-MD5", md5 },
                { "Date", date }
            };
        }

        private string Sign(string encryptText, string encryptKey)
        {
            using var hmacsha1 = new HMACSHA1(Convert.FromBase64String(encryptKey));
            var hash = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(encryptText));

            return Convert.ToBase64String(hash);
        }

        private string Base64Md5(string data)
        {
            return Convert.ToBase64String(Md5(Encoding.UTF8.GetBytes(data)));
        }

        private byte[] Md5(byte[] data)
        {
            using var md5Crp = new MD5CryptoServiceProvider();
            return md5Crp.ComputeHash(data);
        }

        protected string HttpPostClient(string url, string parameters, string contentType, Dictionary<string, string> headers = null)
        {
            var parametersLog = parameters;
            if (headers != null)
            {
                parametersLog = $"{parametersLog}, Header={JsonConvert.SerializeObject(headers)}";
            }
            _logger.LogInformation($"url:{url} parametersLog:{parametersLog}");

            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            }

            using var client = _httpHelper.GetClientWithHandler(CreateHandler());
            try
            {
                var httpRequestMessage = new HttpRequestMessage
                {
                    Content = new StringContent(parameters),
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url)
                };
                httpRequestMessage.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue(contentType);
                if (headers != null)
                {
                    foreach (var (key, value) in headers)
                    {
                        if (string.Compare(key, "Accept", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(value));
                        }
                        else if (key.Contains("-"))
                        {
                            httpRequestMessage.Content.Headers.TryAddWithoutValidation(key, value);
                        }
                        else
                        {
                            httpRequestMessage.Headers.TryAddWithoutValidation(key, value);
                        }
                    }
                }

                var response = client.SendAsync(httpRequestMessage).Result;
                response.EnsureSuccessStatusCode();

                var sb = response.Content.ReadAsStringAsync().Result;
                _logger.LogInformation($"url:{url} | parametersLog:{parametersLog} | Response:{sb}");
                return sb;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"url:{url} parametersLog:{parametersLog} ex:{e.Message}");
                return null;
            }
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
