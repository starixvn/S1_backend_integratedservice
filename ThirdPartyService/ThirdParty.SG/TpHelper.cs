using System;
using System.Collections.Generic;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using System.Text;
using System.Web;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using System.Linq;
using Newtonsoft.Json;
using ThirdPartyCore.Model.TpBetRecordExtend;
using ThirdPartyCore.Extensions;

namespace ThirdParty.SG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly IReadOnlyDictionary<string, string> GameNameList;

        public TpHelper()
        {
            GameNameList = TpBetRecordProvider.GetGameCasinoList(10).ToDictionary(d => d.GameId, d => d.Name);
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
            //throw new NotImplementedException();
            string GameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception();
            string merchantCode = inputs.ContainsKey("merchantCode") && !string.IsNullOrEmpty(inputs["merchantCode"]) ? inputs["merchantCode"] : throw new Exception();
            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            string FunGameUrl = inputs.ContainsKey("FunGameUrl") && !string.IsNullOrEmpty(inputs["FunGameUrl"]) ? inputs["FunGameUrl"] : throw new Exception("FunGameUrl");

            ApiResult oApiResult = new ApiResult();
            string mobile = (appType == AppType.Mobile) ? "true" : "false";

            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = $"{FunGameUrl}?language={TpLangId}&game={gameId}&menumode=off&mobile={mobile}";
                return oApiResult;
            }

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = $"{GameUrl}?acctId={tpCustInfo.TpUserName}&language={TpLangId}&token={HttpUtility.UrlEncode(sessionKey)}&game={gameId}&menumode=off&mobile={mobile}";
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
            var spName = getBetDetail.TpGameTypeOption.Type == 15 ? "Service_TpWagerRng_SG_Detail_Get" : "Service_TpWagerSG_Detail_Get";
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
            var spName = tpGameType.Type == 15 ? "Service_TpWagerRng_SG_Detail_Get" : "Service_TpWagerSG_Detail_Get";
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

        private DeviceType GetDeviceType(string device)
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
            if (!GameNameList.TryGetValue(gameCode, out var name))
            {
                return string.Empty;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return dic.TryGetValue("en", out var result) ? result : string.Empty;
        }

        private string GetFishGameName(string gameCode)
        {
            return gameCode switch
            {
                "F-SF01" => "Fishing God",
                "F-SF02" => "Fishing War",
                _ => gameCode,
            };
        }
         
 


    }
}
