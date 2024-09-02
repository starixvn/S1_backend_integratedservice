using System.Text;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.SG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly IReadOnlyDictionary<string, string> _GameNameList;
        private readonly Dictionary<int, SGConfig> _siteConfigDic;

        public TpHelper(IServiceProvider provider)
        {
            _GameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.SG).ToDictionary(d => d.GameId, d => d.Name);
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SGConfig>(TpEnum.SG.ToString());
        }

        #region Response

        public class LoginResponse
        {
            public string token { get; set; }
            public string gameUrl { get; set; }
            public string msg { get; set; }
            public int code { get; set; }
            public string serialNo { get; set; }
        }
        #endregion

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new ApiResult();
            oApiResult.ErrorCode = ErrorCode.Success;
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

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];

            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            var mobile = (appType == AppType.Mobile) ? true : false;

            var tpGameType = (int)TpGameType.SG_RNG;
            if (gameId.StartsWith("F-", StringComparison.OrdinalIgnoreCase))
            {
                tpGameType = (int)TpGameType.SG_Fish;
            }
            else if (gameId.StartsWith("C-", StringComparison.OrdinalIgnoreCase))
            {
                tpGameType = (int)TpGameType.SG_Table;
            }

            TpCustInfoProvider.SetCustGotoGame(tpCustInfo.CustId, tpCustInfo.TpId, tpGameType);

            // Header
            Dictionary<string, string> headerReq = new()
            {
                { "API", "getAuthorize" },
                { "DataType", "JSON" }
            };

            ApiResult oApiResult = new ApiResult();

            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                var demo = GetPlayDemo(headerReq, tpCustInfo.SiteId, TpLangId, gameId, mobile, inputs["ClientIP"]);
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = demo;
                return oApiResult;
            }

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            var Login = new
            {
                merchantCode = config.merchantCode,
                acctInfo = new
                {
                    acctId = tpCustInfo.TpUserName,
                    userName = tpCustInfo.TpUserName,
                    currency = tpCustInfo.TpCurrencyId,
                    siteId = tpCustInfo.SiteId,
                },
                language = TpLangId,
                token = sessionKey,
                game = gameId,
                acctIp = inputs["ClientIP"],
                fun = false,
                mobile = mobile,
                menuMode = false,
            };

            var result = HttpPost(
                config.ApiUrl,
                JsonConvert.SerializeObject(Login),
                "application/json",
                headerReq
            );

            if (!IsJSON(result))
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = result;
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<LoginResponse>(result);
            if (json.code == 0)
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = json.gameUrl;
            }
            else
            {
                oApiResult.RawMessage = json.msg;
                oApiResult.RawErrorCode = json.code.ToString();
                oApiResult.Message = $"{json.code}-{json.msg}";
                oApiResult.ErrorCode = ErrorCode.Fail;
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
            var spName = getBetDetail.TpGameTypeOption.Type is (int)TpGameType.SG_RNG or (int)TpGameType.SG_Table ?
                "Service_TpWagerRng_SG_Detail_Get" : "Service_TpWagerSG_Detail_Get";
            var wagerDetail = TpBetRecordProvider.DbQueryFirst(spName, new { getBetDetail.TransId }, DataBase.Wager);
            var detail = ToJson(wagerDetail);
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.Casino => new CasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("ticketId")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("acctId")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("ticketTime", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("ticketTime", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("betAmount") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("winLoss") * tpCurrency.ExchangeRate,
                    GameCode = wagerDetail.GetValueOrDefault("gameCode")?.ToString(),
                    GameName = GetGameName(wagerDetail.GetValueOrDefault("gameCode")?.ToString()),
                },
                TpCategory.P2P => new CasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("ticketId")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("acctId")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("ticketTime", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("ticketTime", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("winLoss") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("winLoss") * tpCurrency.ExchangeRate,
                    GameCode = wagerDetail.GetValueOrDefault("gameCode")?.ToString(),
                    GameName = GetGameName(wagerDetail.GetValueOrDefault("gameCode")?.ToString()),
                },
                TpCategory.Fishing => new FishingDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("ticketId")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("acctId")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("ticketTime", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("ticketTime", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("betAmount") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("winLoss") * tpCurrency.ExchangeRate,
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
            var spName = tpGameType.Type == (int)TpGameType.SG_RNG ? "Service_TpWagerRng_SG_Detail_Get" : "Service_TpWagerSG_Detail_Get";
            var detail = TpBetRecordProvider.DbQueryFirst(spName, new { betRecordInfo.TransId }, DataBase.Wager);
            var gameCode = detail.GetStringValue("gameCode");
            return tpGameType.CategoryId switch
            {
                TpCategory.Casino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = gameCode,
                    Game = GetGameName(gameCode),
                    RefNo = detail.GetStringValue("referenceId"),
                    Title = tpGameType.TypeName,
                    MatchID = string.Empty,
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = string.Empty,
                    DeviceType = GetDeviceType(detail.GetStringValue("channel")),
                    IP = detail.GetStringValue("betIp"),
                },
                TpCategory.Fishing => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = gameCode,
                    Game = GetFishGameName(gameCode),
                    RefNo = detail.GetStringValue("referenceId"),
                    Title = tpGameType.TypeName,
                    MatchID = detail.GetStringValue("roundId"),
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = detail.GetStringValue("result"),
                    DeviceType = GetDeviceType(detail.GetStringValue("channel")),
                    IP = detail.GetStringValue("betIp"),
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        private string GetPlayDemo(Dictionary<string, string> headerReq, int SiteId, string TpLangId, string GameId, bool Mobile, string Ip)
        {
            var DefaultTpCurrencyId = ServerInfoProvider.GetDefaultTpCurrencyIdByTpId(SiteId, (int)TpEnum.SG);
            var currency = (DefaultTpCurrencyId != null) ? DefaultTpCurrencyId.TpCurrencyId : "";

            var config = _siteConfigDic[SiteId];

            var Login = new
            {
                merchantCode = config.merchantCode,
                acctInfo = new
                {
                    acctId = "guest",
                    currency = currency,
                },
                language = TpLangId,
                token = Guid.NewGuid().ToString(),
                game = GameId,
                acctIp = Ip,
                fun = true,
                mobile = Mobile,
            };

            var result = HttpPost(
                config.ApiUrl,
                JsonConvert.SerializeObject(Login),
                "application/json",
                headerReq
            );

            if (!IsJSON(result))
            {
                throw new Exception("GetPlayDemo Format Failed");
            }

            var json = JsonConvert.DeserializeObject<LoginResponse>(result);
            if (json.code != 0 || json.gameUrl == null)
            {
                throw new Exception($"GetPlayDemo gameUrl Failed {json.code}\r\n{json.msg}");
            }

            return json.gameUrl;
        }

        private static DeviceType GetDeviceType(string device)
        {
            return device switch
            {
                "Web" => DeviceType.Desktop,
                "PC" => DeviceType.Desktop,
                "Mobile" => DeviceType.Mobile,
                "APP-i" => DeviceType.iOS,
                "APP-A" => DeviceType.Android,
                _ => DeviceType.None,
            };
        }

        private string GetGameName(string gameCode)
        {
            if (!_GameNameList.TryGetValue(gameCode, out var name))
            {
                return string.Empty;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return dic.TryGetValue("en", out var result) ? result : string.Empty;
        }

        private static string GetFishGameName(string gameCode)
        {
            return gameCode switch
            {
                "F-SF01" => "Fishing God",
                "F-SF02" => "Fishing War",
                "F-ZP01" => "Zombie Party",
                "F-AH01" => "Alien Hunter",
                _ => gameCode,
            };
        }
    }
}
