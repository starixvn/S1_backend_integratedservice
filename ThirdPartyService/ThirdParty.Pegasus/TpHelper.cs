using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json.Nodes;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Pegasus
{
    internal class TpHelper : ThirdPartyHelperBase
    {
        private const string ContentType = "application/json";
        private readonly Dictionary<int, Dictionary<string, string>> siteConfigDic;

        public TpHelper(IServiceProvider provider)
        {
            siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>(TpEnum.Pegasus.ToString());
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new ApiResult();
            oApiResult.ErrorCode = ErrorCode.Success;
            return oApiResult;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            var homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            var gameCode = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";

            var siteInputs = siteConfigDic[tpCustInfo.SiteId];
            var config = new PegasusConfig(siteInputs, tpCustInfo.TpCurrencyId);

            var token = ApplyToken(tpCustInfo);
            var sessionKeyArr = new[] { token, tpCustInfo.CustId.ToString(), tpCustInfo.TpUserName, gameCode };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = Convert.ToBase64String(bytes);

            var request = new
            {
                playerUniqueID = fun is "0" ? tpCustInfo.TpUserName : "fun",
                sessionToken = sessionKey,
                merchantID = config.MerchantID,
                merchantSecretKey = config.SecretKey,
                lang = TpLangId,
                gameID = gameCode,
                isTrialModeV2 = fun is "0" ? bool.FalseString : bool.TrueString,
                backUrl = homeUrl,
                userDevice = appType is AppType.Desktop ? "1" : "2", //PC=1 , H5=2
                version = "v1",
            };

            var dicRequest = FromJson<Dictionary<string, string>>(ToJson(request));
            var parameters = string.Join("&", dicRequest.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var gameUrl = config.ApiUrl + "/game/launch-link";
            var json = HttpGet(gameUrl, parameters, ContentType);
            var result = FromJson<PegasusGameLaunchResult>(json);

            if (result.code != 0)
            {
                return new ApiResult<UserBalance>()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = result.code.ToString(),
                    RawMessage = result.message,
                };
            }

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = result.data.link,
            };
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        #region Transfer Methods

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
