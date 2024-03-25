using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.JILI
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        private readonly Dictionary<int, JILIConfig> _siteConfigDic;

        public TpHelper(IServiceProvider provider)
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.JILI).ToDictionary(d => d.AppId, d => d.Name);
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<JILIConfig>(TpEnum.JILI.ToString());
        }

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
            string GameUrl = config.GameUrl;
            string FunGameUrl = config.FunGameUrl;
            string AgentId = config.AgentId;
            string AgentKey = config.AgentKey;

            string HomeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            GameUrl = $"{GameUrl}/Login";

            if (tpCustInfo.MemberType == CustomerMemberTypeEnum.LiveStreaming)
                AgentId = config.AgentId_Stream;

            ApiResult oApiResult = new ApiResult();

            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = $"{FunGameUrl}/{gameId}/{TpLangId}";
                return oApiResult;
            }

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName, gameId };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            var now = new DateTimeOffset(DateTime.UtcNow);
            var current = now.ToOffset(new TimeSpan(-4, 0, 0)).ToString("yyMMd");
            string g = ToMD5($"{current}{AgentId}{AgentKey}").ToLower();
            var param = new
            {
                Token = sessionKey,
                GameId = gameId,
                Lang = TpLangId,
                AgentId = AgentId
            };
            var dicP = FromJson<Dictionary<string, string>>(ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            string md5Param = ToMD5($"{sParam}{g}").ToLower();
            string k = $"{GetRandomString(6)}{md5Param}{GetRandomString(6)}";

            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = $"{GameUrl}?{sParam}&HomeUrl={HomeUrl}&Key={k}";
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];
            var apiUrl = config.ApiUrl;
            var agentId = config.AgentId;
            var agentKey = config.AgentKey;

            var url = $"{apiUrl}/GetGameDetailUrl";
            var current = new DateTimeOffset(DateTime.UtcNow).ToOffset(new TimeSpan(-4, 0, 0)).ToString("yyMMd");
            var g = ToMD5($"{current}{agentId}{agentKey}").ToLower();

            var dic = new Dictionary<string, string>
            {
                { "WagersId",getBetDetail.TransId},
                { "AgentId",agentId},
            };
            var sParam = string.Join("&", dic.Select(kp => $"{kp.Key}={kp.Value}"));
            var key = $"{GetRandomString(6)}{ToMD5($"{sParam}{g}").ToLower()}{GetRandomString(6)}";
            sParam += $"&Key={key}&Lang=en-US";

            var json = HttpPost(url, sParam, "application/x-www-form-urlencoded");
            var result = FromJson<DetailResult>(json);
            if (result.ErrorCode != 0)
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result.ErrorCode} | {result.Message}");
            }

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = result.Data.Url,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var YYYYMM = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime.GetPartitionKey();
            var detailList = TpBetRecordProvider.DbQuery("Service_Bettrans_JILI_Detail_Get_V2", new { YYYYMM, GameType = tpGameType.Type, TransId = betRecordInfo.TransId }, DataBase.Main);
            if (!detailList.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var gameCode = detailList.GetFirstStringValue("game");
            var round = detailList.GetFirstStringValue("round");
            var sessionId = detailList.GetFirstStringValue("sessionId");

            return tpGameType.CategoryId switch
            {
                TpCategory.Casino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = gameCode,
                    Game = GetGameName(gameCode),
                    RefNo = round,
                    Title = tpGameType.TypeName,
                    MatchID = (sessionId != "0") ? sessionId : round,
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = string.Empty,
                    DeviceType = DeviceType.None,
                    IP = string.Empty,
                },
                TpCategory.Fishing => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = gameCode,
                    Game = GetFishGameName(gameCode),
                    RefNo = round,
                    Title = tpGameType.TypeName,
                    MatchID = round,
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = string.Empty,
                    DeviceType = DeviceType.None,
                    IP = string.Empty,
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        public class DetailResult
        {
            public int ErrorCode { get; set; }

            public string Message { get; set; }

            public Data Data { get; set; }
        }

        public class Data
        {
            public string Url { get; set; }
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

        private static string GetFishGameName(string gameCode)
        {
            return gameCode switch
            {
                "1" => "Royal Fishing",
                "2" => "Bombing Fishing",
                "3" => "Dinosaur Tycoon",
                "4" => "Jackpot Fishing",
                "5" => "Dragon Fortune",
                "6" => "Mega Fishing",
                "7" => "Boom Legend",
                "8" => "Happy Fishing",
                "9" => "All-star Fishing",
                _ => gameCode,
            };
        }

    }
}
