using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.PG2
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, PG2Config> _siteConfigDic;
        private const TpEnum Tp = TpEnum.PG2;
        private const string ContentType = "application/json";

        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<PG2Config>(Tp.ToString());
        }
        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];
            var apiUrl = config.ApiUrl;
            var key = config.CurrencyData[tpCustInfo.TpCurrencyId].ApiKey;
            var agent = config.CurrencyData[tpCustInfo.TpCurrencyId].Agent;
            var param = new
            {
                username = tpCustInfo.TpUserName,
                agent = agent,
            };

            var jsonParam = JsonConvert.SerializeObject(param);
            var header = new Dictionary<string, string>
            {
                {"x-amb-signature", HMACSHA512(jsonParam, key)}
            };

            try
            {
                var result = HttpPost($"{apiUrl}/seamless/create", jsonParam, ContentType, header);

                if (IsJSON(result))
                {
                    var gameResult = JsonConvert.DeserializeObject<CommonResponse>(result);

                    if (gameResult.status.code != 0)
                    {
                        return new ApiResult
                        {
                            ErrorCode = ErrorCode.Fail,
                            RawMessage = gameResult.status.message,
                            RawErrorCode = gameResult.status.code.ToString()
                        };
                    }

                    return new ApiResult { ErrorCode = ErrorCode.Success }; // 表示成功
                }

                return new ApiResult
                {
                    ErrorCode = ErrorCode.Fail,
                    RawMessage = result
                };
            }
            catch (Exception e)
            {
                return new ApiResult
                {
                    ErrorCode = ErrorCode.Fail,
                    RawMessage = e.Message
                };
            }
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];
            var apiUrl = config.ApiUrl;
            var key = config.CurrencyData[tpCustInfo.TpCurrencyId].ApiKey;
            var agent = config.CurrencyData[tpCustInfo.TpCurrencyId].Agent;
            var gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            var homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";

            var req = new 
            {
                username = tpCustInfo.TpUserName,
                agent = agent,
                partner = "PG",
                code = gameId,
                gameId = gameId,
                backLink = homeUrl
            };
            var jsonParam = JsonConvert.SerializeObject(req);
            var header = new Dictionary<string, string>
            {
                {"x-amb-signature", HMACSHA512(jsonParam, key)}
            };

            var result = HttpPost($"{apiUrl}/seamless/launchByGame", jsonParam, ContentType,header);

            if (!IsJSON(result))
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = "1",
                    RawMessage = "No Result"
                };

            var json = JsonConvert.DeserializeObject<CommonResponse>(result);

            if (json is null || json.data is null || string.IsNullOrWhiteSpace(json.data.url))
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = "-1",
                    RawMessage = json == null ? "No Result" : "No Url",
                };

            return new ApiResult
            {
                ErrorCode = ErrorCode.Success,
                Message = json.data.url
            };
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>
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
            var detailList = TpBetRecordProvider.DbQuery("Service_BettransRng_PG2_Detail_Get",
                new { YYYYMM = getBetDetail.SettledTime.GetPartitionKey(), getBetDetail.TransId }, DataBase.Report);
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            return new CasinoDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                Detail = ToJson(detailList),
                TpCurrency = tpCurrency,
                RefTransId = string.Empty,
                TpUserName = detailList.FirstOrDefault()?.GetValueOrDefault("username")?.ToString(),
                UTCBetTime = detailList.Min(i => (DateTime)i.GetValueOrDefault("timestamp", DateTime.MinValue)),
                UTCSettledTime = detailList.Max(i => (DateTime)i.GetValueOrDefault("timestamp", DateTime.MinValue)),
                Stake = detailList.Where(i => new[] { 1, 4, 10 }.Contains((sbyte)i.GetValueOrDefault("TicketType")))
                    .Sum(i => Math.Abs((decimal)i.GetValueOrDefault("betAmount"))),
                WinlostAmount = detailList.Sum(i => (decimal)i.GetValueOrDefault("amount")),
                GameCode = detailList.FirstOrDefault()?.GetValueOrDefault("GameId")?.ToString(),
                GameName = detailList.FirstOrDefault()?.GetValueOrDefault("gameName")?.ToString(),
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo,
            Dictionary<string, string> inputs)
        {
            var YYYYMM = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime.GetPartitionKey();
            var detailList = TpBetRecordProvider.DbQuery("Service_BettransRng_PG2_Detail_Get",
                new { YYYYMM, betRecordInfo.TransId }, DataBase.Report);

            if (!detailList.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = detailList.GetFirstStringValue("GameId"),
                Game = detailList.GetFirstStringValue("gameName"),
                RefNo = detailList.GetFirstStringValue("parentRounId"),
                Title = tpGameType.TypeName,
                MatchID = detailList.GetFirstStringValue("RounId"),
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = string.Empty,
            };
        }

        private string HMACSHA512(string input, string key)
        {
            var salt = Encoding.UTF8.GetBytes(key);
            var HASH_SIZE = 64;
            var ITERATIONS = 1000;

            // Generate the hash
            var pbkdf2 = new Rfc2898DeriveBytes(input, salt, ITERATIONS, HashAlgorithmName.SHA512);
            var data = pbkdf2.GetBytes(HASH_SIZE);
            return Convert.ToBase64String(data);
        }
    }
}
