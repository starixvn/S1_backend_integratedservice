using System.Net;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.MG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var apiUrl = GetInputValue(inputs, "ApiUrl");
            var strAgents = GetInputValue(inputs, "Agent");
            var mgAgents = JsonConvert.DeserializeObject<List<MGAgents>>(strAgents);
            var agent = mgAgents.SingleOrDefault(a => a.Currency == tpCustInfo.TpCurrencyId);

            var token = GetToken(inputs, tpCustInfo.TpCurrencyId);
            try
            {
                var header = new Dictionary<string, string>()
                {
                    {"Authorization", $"Bearer {token}"}
                };
                var parameters = $"playerId={tpCustInfo.TpUserName}";
                HttpPost($"{apiUrl}/agents/{agent.AgentCode}/players",
                          parameters,
                          "application/x-www-form-urlencoded",
                          header);

                return new ApiResult
                {
                    ErrorCode = ErrorCode.Success
                };
            }
            catch (WebException e)
            {
                var (code, message) = GetMgError(e.Response);
                if (string.IsNullOrEmpty(message))
                {
                    message = e.Message;
                }
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    Message = message,
                    RawErrorCode = code,
                    RawMessage = message
                };
            }
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var apiUrl = GetInputValue(inputs, "ApiUrl");
            var strAgents = GetInputValue(inputs, "Agent");
            var gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            var homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            var mgAgents = JsonConvert.DeserializeObject<List<MGAgents>>(strAgents);
            var agent = mgAgents.SingleOrDefault(a => a.Currency == tpCustInfo.TpCurrencyId);

            var token = GetToken(inputs, tpCustInfo.TpCurrencyId);

            try
            {
                var tpGameType = (int)TpGameType.MG_RNG;
                if (gameId == "0")
                {
                    gameId = "SMG_titaniumLiveGames_Baccarat";
                    tpGameType = (int)TpGameType.MG_LiveCasino;
                }

                TpCustInfoProvider.SetCustGotoGame(tpCustInfo.CustId, tpCustInfo.TpId, tpGameType);

                var header = new Dictionary<string, string>()
                {
                    {"Authorization", $"Bearer {token}"}
                };
                var platform = appType == AppType.Desktop ? "Desktop" : "Mobile";
                var parameters = $"contentType=Game&contentCode={gameId}&platform={platform}&homeUrl={homeUrl}&langCode={TpLangId}";
                var result = HttpPost($"{apiUrl}/agents/{agent.AgentCode}/players/{tpCustInfo.TpUserName}/sessions",
                                            parameters,
                                            "application/x-www-form-urlencoded",
                                            header);
                var con = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Success,
                    Message = $"{con["url"]}"
                };
            }
            catch (WebException e)
            {
                var (code, message) = GetMgError(e.Response);
                if (string.IsNullOrEmpty(message))
                {
                    message = e.Message;
                }
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    Message = message,
                    RawErrorCode = code,
                    RawMessage = message
                };
            }
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID,
            string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID,
            string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            try
            {
                var apiUrl = GetInputValue(inputs, "ApiUrl");
                var strAgents = GetInputValue(inputs, "Agent");
                var mgAgents = JsonConvert.DeserializeObject<List<MGAgents>>(strAgents);
                var agent = mgAgents.SingleOrDefault(a => a.Currency == tpCustInfo.TpCurrencyId);
                var token = GetToken(inputs, tpCustInfo.TpCurrencyId);

                var url = $"{apiUrl}/agents/{agent.AgentCode}/players/{tpCustInfo.TpUserName}/betVisualizers";
                var sParam = $"betId={getBetDetail.TransId}&utcOffset=0&langCode=en-US";
                var header = new Dictionary<string, string>()
                {
                     { "Authorization", $"Bearer {token}"}
                };

                var json = HttpPost(url, sParam, "application/x-www-form-urlencoded", header);
                var result = FromJson<DetailResult>(json);

                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    DetailUrl = result.url,
                    TpCurrency = new TpCurrency()
                    {
                        ExchangeRate = tpCustInfo.ExchangeRate,
                        TpCurrencyId = tpCustInfo.TpCurrencyId,
                    },
                };
            }
            catch (WebException e)
            {
                var (code, message) = GetMgError(e.Response);
                if (string.IsNullOrEmpty(message))
                {
                    message = e.Message;
                }
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {code} | {message}");
            }
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var YYYYMM = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime.GetPartitionKey();
            var detail = TpBetRecordProvider.DbQuery("Service_Bettrans_MG_Detail_Get",
                new { betRecordInfo.TransId, GameType = tpGameType.Type, YYYYMM }, DataBase.Main);

            return tpGameType.CategoryId switch
            {
                TpCategory.Casino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = detail.GetFirstStringValue("contentCode"),
                    Game = detail.GetFirstStringValue("contentCode"),
                    RefNo = detail.GetFirstStringValue("betId"),
                    Title = tpGameType.TypeName,
                    MatchID = detail.GetFirstStringValue("roundId"),
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = string.Empty,
                    DeviceType = GetDeviceType(detail.GetFirstStringValue("deviceType")),
                    IP = string.Empty
                },
                TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = detail.GetFirstStringValue("contentCode"),
                    Game = detail.GetFirstStringValue("contentCode"),
                    RefNo = detail.GetFirstStringValue("betId"),
                    Title = tpGameType.TypeName,
                    MatchID = detail.GetFirstStringValue("roundId"),
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = string.Empty,
                    DeviceType = GetDeviceType(detail.GetFirstStringValue("deviceType")),
                    IP = string.Empty
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        public class DetailResult
        {
            public string url { get; set; }
        }

        private DeviceType GetDeviceType(string device)
        {
            if (device.Equals("Desktop", StringComparison.OrdinalIgnoreCase))
            {
                return DeviceType.Desktop;
            }

            if (device.Equals("Mobile", StringComparison.OrdinalIgnoreCase))
            {
                return DeviceType.Mobile;
            }

            return DeviceType.None;
        }

        private string GetInputValue(Dictionary<string, string> inputs, string key)
        {
            if (inputs.ContainsKey(key) && !string.IsNullOrEmpty(inputs[key]))
            {
                return inputs[key];
            }
            throw new Exception();
        }

        private string GetToken(Dictionary<string, string> inputs, string tpCurrencyId)
        {
            var tokenUrl = GetInputValue(inputs, "TokenUrl");
            var strAgents = GetInputValue(inputs, "Agent");
            var mgAgents = JsonConvert.DeserializeObject<List<MGAgents>>(strAgents);
            var agent = mgAgents.SingleOrDefault(a => a.Currency == tpCurrencyId);

            var parameters =
                $"client_id={agent.AgentCode}&client_secret={agent.SecretKey}&grant_type=client_credentials";
            var tokenResult = HttpPost($"{tokenUrl}/connect/token", parameters, "application/x-www-form-urlencoded");

            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult);
            return dict["access_token"];
        }

        private (string, string) GetMgError(WebResponse webResponse)
        {
            using var response = webResponse;
            var httpResponse = (HttpWebResponse)response;
            if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
            {
                using var data = response.GetResponseStream();
                if (data != null)
                {
                    using (var reader = new StreamReader(data))
                    {
                        var text = reader.ReadToEnd();
                        var error = JsonConvert.DeserializeObject<MGError>(text);
                        return (error.error.code, error.error.message);
                    }
                }
            }

            return (string.Empty, string.Empty);
        }

        private class MGAgents
        {
            public string Currency { get; set; }
            public string MasterAgent { get; set; }
            public string AgentCode { get; set; }
            public string SecretKey { get; set; }
        }
        public class Error
        {
            public string code { get; set; }
            public string message { get; set; }
        }

        public class MGError
        {
            public Error error { get; set; }
        }


    }
}