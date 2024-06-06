using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Nextspin
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        private readonly ILogger<TpHelper> _logger;

        public TpHelper(IServiceProvider provider)
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.Nextspin).ToDictionary(d => d.AppId, d => d.Name);
            _logger = provider.GetRequiredService<ILogger<TpHelper>>();
        }
        #region NotImplemented
        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        #endregion

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var merchantCode = GetInputValue(inputs, "MerchantCode");
            var launchUrl = GetInputValue(inputs, "LaunchUrl");
            var gameCode = GetInputValue(inputs, "gameId");
            var apiUrl = GetInputValue(inputs, "ApiUrl");

            try
            {
                // 透過三方api取得LaunchUrl       
                launchUrl = GetLaunchUrl(launchUrl, apiUrl, merchantCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            string gameUrl;
            if (IsFunMode(inputs))
            {
                launchUrl = launchUrl.Replace("lobby", "play");
                gameUrl = $"{launchUrl}/game?merchantCode={merchantCode}&game={gameCode}&language={TpLangId}";
            }
            else
            {
                var key = ApplyToken(tpCustInfo);
                gameUrl = $"{launchUrl}/{merchantCode}/auth?acctId={tpCustInfo.TpUserName}&language={TpLangId}&token={key}&game={gameCode}";
            }
            var apiResult = new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = gameUrl
            };
            return apiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var merchantCode = GetInputValue(inputs, "MerchantCode");
            var launchUrl = GetInputValue(inputs, "LaunchUrl");
            var secretKey = GetInputValue(inputs, "SecretKey");
            launchUrl = launchUrl.Replace("lobby", "gameapi");

            var secret = $"{getBetDetail.TransId}|{secretKey}";
            var token = ToMD5(secret);

            var betDetailUrl = $"{launchUrl}/betDetails?merchantId={merchantCode}&token={token}&lang=en&ticketId={getBetDetail.TransId}";

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = betDetailUrl,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                }
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var YYYYMM = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime.GetPartitionKey();
            var type = betRecordInfo.TransId.Length > 20 ? 1 : 0;
            var detail = TpBetRecordProvider.DbQueryFirst("Service_BettransRng_Nextspin_Detail_Get",
                new { YYYYMM, betRecordInfo.TransId, Type = type }, DataBase.Main);
            if (!detail.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var gameCode = detail.GetStringValue("gameCode");
            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = gameCode,
                Game = GetGameName(gameCode),
                RefNo = detail.GetStringValue("referenceId"),
                Title = tpGameType.TypeName,
                MatchID = detail.GetStringValue("ticketId"),
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = GetDeviceType(detail.GetStringValue("channel")),
                IP = string.Empty,
            };
        }

        private bool IsFunMode(Dictionary<string, string> inputs)
        {
            return GetInputValue(inputs, "fun") == "1";
        }

        private string GetInputValue(Dictionary<string, string> inputs, string key, bool allowNull = false)
        {
            if (inputs.ContainsKey(key) && !string.IsNullOrEmpty(inputs[key]))
            {
                return inputs[key];
            }

            if (allowNull)
                return "";
            throw new Exception($"inputs {key} is empty");
        }
        private string GetGameName(string gameCode)
        {
            if (!_gameNameList.TryGetValue(gameCode, out var name))
            {
                return string.Empty;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return dic.TryGetValue("en", out var result) ? result : string.Empty;
        }
        private DeviceType GetDeviceType(string device)
        {
            if (device.Equals("Web", StringComparison.OrdinalIgnoreCase))
            {
                return DeviceType.Desktop;
            }

            if (device.Equals("Mobile", StringComparison.OrdinalIgnoreCase))
            {
                return DeviceType.Mobile;
            }

            return DeviceType.None;
        }

        private string GetLaunchUrl(string launchUrl, string apiUrl, string merchantCode)
        {
            var header = new Dictionary<string, string>()
            {
                {"API", "getDomainList"},
                {"DataType", "JSON"}
            };
            var parameters = JsonConvert.SerializeObject(new
            {
                merchantCode,
                serialNo = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            });

            var json = HttpPost(apiUrl, parameters, "application/json", header);
            var result = FromJson<GetDomainListResponse>(json);

            if (result.code == 0)
            {
                launchUrl = result.domains[0];
            }
            else
            {
                _logger.LogWarning($"Nextspin getDomainList API error: {JsonConvert.SerializeObject(result)}");
            }

            return launchUrl;
        }
    }
}
