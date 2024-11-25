using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.BNG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly BNGConfig _siteBNGConfig;

        public TpHelper(IServiceProvider provider)
        {
            _siteBNGConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<BNGConfig>(TpEnum.BNG.ToString());
        }
        public class BNGConfig : TpSiteConfig
        {
            public string ProjectName { get; set; }
            public string ApiToken { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var wagerDetailList = TpBetRecordProvider.DbQuery("Service_BettransRng_BNG_Detail_Get",
                new { round_id = getBetDetail.TransId }, DataBase.Wager);
            var detail = ToJson(wagerDetailList);
            return new CasinoDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                Detail = detail,
                TpCurrency = tpCurrency,
                TpUserName = wagerDetailList.FirstOrDefault()?.GetValueOrDefault("player_id")?.ToString(),
                UTCBetTime = wagerDetailList.Min(i => (DateTime)i.GetValueOrDefault("c_at", DateTime.MinValue)),
                UTCSettledTime = wagerDetailList.Max(i => (DateTime)i.GetValueOrDefault("c_at", DateTime.MinValue)),
                Stake = wagerDetailList.Sum(i => Math.Abs((decimal)i.GetValueOrDefault("bet"))) * tpCurrency.ExchangeRate,
                WinlostAmount = (wagerDetailList.Sum(i => Math.Abs((decimal)i.GetValueOrDefault("win"))) * tpCurrency.ExchangeRate) - (wagerDetailList.Sum(i => Math.Abs((decimal)i.GetValueOrDefault("bet"))) * tpCurrency.ExchangeRate),
                GameCode = wagerDetailList.FirstOrDefault()?.GetValueOrDefault("game_id")?.ToString(),
                GameName = wagerDetailList.FirstOrDefault()?.GetValueOrDefault("game_name")?.ToString(),
            };
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQuery("Service_TpWager_BNG_Get", new { round_id = betRecordInfo.TransId }, DataBase.Wager);
            if (!detail.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = detail.FirstOrDefault().GetStringValue("game_id"),
                Game = detail.FirstOrDefault().GetStringValue("game_name"),
                RefNo = string.Empty,
                Title = tpGameType.TypeName,
                MatchID = betRecordInfo.TransId,
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = string.Empty,
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new ApiResult();

            string apiUrl = _siteBNGConfig.ApiUrl;
            string projectName = _siteBNGConfig.ProjectName;
            string apiToken = _siteBNGConfig.ApiToken;
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception();
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            bool FunPlay = fun == "1" ? true : false;

            #region token
            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = HttpUtility.UrlEncode(Convert.ToBase64String(bytes));
            #endregion

            var ts = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            var platform = appType == AppType.Desktop ? "desktop" : "mobile";

            var url = $"{apiUrl}{projectName}/game.html?token={sessionKey}&game={gameId}&ts={ts}&platform={platform}&lang={TpLangId}";

            if (FunPlay)
            {
                url = $"{apiUrl}{projectName}/game.html?token={sessionKey}&game={gameId}&ts={ts}&platform={platform}&lang={TpLangId}&wl=demo";
            }

            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = url;

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
    }
}