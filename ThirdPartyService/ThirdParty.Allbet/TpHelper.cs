using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Allbet
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, AllBetConfig> _allSiteConfig;

        public TpHelper(IServiceProvider provider)
        {
            _allSiteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<AllBetConfig>(TpEnum.Allbet.ToString());
        }

        #region Model
        private class Result<T>
        {
            public string resultCode { get; set; }
            public string message { get; set; }
            public T data { get; set; }
        }

        private class UserResult
        {
            public string player { get; set; }
        }

        private class LoginResult
        {
            public string gameLoginUrl { get; set; }
        }

        public class AllBetConfig
        {
            public string ApiUrl { get; set; }
            public string Suffix { get; set; }
            public string AllbetKey {  get; set; }
            public string OperatorId { get; set; }
            public string PartnerKey { get; set; }
            public List<CurrencyAgent> CurrencyAgents { get; set; }
        }
        
        public class CurrencyAgent
        {
            public string CurrencyId { get; set; }
            public string Agent { get; set; }
        }
        
        #endregion

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var _siteAllBetConfig = _allSiteConfig[tpCustInfo.SiteId];
            var apiUrl = _siteAllBetConfig.ApiUrl;
            var agent = _siteAllBetConfig.CurrencyAgents.FirstOrDefault(x => x.CurrencyId == tpCustInfo.TpCurrencyId).Agent; 
            var path = "CheckOrCreate";
            tpCustInfo.TpUserName = $"{tpCustInfo.TpUserName}{_siteAllBetConfig.Suffix}";
            var parameters = ToJson(new
            {
                agent,
                player = tpCustInfo.TpUserName
            });
            var header = GetAuthData(inputs, path, parameters, _siteAllBetConfig, out var contentType);

            var result = HttpPostClient($"{apiUrl}/{path}", parameters, contentType, header);

            var oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var userResult = FromJson<Result<UserResult>>(result);
                if (userResult.resultCode.Equals("OK", StringComparison.OrdinalIgnoreCase))
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawErrorCode = userResult.resultCode;
                    oApiResult.Message = oApiResult.RawMessage = userResult.message;
                }
            }
            else
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.SystemError;
            }
            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string tpLangId, Dictionary<string, string> inputs)
        {
            var _siteAllBetConfig = _allSiteConfig[tpCustInfo.SiteId];
            var apiUrl = _siteAllBetConfig.ApiUrl;
            var isFun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) && inputs["fun"] == "1";
            var path = isFun ? "LoginTrial" : "Login";

            if (!tpCustInfo.TpUserName.EndsWith(_siteAllBetConfig.Suffix))
            {
                tpCustInfo.TpUserName = $"{tpCustInfo.TpUserName}{_siteAllBetConfig.Suffix}";
            }

            var parameters = ToJson(new
            {
                player = isFun ? null : tpCustInfo.TpUserName,
                language = tpLangId
            });
            var header = GetAuthData(inputs, path, parameters, _siteAllBetConfig, out var contentType);

            var result = HttpPostClient($"{apiUrl}/{path}", parameters, contentType, header);

            var oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var userResult = FromJson<Result<LoginResult>>(result);
                if (userResult.resultCode.Equals("OK", StringComparison.OrdinalIgnoreCase))
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = userResult.data.gameLoginUrl;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawErrorCode = userResult.resultCode;
                    oApiResult.Message = oApiResult.RawMessage = userResult.message;
                }
            }
            else
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.SystemError;
            }
            return oApiResult;
        }
        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_Allbet_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            var detail = ToJson(wagerDetail);
            var gameCode = wagerDetail.GetValueOrDefault("gameType")?.ToString();
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.LiveCasino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("gameRoundId")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("client")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("betTime", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("gameRoundEndTime", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("betAmount") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("winOrLoss") * tpCurrency.ExchangeRate,
                    GameCode = gameCode,
                    GameName = GetGameName(gameCode),
                },
                _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                }
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_Allbet_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);
            var gameCode = wagerDetail.GetValueOrDefault("gameType")?.ToString();
            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = gameCode,
                Game = GetGameName(gameCode),
                RefNo = wagerDetail.GetStringValue("betNum"),
                Title = wagerDetail.GetStringValue("tableName"),
                MatchID = wagerDetail.GetStringValue("gameRoundId"),
                BetType = wagerDetail.GetStringValue("betType"),
                BetChoice = string.Empty,
                Result = wagerDetail.GetStringValue("gameResult"),
                DeviceType = GetDeviceType(wagerDetail.GetStringValue("appType")),
                IP = string.Empty,
            };
        }
        private DeviceType GetDeviceType(string device)
        {
            return device switch
            {
                "6" => DeviceType.Desktop,
                "3" => DeviceType.Mobile,
                _ => DeviceType.None,
            };
        }

        private string GetGameName(string gameCode)
        {
            return gameCode switch
            {
                "101" => "Normal Baccarat",
                "102" => "VIP Baccarat",
                "103" => "Quick Baccarat",
                "104" => "See Card Baccarat",
                "110" => "Insurance Baccarat",
                "201" => "Sicbo(HiLo)",
                "301" => "Dragon Tiger",
                "401" => "Roulette",
                "501" => "Pok Deng",
                "601" => "Rock Paper Scissors",
                "801" => "Bull Bull",
                "901" => "Win Three Cards / Three Pictures",
                _ => gameCode,
            };
        }

        private Dictionary<string, string> GetAuthData(IReadOnlyDictionary<string, string> inputs, string path, string parameters, AllBetConfig config, out string contentType)
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
    }
}
