using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using System.Threading.Tasks;
using ThirdPartyCore.Model.Exceptions;

namespace ThirdParty.HB
{
    public class TpHelper: ThirdPartyHelperBase
    {
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

            var gameUrl = "";
            if (IsFunMode(inputs))
            {
                gameUrl = $"{launchUrl}?brandid={brandId}&keyname={gameId}&mode=fun&local={TpLangId}&lobbyurl={homeUrl}";
            }
            else
            {
                var key = ApplyToken(tpCustInfo);
                inputs.TryGetValue("Token", out var token);
                var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName };
                var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
                var sessionKey = HttpUtility.UrlEncode(Convert.ToBase64String(bytes));
                gameUrl = $"{launchUrl}?brandid={brandId}&keyname={gameId}&token={sessionKey}&mode=real&local={TpLangId}&lobbyurl={homeUrl}";
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

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo,
            Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private string GetInputValue(Dictionary<string, string> inputs, string key, bool allowNull = false)
        {
            if(inputs.ContainsKey(key) && !string.IsNullOrEmpty(inputs[key]))
            {
                return inputs[key];
            }

            if (allowNull)
                return "";
            throw new Exception($"inputs {key} is empty");
        }

        private bool IsFunMode(Dictionary<string, string> inputs)
        {
            return inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) && inputs["fun"] == "1";
        }
    }
}