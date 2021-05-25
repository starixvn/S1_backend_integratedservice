using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.KM
{
    public class TpHelper : ThirdPartyHelperBase
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
            var apiUrl = GetInputValue(inputs, "ApiUrl");
            var gameUrl = GetInputValue(inputs, "GameUrl");
            var clientId = GetInputValue(inputs, "X-QM-ClientId");
            var clientSecret = GetInputValue(inputs, "X-QM-ClientSecret");

            var obj = new
            {
                ipaddress = inputs["ClientIP"],
                username = tpCustInfo.UserName,
                userid = tpCustInfo.TpUserName,
                lang = TpLangId,
                cur = tpCustInfo.TpCurrencyId,
                betlimitid = 1,
                platformtype = (int)appType - 1,
                istestplayer = false
            };
            var parameters = JsonConvert.SerializeObject(obj);
            var header = new Dictionary<string, string>()
            {
                {"X-QM-ClientId", clientId},
                {"X-QM-ClientSecret", clientSecret}
            };
            var result = HttpPost($"{apiUrl}/api/player/authorize", parameters, "application/json", header);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
            if (dict.ContainsKey("err"))
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    Message = dict["errdesc"],
                    RawErrorCode = dict["err"],
                    RawMessage = dict["errdesc"]
                };
            }
            else
            {
                AuthProvider.KmUserTokenSet(tpCustInfo.TpUserName, dict["authtoken"], DateTime.UtcNow.AddDays(1));
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Success,
                    Message = $"{gameUrl}?token={dict["authtoken"]}"
                };
            }
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

        private string GetInputValue(Dictionary<string, string> inputs, string key)
        {
            if(inputs.ContainsKey(key) && !string.IsNullOrEmpty(inputs[key]))
            {
                return inputs[key];
            }
            throw new Exception();
        }
    }
}