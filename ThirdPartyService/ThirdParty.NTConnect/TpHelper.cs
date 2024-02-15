using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.NTConnect
{
    public class TpHelper : ThirdPartyHelperBase , IThirdPartyParticular
    {
        private const string ContentType = "application/json";
        private readonly Dictionary<int, NTConnectConfig> _siteConfigDic;
        
        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<NTConnectConfig>(TpEnum.NTConnect.ToString());
        }
        
        
        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var siteConfig = _siteConfigDic[tpCustInfo.SiteId];
            var apiUrl = siteConfig.ApiUrl;
            var currencyConfig = siteConfig.CurrencyList.FirstOrDefault(x => x.CurrencyCode == tpCustInfo.TpCurrencyId);
            
            var tokenResponse = GetToken(apiUrl, currencyConfig.ClientKey, currencyConfig.SecretKey);
            if (tokenResponse.status != "success")
            {
                return ProcessErrorResponse(tokenResponse);
            }
            
            //93Connect Password Rule : password required at least 1 alphabet, 1 numeric and 1 symbol.
            //因前面產的TpPwd有機會全英文，故這邊直接強制特殊符號及數字確保不會錯誤
            var request = new Dictionary<string, string>()
            {
                { "token", tokenResponse.token },
                { "username", tpCustInfo.TpUserName },
                { "password", tpCustInfo.TpPwd + "$7"},
                { "confirm_password", tpCustInfo.TpPwd + "$7" },
                { "test_account", tpCustInfo.IsTest ? "1" : "0" }
            };
            
            var json = HttpPost(apiUrl + "player/?a=register", JsonConvert.SerializeObject(request), ContentType);
            var result = FromJson<GetRegisterOrLogoutResponse>(json);
            
            if (result.status == "success" || result.errors.Any(x => x.code == "103"))
            {
                return new ApiResult
                {
                    ErrorCode = ErrorCode.Success, 
                    Message = "Success"
                };
            }

            return result.errors.Count != 0 ? ProcessErrorResponse(result) : 
                new ApiResult
                {
                    ErrorCode = ErrorCode.Fail, 
                    Message = result.message
                };

        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            try
            {
                var siteConfig = _siteConfigDic[tpCustInfo.SiteId];
                var apiUrl = siteConfig.ApiUrl;
                var currencyConfig = siteConfig.CurrencyList.FirstOrDefault(x => x.CurrencyCode == tpCustInfo.TpCurrencyId);

                var gameId = GetValueOrDefault(inputs, "gameId", throwException: true);
                var clientIP = GetValueOrDefault(inputs, "ClientIP", "1.2.3.4");
                var fun = GetValueOrDefault(inputs, "fun", "0");

                if (fun == "1")
                {
                    HttpGet(apiUrl + "Demo/", null);
                    return new ApiResult()
                    {
                        ErrorCode = ErrorCode.Success, 
                        Message = "http://test123.com"
                    };
                }

                var tokenResponse = GetToken(apiUrl, currencyConfig.ClientKey, currencyConfig.SecretKey);
                if (tokenResponse.status != "success")
                {
                    return ProcessErrorResponse(tokenResponse);
                }
                
                var playerSessionResponse = GetPlayerSession(apiUrl, tpCustInfo.TpUserName, tpCustInfo.TpPwd, clientIP, tokenResponse.token);
                if (playerSessionResponse.status != "success")
                {
                    return ProcessErrorResponse(playerSessionResponse);
                }
                
                // 新增Token
                var token = ApplyToken(tpCustInfo,5);
                var tpToken = playerSessionResponse.session;
                UpdateTpToken(token, tpToken);

                var request = new Dictionary<string, string>
                {
                    { "token", tokenResponse.token },
                    { "ip", clientIP },
                    { "session", playerSessionResponse.session },
                    { "username", tpCustInfo.TpUserName },
                    { "game", gameId },
                    { "lang_id", TpLangId },
                    { "seamless", "1" }
                };

                var json = HttpClientPost(apiUrl + "game/", JsonConvert.SerializeObject(request), ContentType);
                var result = FromJson<GetGameUrlResponse>(json);

                return result.status != "success" ? ProcessErrorResponse(result) : new ApiResult() { ErrorCode = ErrorCode.Success, Message = result.gameurl };
            }
            catch (Exception ex)
            {
                // Log the exception
                return new ApiResult() { ErrorCode = ErrorCode.Fail, Message = ex.Message };
            }
        }
        
        public ApiResult Logout(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var siteConfig = _siteConfigDic[tpCustInfo.SiteId];
            var apiUrl = siteConfig.ApiUrl;
            var currencyConfig = siteConfig.CurrencyList.FirstOrDefault(x => x.CurrencyCode == tpCustInfo.TpCurrencyId);
            
            var tokenResponse = GetToken(apiUrl, currencyConfig.ClientKey, currencyConfig.SecretKey);
            if (tokenResponse.status != "success")
            {
                return ProcessErrorResponse(tokenResponse);
            }

            var getSessionResponse = GetPlayerCheck(apiUrl, tpCustInfo.TpUserName, tokenResponse.token);
            if (getSessionResponse.status != "success")
            {
                return ProcessErrorResponse(getSessionResponse);
            }

            var request = new Dictionary<string, string>()
            {
                {"token",tokenResponse.token},
                {"username",tpCustInfo.TpUserName},
                {"session",getSessionResponse.session}
            };
            
            var json = HttpClientPost(apiUrl + "player/?a=logout", JsonConvert.SerializeObject(request), ContentType);
            var result = FromJson<GetRegisterOrLogoutResponse>(json);

            return result.status != "success" ? ProcessErrorResponse(result) : new ApiResult() { ErrorCode = ErrorCode.Success, Message = result.message };
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        { 
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }
        
        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var spName = "Service_BettransLT_93Connect_Detail_Get";
            var db = DataBase.Report;
            var siteConfig = _siteConfigDic[tpCustInfo.SiteId];
            var apiUrl = siteConfig.ApiUrl;
            var currencyConfig = siteConfig.CurrencyList.FirstOrDefault(x => x.CurrencyCode == tpCustInfo.TpCurrencyId);
            
            
            TpCurrency tpCurrency = new()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            
            var wagerDetail = TpBetRecordProvider.DbQueryFirst(spName, new { getBetDetail.TransId }, db);
            if (wagerDetail != null && wagerDetail.Count > 0)
            {
                //wagerDetail.GetValueOrDefault("")?.ToString(),
                var detail = ToJson(wagerDetail);
                var resultData = new LotteryDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = getBetDetail.TransId,
                    TpUserName = wagerDetail.GetValueOrDefault("username")?.ToString(),
                    UTCBetTime = DateTime.Parse(wagerDetail.GetValueOrDefault("LocalBetTime")?.ToString()),
                    UTCSettledTime = DateTime.Parse(wagerDetail.GetValueOrDefault("LocalSettleTime")?.ToString()),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("bet_amt") * tpCurrency.ExchangeRate,
                    WinlostAmount = ((decimal)wagerDetail.GetValueOrDefault("win_amt") - 
                                     (decimal)wagerDetail.GetValueOrDefault("bet_amt")) * 
                                    tpCurrency.ExchangeRate,
                    DetailUrl = Get93ConnectBetDetailUrl(getBetDetail.TransId,apiUrl,currencyConfig.ClientKey,currencyConfig.SecretKey,tpCustInfo.TpUserName,tpCustInfo.TpPwd) ?? ""
                };
                return resultData;
            }
            return null;
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo,
            Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerTFG_Get", new { id = betRecordInfo.TransId }, DataBase.Wager);
            if (detail == null)
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = detail.GetStringValue("gamecode"),
                Game = tpGameType.TypeName,
                RefNo = detail.GetStringValue("drawno"),
                Title = tpGameType.TypeName,
                MatchID = betRecordInfo.TransId,
                BetType = detail.GetStringValue("bet_typegroup"),
                BetChoice = detail.GetStringValue("bet_type"),
                Result = detail.GetStringValue("status"),
                IP = string.Empty,
                Odds = decimal.TryParse(detail.GetStringValue("bet_odd"), out var odds) ? odds : 0,
            };
        }
        
        #region private

        private GetTokenResponse GetToken(string apiUrl ,string ClientKey, string SecretKey)
        {
            var getTokenUrl = apiUrl + "token/";
            var request = new Dictionary<string, string>
            {
                {"client_key",ClientKey},
                {"client_secret", SecretKey}
            };

            var sParam = JsonConvert.SerializeObject(request);

            var json = HttpClientPost(getTokenUrl, sParam, ContentType);
            var result = FromJson<GetTokenResponse>(json);

            return result;
        }

        private GetPlayerSessionResponse GetPlayerSession(string apiUrl, string accountName, string accountPWD,string clientIP, string token)
        {
            var getPlayerSessionUrl = apiUrl + "player/?a=login";
            //93Connect Password Rule : password required at least 1 alphabet, 1 numeric and 1 symbol.
            //因前面產的TpPwd有機會全英文，故這邊直接強制特殊符號及數字確保不會錯誤
            var request = new Dictionary<string, string>
            {
                {"token",token},
                {"username", accountName},
                {"password", accountPWD + "$7"},
                {"ip", clientIP}
            };
            var sParam = JsonConvert.SerializeObject(request);

            var json = HttpClientPost(getPlayerSessionUrl, sParam, ContentType);
            var result = FromJson<GetPlayerSessionResponse>(json);

            return result;
        }

        private GetPlayerCheckResponse GetPlayerCheck(string apiUrl, string accountName, string token)
        {
            var getPlayerCheckUrl = apiUrl + "player/?a=get";
            var request = new Dictionary<string, string>
            {
                {"token",token},
                {"username", accountName},
            };
            var sParam = JsonConvert.SerializeObject(request);

            var json = HttpClientPost(getPlayerCheckUrl, sParam, ContentType);
            var result = FromJson<GetPlayerCheckResponse>(json);

            return result;
        }

        private Get93ConnectBetDetailResponse Get93ConnectBetDetail(string apiUrl,string session,string transId,string username,string token)
        {
            var getBetDetailUrl = apiUrl + "player/?a=getbetbyid";
            var request = new Dictionary<string, string>
            {
                {"token",token},
                {"username", username},
                {"ticket",transId},
                {"lang_id","0"}
            };
            var sParam = JsonConvert.SerializeObject(request);

            var json = HttpClientPost(getBetDetailUrl, sParam, ContentType);
            var result = FromJson<Get93ConnectBetDetailResponse>(json);

            return result;
        }
        

        private string Get93ConnectBetDetailUrl(string transId ,string apiUrl, string clientKey, string SecretKey, string tpUserName, string tpPwd)
        {
            string playerSession;
            var getToken = GetToken(apiUrl, clientKey, SecretKey);
            var getPlayerCheck = GetPlayerCheck(apiUrl, tpUserName, getToken.token);
            if (!string.IsNullOrEmpty(getPlayerCheck.session))
            {
                playerSession = getPlayerCheck.session;
            }
            else
            {
                // 如果 getPlayerCheck 為 null 或其 session 為空，則嘗試獲取新的 session。
                var getPlayerSession = GetPlayerSession(apiUrl, tpUserName, tpPwd, "0.0.0.0" , getToken.token);
                playerSession = getPlayerSession.session;
            }
            
            var result = Get93ConnectBetDetail(apiUrl, playerSession,transId,tpUserName, getToken.token);
            return result != null ? result.mybeturl : "";
        }

        private static string GetErrorString(string errorMessage,IEnumerable<Error> errors)
        {
            var resultString = errors.Aggregate("", (current, error) => current + $"Main Message: {errorMessage}, ErrorCode: {error.code}, Message: {error.message}, Description: {error.description}");

            return resultString.Trim();
        }
        
        private string GetValueOrDefault(IReadOnlyDictionary<string, string> inputs, string key, string defaultValue = null, bool throwException = false)
        {
            if (!inputs.ContainsKey(key) || string.IsNullOrEmpty(inputs[key]))
            {
                if (throwException)
                    throw new Exception($"Missing or empty input for key: {key}");
                return defaultValue;
            }
            return inputs[key];
        }

        private ApiResult ProcessErrorResponse(dynamic response)
        {
            var errorList = response.errors.Count != 0 ? GetErrorString(response.message, response.errors) : response.message;
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Fail, 
                Message = errorList
            };
        }

        private static string ConvertLangId(string tpLangId)
        {
            return tpLangId switch
            {
                "en" => "0",
                "cs" => "1",
                "vn" => "3",
                "th" => "4",
                _ => "0"
            };
        }
        
        
        #endregion
        
        #region Transfer Method
        
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
        
        #endregion
    }
}