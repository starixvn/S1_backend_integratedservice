using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.BNG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, Dictionary<string, string>> siteConfigDic;

        public TpHelper(IServiceProvider provider)
        {
            siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>((TpEnum.BNG).ToString());
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
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var wagerDetailList = TpBetRecordProvider.DbQuery("Service_TpWager_BNG_Get",
                new { round_id = getBetDetail.TransId }, DataBase.Wager);
            var detail = ToJson(wagerDetailList);
            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                Detail = detail,
                TpCurrency = tpCurrency,
            };
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQuery("Service_TpWager_BNG_Get", new { round_id = betRecordInfo.TransId }, DataBase.Wager);
            if (detail.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = detail.FirstOrDefault().GetStringValue("game_id"),
                Game = tpGameType.TypeName,
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

            var siteInputs = siteConfigDic[tpCustInfo.SiteId];
            string apiUrl = siteInputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(siteInputs["ApiUrl"]) ? siteInputs["ApiUrl"] : throw new Exception();
            string projectName = siteInputs.ContainsKey("ProjectName") && !string.IsNullOrEmpty(siteInputs["ProjectName"]) ? siteInputs["ProjectName"] : throw new Exception();
            string apiToken = siteInputs.ContainsKey("ApiToken") && !string.IsNullOrEmpty(siteInputs["ApiToken"]) ? siteInputs["ApiToken"] : throw new Exception();
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception();

            #region token
            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = HttpUtility.UrlEncode(Convert.ToBase64String(bytes));
            #endregion

            var ts = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            var platform = appType == AppType.Desktop ? "desktop" : "mobile";
            var url = $"{apiUrl}{projectName}/game.html?token={sessionKey}&game={gameId}&ts={ts}&platform={platform}";

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