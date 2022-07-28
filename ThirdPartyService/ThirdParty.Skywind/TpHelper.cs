using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Skywind
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public TpHelper()
        {

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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            string SecretKey = inputs.ContainsKey("SecretKey") && !string.IsNullOrEmpty(inputs["SecretKey"]) ? inputs["SecretKey"] : throw new Exception();
            string Username = inputs.ContainsKey("Username") && !string.IsNullOrEmpty(inputs["Username"]) ? inputs["Username"] : throw new Exception();
            string Password = inputs.ContainsKey("Password") && !string.IsNullOrEmpty(inputs["Password"]) ? inputs["Password"] : throw new Exception();
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception();
            var jwtToken = GetJwtToken(ApiUrl, Username, Password, SecretKey);
            var gameUrl = $"{ApiUrl}/players/{tpCustInfo.TpUserName}/games/{gameId}";
            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("Authorization", $"Bearer {jwtToken.accessToken}");
            var json = HttpGet(gameUrl, null, null, header);
            ApiResult oApiResult = new ApiResult();
            {
                if (IsJSON(json))
                {
                    var result = FromJson<GameURL>(json);
                    if (!string.IsNullOrWhiteSpace(result.url))
                    {
                        oApiResult.ErrorCode = ErrorCode.Success;
                        oApiResult.Message = result.url;
                    }
                    else
                    {
                        oApiResult.RawMessage = "No Result";
                        oApiResult.RawErrorCode = "1";
                        oApiResult.ErrorCode = ErrorCode.Fail;                 
                    }
                }
                else
                {
                    oApiResult.RawMessage = "No Result";
                    oApiResult.RawErrorCode = "1";
                    oApiResult.ErrorCode = ErrorCode.Fail;
                }
            }
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        private JwtToken GetJwtToken(string apiUrl, string username, string password, string secretKey)
        {
            string url = $"{apiUrl}/login";
            var sParam = new
            {
                secretKey = secretKey,
                username = username,
                password = password
            };
            var param = ToJson(sParam);
            var r = HttpPost(url, param, "application/json");
            var result = JsonConvert.DeserializeObject<JwtToken>(r);
            return result;
        }

        public class JwtToken
        {
            public string key { get; set; }
            public string username { get; set; }
            public string accessToken { get; set; }
        }
        public class GameURL
        {
            public string url { get; set; }
            public string token { get; set; }
        }
    }
}
