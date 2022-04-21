using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using System.Security.Cryptography;
using System.Net.Http.Headers;

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
        private readonly string _tpName = Tp.Allbet.ToString();
        private readonly IConfiguration _configuration;

        public ApiAllBetBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiAllBetBLL>>();
            _configuration = provider.GetRequiredService<IConfiguration>();
            _seamlessUrl = _configuration.GetValue<string>("TpSWserviceUrl");
        }

        public async Task<WagerAllBetModel<BetAllBet>> GetBetListByTime(IReadOnlyDictionary<string, string> setting, string agent, string startDateTime, string endDateTime, int pageNum)
        {
            // param
            var apiUrl = setting["ApiUrl"];
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
            var header = GetAuthData(setting, path, parameters, out var contentType);
            // request
            var r = HttpPostClient($"{apiUrl}/{path}", parameters, contentType, header);
            if (string.IsNullOrEmpty(r))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<WagerAllBetModel<BetAllBet>>(r);
        }

        public async Task<WagerAllBetModel<BetAllBet>> GetModifiedBetListByTime(IReadOnlyDictionary<string, string> setting, string agent, string startDateTime, string endDateTime)
        {
            // param
            var apiUrl = setting["ApiUrl"];
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
            var header = GetAuthData(setting, path, parameters, out var contentType);
            // request
            var r = HttpPostClient($"{apiUrl}/{path}", parameters, contentType, header);
            if (string.IsNullOrEmpty(r))
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<WagerAllBetModel<BetAllBet>>(r);
            return result;
        }

        public async Task<WagerAllBetModel<RewardAllBet>> GetRewardByTime(IReadOnlyDictionary<string, string> setting, string agent, string startDateTime, string endDateTime, int pageNum)
        {
            var apiUrl = setting["ApiUrl"];
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
            var header = GetAuthData(setting, path, parameters, out var contentType);
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

        public async Task<string> CallEventSettleApi(IReadOnlyDictionary<string, string> setting, RewardAllBet reward)
        {
            try
            {
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
                var url = $"{_seamlessUrl}/Allbet/transfer";
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

            

            // request
            return await DoSeamlessHttpClientAsync(url, uri, param, "", "");
        }

        private Dictionary<string, string> GetAuthData(IReadOnlyDictionary<string, string> setting, string path, string parameters, out string contentType)
        {
            var md5 = Base64Md5(parameters);
            contentType = "application/json";
            var date = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm:ss z", new CultureInfo("en-US"));
            var signString = $"POST\n{md5}\n{contentType}\n{date}\n/{path}";
            var signature = Sign(signString, setting["AllbetKey"]);
            return new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"AB {setting["OperatorId"]}:{signature}" },
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
