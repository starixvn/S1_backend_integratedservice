using System.Security.Cryptography;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.HB
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        public TpHelper()
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.HB).ToDictionary(d => d.AppId, d => d.Name);
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
            var brandId = GetInputValue(inputs, "BrandId");
            var launchUrl = GetInputValue(inputs, "LaunchUrl");
            var gameId = GetInputValue(inputs, "gameId");
            var homeUrl = GetInputValue(inputs, "homeUrl", true);

            string gameUrl;
            if (IsFunMode(inputs))
            {
                gameUrl = $"{launchUrl}?brandid={brandId}&keyname={gameId}&mode=fun&locale={TpLangId}&lobbyurl={homeUrl}";
            }
            else
            {
                var key = ApplyToken(tpCustInfo);
                inputs.TryGetValue("Token", out var token);
                var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName };
                var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
                var sessionKey = HttpUtility.UrlEncode(Convert.ToBase64String(bytes));
                gameUrl = $"{launchUrl}?brandid={brandId}&keyname={gameId}&token={sessionKey}&mode=real&locale={TpLangId}&lobbyurl={homeUrl}";
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
            ApiResult<UserBalance> oApiResult = new()
            {
                ErrorCode = ErrorCode.Fail
            };
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
            var brandId = GetInputValue(inputs, "BrandId");
            var url = GetInputValue(inputs, "LaunchUrl");
            var apiKey = GetInputValue(inputs, "APIKey");
            var idx = url.LastIndexOf('/');
            var gameHistory = url[..idx];
            var friendlygameinstanceid = getBetDetail.TransId;
            var hash = SHA256($"{friendlygameinstanceid.ToLower()}{brandId.ToLower()}{apiKey.ToLower()}");

            var URL = $"{gameHistory}/games/history/?brandid={brandId}&friendlyid={friendlygameinstanceid}&hash={hash}&locale=en&viewtype=game";
            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = URL,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }

        private static string GetInputValue(Dictionary<string, string> inputs, string key, bool allowNull = false)
        {
            if (inputs.ContainsKey(key) && !string.IsNullOrEmpty(inputs[key]))
            {
                return inputs[key];
            }

            if (allowNull)
                return "";
            throw new Exception($"inputs {key} is empty");
        }

        private static bool IsFunMode(Dictionary<string, string> inputs)
        {
            return inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) && inputs["fun"] == "1";
        }

        private string SHA256(string message)
        {
            var sha256 = new SHA256CryptoServiceProvider();
            var source = Encoding.UTF8.GetBytes(message);
            var crypto = sha256.ComputeHash(source);
            return BitConverter.ToString(crypto).Replace("-", "");
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var YYYYMM = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime.GetPartitionKey();
            var detail = TpBetRecordProvider.DbQueryFirst("Service_BettransRng_HB_Detail_Get",
                new { YYYYMM, betRecordInfo.TransId }, DataBase.Main);
            if (!detail.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var gameCode = detail.GetStringValue("keyname");
            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = gameCode,
                Game = GetGameName(gameCode),
                RefNo = detail.GetStringValue("gameinstanceid"),
                Title = tpGameType.TypeName,
                MatchID = detail.GetStringValue("initialdebittransferid"),
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = string.Empty,
            };
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
    }
}