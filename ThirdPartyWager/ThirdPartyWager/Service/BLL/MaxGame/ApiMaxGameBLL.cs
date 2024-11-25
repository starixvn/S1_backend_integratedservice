using System.Text;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiMaxGameBLL
    {
        // 共用SabaPlay所有遊戲: MaxGame、SabaPlay、Live22、UUSlots、FastSpin
        private readonly string _seamlessUrl;

        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiMaxGameBLL> _logger;
        private readonly string _commonTpName = Tp.MaxGame.ToString();

        private const string _formatOfTime = "yyyy-MM-dd'T'HH:mm:ss.fffffffK";
        private const int _utc = -4;

        public ApiMaxGameBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiMaxGameBLL>>();
            _seamlessUrl = provider.GetRequiredService<ConfigBLL>().GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").SeamlessService;
        }

        public async Task<TicketDataMaxGame> CheckTicketStatus(MaxGameConfig setting, string tpName, CheckTicketStatusPayloadMaxGame parameters)
        {
            // param
            var uri = "/api/CommonWallet/BetResultCheck";
            var url = $"{setting.ApiUrl}{uri}";
            var param = new
            {
                Timestamp = GetCurrentTimeFormat(),
                Seq = Guid.NewGuid().ToString(),
                Payload = new
                {
                    GameType = setting.GameType,
                    parameters.OperatorCustId,
                    parameters.TicketNo,
                    parameters.TransDate,
                    AgentInfo = new
                    {
                        parameters.AgentId,
                        parameters.AgentKey,
                        parameters.AgentCode,
                        parameters.ExtraInfo
                    }
                }
            };
            var postdata = JsonConvert.SerializeObject(param, Formatting.None);
            // request
            var r = await DoHttpClientWithProxyAsync(setting, tpName, url, postdata);
            var result = JsonConvert.DeserializeObject<TicketDataMaxGame>(r);
            var additionalLogInfo = new Dictionary<string, object>() {
                {"Request", JsonConvert.SerializeObject(param, Formatting.Indented) },
                {"Response",JsonConvert.SerializeObject(result, Formatting.Indented) }
            };
            using (_logger.BeginScope(additionalLogInfo))
            {
                _logger.LogInformation($"SABA PLUS API CALL, uri: {uri}");
            }
            return result;
        }

        public async Task<DailySummaryData_MaxGame> GetDailySummaryDataAsync(MaxGameConfig setting, string tpName, PayloadRequestDailySummaryData_MaxGame parameters)
        {
            var uri = "/api/CommonWallet/DailyBetCrossCheck";
            var url = $"{setting.ApiUrl}{uri}";

            var param = new
            {
                Timestamp = GetCurrentTimeFormat(),
                Seq = Guid.NewGuid().ToString(),
                Payload = new
                {
                    setting.GameType,
                    parameters.CheckDate
                }
            };

            var postdata = JsonConvert.SerializeObject(param, Formatting.None);
            var r = await DoHttpClientWithProxyAsync(setting, tpName, url, postdata);
            var result = JsonConvert.DeserializeObject<DailySummaryData_MaxGame>(r);

            var additionalLogInfo = new Dictionary<string, object>() {
                {"Request", JsonConvert.SerializeObject(param, Formatting.Indented) },
                {"Response",JsonConvert.SerializeObject(result, Formatting.Indented) }
            };
            using (_logger.BeginScope(additionalLogInfo))
            {
                _logger.LogInformation($"SABA PLUS API CALL, uri: {uri}");
            }
            return result;
        }

        public async Task<string> CallSeamlessApi(MaxGameConfig setting, string tpName, string siteName, BettransData_MaxGame bettransDataMaxGame, TicketDataMaxGame ticketDataMaxGame)
        {
            // param
            var url = $"{_seamlessUrl}{_commonTpName}/{siteName}";
            var uri = "";
            var tag = true;
            var param = string.Empty;
            var type = ticketDataMaxGame.Payload.Status;
            // 加上_前置區分內部調用
            var seq = $"_{ticketDataMaxGame.Seq}";
            switch (type)
            {
                case TypeOfMaxGame.Win:
                case TypeOfMaxGame.Lose:
                case TypeOfMaxGame.Draw:
                case TypeOfMaxGame.Refund: // 無實際案例，待有案例後測試
                    uri = "/EndGame";
                    var resultId = "";
                    if (ticketDataMaxGame.Payload.OtherBetInfo != null)
                    {
                        resultId = ticketDataMaxGame.Payload.OtherBetInfo.GetValueOrDefault("ResultId");
                    }

                    var endGameData = new
                    {
                        Seq = seq,
                        TimeStamp = GetCurrentTimeFormat(),
                        Payload = new
                        {
                            OperatorCustId = bettransDataMaxGame.OperatorCustId,
                            GameType = bettransDataMaxGame.GameType,
                            BetDataSet = new List<BetDataSetEndGameReq_MaxGame>
                            {
                                new BetDataSetEndGameReq_MaxGame
                                {
                                    TicketNo = bettransDataMaxGame.TicketNo,
                                    GameId = bettransDataMaxGame.GameId,
                                    GameRoundId = bettransDataMaxGame.GameRoundId,
                                    TransactionId = bettransDataMaxGame.TransactionId,
                                    ActualStake = ticketDataMaxGame.Payload.ActualStake,
                                    NetStake = ticketDataMaxGame.Payload.NetStake,
                                    Payout =  ticketDataMaxGame.Payload.Payout,
                                    Odds = ticketDataMaxGame.Payload.Odds,
                                    GameResult = ticketDataMaxGame.Payload.GameResult,
                                    SettleDate =  (DateTime)ticketDataMaxGame.Payload.SettleDate,
                                    ResultId = resultId,
                                    OtherBetInfo = ticketDataMaxGame.Payload.OtherBetInfo
                                }
                            }
                        }
                    };
                    param = JsonConvert.SerializeObject(endGameData);
                    break;

                case TypeOfMaxGame.Void:
                case TypeOfMaxGame.Reject:
                    uri = "/VoidGame";
                    var voidGameData = new
                    {
                        Seq = seq,
                        TimeStamp = GetCurrentTimeFormat(),
                        Payload = new
                        {
                            OperatorCustId = bettransDataMaxGame.OperatorCustId,
                            GameType = bettransDataMaxGame.GameType,
                            BetDataSet = new List<BetDataSetVoidGameReq_MaxGame>
                            {
                                new BetDataSetVoidGameReq_MaxGame
                                {
                                    TicketNo = bettransDataMaxGame.TicketNo,
                                    GameId = bettransDataMaxGame.GameId,
                                    GameRoundId = bettransDataMaxGame.GameRoundId,
                                    TransactionId = bettransDataMaxGame.TransactionId,
                                    Status = (int)TypeOfMaxGame.Reject,
                                }
                            }
                        }
                    };
                    param = JsonConvert.SerializeObject(voidGameData);
                    break;

                default:
                    /*
                     * TypeOfMaxGame.Running 不處理
                     * BetResultCheck 問到的狀態是 Running.
                     * 這時候會觸發特定機制, 如果是結算的狀態, 遊戲端就會配合我方主動發送EndGame給你們
                     * 或者是如果注單是取消, 就會發送VoidGame給你們
                     */
                    tag = false;
                    _logger.LogWarning($"{type} not found");
                    break;
            }

            if (!tag || param == string.Empty || uri == "")
            {
                return null;
            }

            // request
            var seamlessApi = $"{url}{uri}";
            return await DoHttpClientAsync(setting, tpName, seamlessApi, param);
        }

        private async Task<string> DoHttpClientAsync(HttpClient httpClient, MaxGameConfig setting, string tpName, string url, string param)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/json");
            request.Headers.Add("operator-pwd", setting.OperatorPwd);
            request.Headers.Add("operator-sign", Encrypt(param, setting));
            _log2File.InsertRequestLog(tpName, url, $"{param}\r\n{JsonConvert.SerializeObject(request.Headers)}", "1");

            var response = await httpClient.SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(tpName, url, param, result);
                return result;
            }
            else
            {
                _log2File.InsertResponseLog(tpName, url, param, result, false);
            }

            return result;
        }

        private async Task<string> DoHttpClientAsync(MaxGameConfig setting, string tpName, string url, string param)
        {
            var normalHttpClient = _httpHelper.GetClient();
            return await DoHttpClientAsync(normalHttpClient, setting, tpName, url, param);
        }

        private async Task<string> DoHttpClientWithProxyAsync(MaxGameConfig setting, string tpName, string url, string param)
        {
            var httpClientWithProxy = _httpHelper.GetClientWithProxy();
            return await DoHttpClientAsync(httpClientWithProxy, setting, tpName, url, param);
        }

        private string Encrypt(string req, MaxGameConfig config)
        {
            var s = req + config.OperatorKey;
            var sign = _common.ToMD5(s);
            return sign;
        }

        private static string GetCurrentTimeFormat()
        {
            var currentAt = new DateTimeOffset(DateTime.UtcNow);
            return currentAt.ToOffset(new TimeSpan(_utc, 0, 0)).ToString(_formatOfTime);
        }
    }
}