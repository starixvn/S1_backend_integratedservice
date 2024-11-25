using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
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
            
            //93Connect UserName 最大名稱為15字(三方限制)，
            //因特殊幣別部分會造成字元超出限制，改為使用93定義自建帳號。
            //Rule : SiteId + _ + CustId
            var new93AccountName = $"{tpCustInfo.SiteId}_{tpCustInfo.CustId}";
            //93Connect Password Rule : password required at least 1 alphabet, 1 numeric and 1 symbol.
            //因前面產的TpPwd有機會全英文，故這邊直接強制特殊符號及數字確保不會錯誤
            var new93AccountPwd = $"{tpCustInfo.TpPwd}$7";
            
            //將更新至DB
            tpCustInfo.TpUserName = new93AccountName;
            tpCustInfo.TpPwd = new93AccountPwd;
            
            
            var request = new Dictionary<string, string>()
            {
                { "token", tokenResponse.token },
                { "username", new93AccountName },
                { "password", new93AccountPwd},
                { "confirm_password", new93AccountPwd },
                { "test_account", tpCustInfo.IsTest ? "1" : "0" }
            };
            
            var json = HttpClientPost(apiUrl + "player/?a=register", JsonConvert.SerializeObject(request), ContentType);
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
                    Message = result.errors.FirstOrDefault()?.description
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
                    return new ApiResult()
                    {
                        ErrorCode = ErrorCode.Success, 
                        Message = siteConfig.FunGameUrl
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
            var spName = "Service_Bettrans_93Connect_Detail_Get";
            var db = DataBase.Main;
            var siteConfig = _siteConfigDic[tpCustInfo.SiteId];
            var apiUrl = siteConfig.ApiUrl;
            var YYYYMM = getBetDetail.SettledTime.GetPartitionKey();
            var currencyConfig = siteConfig.CurrencyList.FirstOrDefault(x => x.CurrencyCode == tpCustInfo.TpCurrencyId);
            
            TpCurrency tpCurrency = new()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            
            var wagerDetail = TpBetRecordProvider.DbQueryFirst(spName, new { YYYYMM, getBetDetail.TransId }, db);
            if (wagerDetail != null && wagerDetail.Count > 0)
            {
                var detail = ToJson(wagerDetail);
                var resultData = new PokerDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = getBetDetail.TransId,
                    TpUserName = tpCustInfo.TpUserName,
                    UTCBetTime = DateTime.Parse(wagerDetail.GetValueOrDefault("LocalBetTime")?.ToString()),
                    UTCSettledTime = DateTime.Parse(wagerDetail.GetValueOrDefault("LocalSettleTime")?.ToString()),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("bet_amt"),
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("win_amt") - 
                                     (decimal)wagerDetail.GetValueOrDefault("bet_amt"),
                    DetailUrl = Get93ConnectBetDetailUrl(getBetDetail.TransId,apiUrl,currencyConfig.ClientKey,currencyConfig.SecretKey,tpCustInfo.TpUserName,tpCurrency.TpCurrencyId)
                };
                return resultData;
            }
            return null;
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo,
            Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_93Connect_Get", new { id = betRecordInfo.TransId }, DataBase.Wager);
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

            var request = new Dictionary<string, string>
            {
                {"token",token},
                {"username", accountName},
                {"password", accountPWD},
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

        private Get93ConnectBetDetailResponse Get93ConnectBetDetail(string apiUrl,string transId,string username, string tpCurrency,string token)
        {
            var getBetDetailUrl = apiUrl + "player/?a=getbetbyid";
            var request = new Dictionary<string, string>
            {
                {"token",token},
                {"username", username},
                {"ticket",transId},
                {"lang_id","0"} // all language use English : 0
            };
            var sParam = JsonConvert.SerializeObject(request);

            var json = HttpClientPost(getBetDetailUrl, sParam, ContentType);
            var result = FromJson<Get93ConnectBetDetailResponse>(json);

            return result;
        }
        

        private string Get93ConnectBetDetailUrl(string transId ,string apiUrl, string clientKey, string SecretKey, string tpCustId, string tpCurrency)
        {
            var getToken = GetToken(apiUrl, clientKey, SecretKey);
            if (getToken.status != "success")
            {
                return "";
            }
            
            var result = Get93ConnectBetDetail(apiUrl,transId,tpCustId, tpCurrency,getToken.token);
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