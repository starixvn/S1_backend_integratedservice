using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.WonCasino
{
    public class TpHelper : ThirdPartyHelperBase , IThirdPartyParticular
    {
        private readonly Dictionary<int, WonCasinoConfig> _siteConfigDic;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<WonCasinoConfig>(TpEnum.WonCasino.ToString());
        }
        
        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            //WonCasino Login 及 CreateMember都是同一隻API。
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var siteConfig = _siteConfigDic[tpCustInfo.SiteId];
            var apiUrl = siteConfig.ApiUrl;
            var currencyConfig = siteConfig.CurrencyList.FirstOrDefault(x => x.CurrencyCode == tpCustInfo.TpCurrencyId);
            var fun = GetValueOrDefault(inputs, "fun", "0");
            
            //WonCasino Login 及 CreateMember都是同一隻API。
            var req = new SortedDictionary<string, object>
            {
                {"appid", currencyConfig.AppId},
                {"username", tpCustInfo.TpUserName},
                {"nickname", tpCustInfo.UserName},
                {"iscreate", "1"},
                {"clienttype", (int)appType},
                {"language", TpLangId},
                {"currency", currencyConfig.CurrencyCode},
            };

            if (fun == "1") 
            { 
                req.Add("ist","1");
            }

            var sign = GetSign(req, currencyConfig.SecretKey).ToLower();
            req.Add("sign",sign);

                
            var queryString =  string.Join("&", req.OrderBy(p => p.Key)
                .Select(p => $"{Uri.EscapeDataString(p.Key).ToLower()}={Uri.EscapeDataString(p.Value.ToString())}"));
                
            var result = HttpPostWithoutParameters(apiUrl + "/api/login?" + queryString,null,30);
                
            if (!IsJSON(result))
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = "1",
                    RawMessage = "No Result. Response is not json." 
                };
            }
                
            var jsonResponse = JsonConvert.DeserializeObject<WonLoginResponse>(result);

            if (jsonResponse == null)
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = "1", 
                    RawMessage = "No Result.Response is null." 
                };
            }

            if (jsonResponse.result == 0 || jsonResponse.desc == "succeed")
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Success,
                    Message = jsonResponse.openurl
                };
            }

            return new ApiResult()
            { 
                ErrorCode = ErrorCode.Fail,
                RawErrorCode = "1",
                RawMessage = "No Result.Response is null." 
            };
        }
        
        public ApiResult Logout(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var siteConfig = _siteConfigDic[tpCustInfo.SiteId];
            var apiUrl = siteConfig.ApiUrl;
            var currencyConfig = siteConfig.CurrencyList.FirstOrDefault(x => x.CurrencyCode == tpCustInfo.TpCurrencyId);
            
            var req = new SortedDictionary<string, object>
            {
                {"appid", currencyConfig.AppId},
                {"username", tpCustInfo.TpUserName},
            };

            var sign = GetSign(req, currencyConfig.SecretKey).ToLower();
            req.Add("sign",sign);


            var queryString =  string.Join("&", req.OrderBy(p => p.Key)
                .Select(p => $"{Uri.EscapeDataString(p.Key).ToLower()}={Uri.EscapeDataString(p.Value.ToString())}"));

            var result = HttpPostWithoutParameters(apiUrl + "/api/kickout?" + queryString,null,30);

            if (!IsJSON(result))
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = "1",
                    RawMessage = "No Result. Response is not json."
                };
            }

            var jsonResponse = JsonConvert.DeserializeObject<WonLoginResponse>(result);

            if (jsonResponse == null)
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = "1",
                    RawMessage = "No Result.Response is null."
                };
            }

            if (jsonResponse.result == 0 || jsonResponse.desc == "succeed")
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Success,
                    Message = jsonResponse.desc
                };
            }

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Fail,
                RawErrorCode = "1",
                RawMessage = "No Result.Response is null."
            };
                
        }
        
        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            //拿TpWager_WonCasino，Bettrans會有撤銷情況導致WinLoss不準確，WonCasino無注單明細API
            var spName = "Service_TpWager_WonCasino_Get";
            var db = DataBase.Wager;

            TpCurrency tpCurrency = new()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };

            var transId = int.Parse(getBetDetail.TransId);
            var wagerDetail = TpBetRecordProvider.DbQuery(spName, new{transId}, db);
            var getDetail = wagerDetail.LastOrDefault();
            var detail = ToJson(wagerDetail);
            
            return new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                Detail = detail,
                TpCurrency = tpCurrency,
                RefTransId = getBetDetail.TransId,
                TpUserName = getDetail.GetValueOrDefault("username")?.ToString(),
                UTCBetTime = DateTime.Parse(getDetail.GetValueOrDefault("createtime")?.ToString()).ToUniversalTime(),
                UTCSettledTime = DateTime.Parse(getDetail.GetValueOrDefault("updatetime")?.ToString()).ToUniversalTime(),
                Stake = (decimal)getDetail.GetValueOrDefault("betamount"),
                WinlostAmount = (decimal)getDetail.GetValueOrDefault("winlost") 
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo,
            Dictionary<string, string> inputs)
        {
            var transId = int.Parse(betRecordInfo.TransId);
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_WonCasino_Get", new { id = transId }, DataBase.Wager);
            if (detail == null)
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }
            
            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = detail.GetStringValue("bettype"),
                Game = tpGameType.TypeName,
                RefNo = detail.GetStringValue("gameroundid"),
                Title = tpGameType.TypeName,
                MatchID = betRecordInfo.TransId,
                BetType = detail.GetStringValue("bettype"),
                BetChoice = detail.GetStringValue("betpoint"),
                Result = detail.GetStringValue("gameresult"),
                IP = string.Empty,
                Odds = decimal.TryParse(detail.GetStringValue("betodds"), out var odds) ? odds : 0,
            };
        }
        
        #region private

        private string GetSign(SortedDictionary<string, object> fields, string secretKey)
        {
            var sortedData = fields.OrderBy(kvp => kvp.Key)
                .Select(kvp => $"{kvp.Key.ToLower()}={kvp.Value}")
                .ToList();
            
            var rawData = string.Join("&", sortedData) + "&key=" + secretKey;
            
            return ToMD5(rawData);
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

        private string ConvertGameType(string gametype)
        {
            return gametype switch
            {
                "4" => "Baccarat",
                "5" => "Roulette",
                "10" => "DragonTiger",
                "11" => "SicBo",
                "18" => "ColorDisc",
                "19" => "FishPrawnCrab",
                _ => "NA"
            };
        }
        
        #endregion
        
        #region Transfer
        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
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
        #endregion
    }
}