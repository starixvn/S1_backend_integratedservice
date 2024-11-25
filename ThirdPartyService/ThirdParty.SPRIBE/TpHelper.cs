using System.Net;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.SPRIBE
{
    public class TpHelper : ThirdPartyHelperBase
    {

        #region SPRIBE Configs Setting

        private enum ProductWallet
        {
            SPRIBE = 101,
        }

        Dictionary<TpGameType, int> _productWallet = new()
        {
            {TpGameType.SPRIBE_RNG, (int)ProductWallet.SPRIBE},
        };
        public class SPRIBEConfig : TpSiteConfig
        {
            public string Key { get; set; }
            public string Prefix { get; set; }
            public string MerchantName { get; set; }
        }

        private readonly Dictionary<int, SPRIBEConfig> _siteSPRIBEConfig;
        #endregion

        #region Response

        private class RegisterResponse
        {
            public int Code { get; set; }
            public string Message { get; set; } = "";
            public string Currency { get; set; } = "";
        }

        private class LaunchGameResponse
        {
            public int Code { get; set; }
            public string Message { get; set; } = "";
            public string GameUrl { get; set; } = "";
            public string SessionToken { get; set; } = "";
            public int TTL { get; set; }
        }

        #endregion

        private const string ContentType = "application/json";
        private readonly WebProxy? _proxy;

        public TpHelper(IServiceProvider provider)
        {
            var tokyoProxyConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValueByEnvironment<ProxyConfig>("TokyoProxy");
            if (tokyoProxyConfig != null)
            {
                _proxy = new WebProxy(tokyoProxyConfig.ProxyHost, tokyoProxyConfig.ProxyPort);
            }
            _siteSPRIBEConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SPRIBEConfig>(TpEnum.SPRIBE.ToString());
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = _siteSPRIBEConfig[tpCustInfo.SiteId];
            var apiUrl = $"{config.ApiUrl}/Player/Register";

            var signUp = new
            {
                MerchantCode = config.Key,
                PlayerId = tpCustInfo.TpUserName,
                Currency = tpCustInfo.TpCurrencyId,
                Password = tpCustInfo.TpPwd
            };

            var json = HttpPostWithProxy(apiUrl, ToJson(signUp), ContentType, _proxy);
            var result = FromJson<RegisterResponse>(json);

            if (result.Code == 0)
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Success,
                    Message = result.Message
                };
            }
            else
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    Message = result.Message
                };
            }
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteSPRIBEConfig[tpCustInfo.SiteId];
            var apiUrl = appType == AppType.Desktop ? $"{config.ApiUrl}/Game/NewLaunchGame" : $"{config.ApiUrl}/Game/NewLaunchMobileGame";
            var productWallet = _productWallet[TpGameType.SPRIBE_RNG];
            var fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";

            var playerName = tpCustInfo.TpUserName;
            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                apiUrl = appType == AppType.Desktop ? $"{config.ApiUrl}/Game/LaunchFreeGame" : $"{config.ApiUrl}/Game/LaunchFreeMobileGame";
                playerName = null;
            }

            var signUp = new
            {
                MerchantCode = config.Key,
                PlayerId = playerName,
                GameCode = inputs["gameId"],
                Language = TpLangId,
                IpAddress = inputs["ClientIP"],
                ProductWallet = productWallet,
                LobbyURL = inputs["homeUrl"]
            };

            var json = HttpPostWithProxy(apiUrl, ToJson(signUp), ContentType, _proxy, null);
            var result = FromJson<LaunchGameResponse>(json);

            return new ApiResult
            {
                ErrorCode = ErrorCode.Success,
                Message = result.GameUrl,
                RawMessage = result.Message
            };
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var oApiResult = new ApiResult<UserBalance>
            {
                ErrorCode = ErrorCode.Fail
            };
            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
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
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerRng_SPRIBE_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            var detail = ToJson(wagerDetail);
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.Casino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = "",
                    TpUserName = wagerDetail.GetValueOrDefault("PlayerId")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("BetDate", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("SettlementDate", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("BetAmount") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("WinLoss") * tpCurrency.ExchangeRate,
                    GameCode = wagerDetail.GetValueOrDefault("GameId")?.ToString(),
                    GameName = wagerDetail.GetValueOrDefault("GameName")?.ToString(),
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
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerRng_SPRIBE_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = wagerDetail.GetValueOrDefault("GameId")?.ToString(),
                Game = wagerDetail.GetValueOrDefault("GameName")?.ToString(),
                RefNo = wagerDetail.GetValueOrDefault("RoundId")?.ToString(),
                Title = tpGameType.TypeName,
                MatchID = wagerDetail.GetValueOrDefault("ExternalRoundId")?.ToString(),
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = wagerDetail.GetValueOrDefault("Status")?.ToString(),
                DeviceType = GetDeviceType(wagerDetail.GetValueOrDefault("Platform")?.ToString()),
                IP = string.Empty,
            };
        }

        private DeviceType GetDeviceType(string device)
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
