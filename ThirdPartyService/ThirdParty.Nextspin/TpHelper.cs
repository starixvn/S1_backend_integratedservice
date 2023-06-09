using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Nextspin
{
    public class TpHelper : ThirdPartyHelperBase
    {
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
            throw new NotImplementedException();
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
    }
}
