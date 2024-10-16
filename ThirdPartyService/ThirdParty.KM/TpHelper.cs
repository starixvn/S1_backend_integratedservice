using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.KM
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, KMConfig> _siteConfig;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<KMConfig>(TpEnum.KM.ToString());
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfig[tpCustInfo.SiteId];

            var apiUrl = config.ApiUrl;
            var gameUrl = (tpCustInfo.TpCurrencyId == "RMB") ? config.GameUrl_RMB : config.GameUrl;
            var clientId = config.X_QM_ClientId;
            var clientSecret = config.X_QM_ClientSecret;

            var obj = new
            {
                ipaddress = inputs["ClientIP"],
                username = tpCustInfo.TpUserName,
                userid = tpCustInfo.TpUserName,
                lang = TpLangId,
                cur = tpCustInfo.TpCurrencyId,
                betlimitid = 1,
                platformtype = (int)appType - 1,
                istestplayer = false
            };
            var parameters = JsonConvert.SerializeObject(obj);
            var header = new Dictionary<string, string>()
            {
                {"X-QM-ClientId", clientId},
                {"X-QM-ClientSecret", clientSecret}
            };
            var result = HttpPost($"{apiUrl}/api/player/authorize", parameters, "application/json", header);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
            if (dict.ContainsKey("err"))
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    Message = dict["errdesc"],
                    RawErrorCode = dict["err"],
                    RawMessage = dict["errdesc"]
                };
            }
            else
            {
                var platform = appType == AppType.Desktop ? "Nova88_Desktop" : "Nova88_Mobile";

                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Success,
                    Message = $"{gameUrl}/{platform}?token={dict["authtoken"]}"
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
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_KM_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            var detail = ToJson(wagerDetail);
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.P2P => new PokerDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("txid")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("userid")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("beton", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("betupdatedon", DateTime.MinValue),
                    Stake = Math.Abs((decimal)wagerDetail.GetValueOrDefault("riskamt")) * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("winloss") * tpCurrency.ExchangeRate,
                    GameCode = wagerDetail.GetValueOrDefault("gameid")?.ToString(),
                    GameName = wagerDetail.GetValueOrDefault("gamename")?.ToString(),
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
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_KM_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = detail.GetStringValue("gameid"),
                Game = detail.GetStringValue("gamename"),
                RefNo = detail.GetStringValue("txid"),
                Title = detail.GetStringValue("gameprovider"),
                MatchID = detail.GetStringValue("roundid"),
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = GetDeviceType(detail.GetStringValue("platformtype")),
                IP = detail.GetStringValue("ipaddress")
            };
        }

        private static DeviceType GetDeviceType(string device)
        {
            return device switch
            {
                "Desktop" => DeviceType.Desktop,
                "Mobile" => DeviceType.Mobile,
                _ => DeviceType.None,
            };
        }
    }
}