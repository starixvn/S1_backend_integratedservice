using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.EVO
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<string, string> countryList = new Dictionary<string, string>() {
            {"MYR","MY"},
            {"CNY","CN"},
            {"IDR","ID"},
            {"THB","TH"},
            {"VND","VN"},
            {"INR","HT"},
        };

        public class LoginResponse
        {
            public string entry { get; set; }
            public string entryEmbedded { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new ApiResult();
            tpCustInfo.TpUserName = $"{tpCustInfo.TpUserName}";
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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string GameUrl = inputs.ContainsKey("GameUrl") && !String.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception($"GameUrl is empty");
            string Ua2Token = inputs.ContainsKey("Ua2Token") && !string.IsNullOrEmpty(inputs["Ua2Token"]) ? inputs["Ua2Token"] : throw new Exception("Ua2Token");
            string CasinoKey = inputs.ContainsKey("CasinoKey") && !string.IsNullOrEmpty(inputs["CasinoKey"]) ? inputs["CasinoKey"] : throw new Exception("CasinoKey");
            string lobbyUrl = inputs.ContainsKey("LobbyUrl") && !string.IsNullOrEmpty(inputs["LobbyUrl"]) ? inputs["LobbyUrl"] : "LobbyUrl";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "0";
            ApiUrl = $"{ApiUrl}/{CasinoKey}/{Ua2Token}";

            bool mobile = appType != AppType.Desktop;
            var siteName = ServerInfoProvider.getSiteName(tpCustInfo.SiteId);

            ApiResult oApiResult = new ApiResult();

            if (string.IsNullOrEmpty(siteName))
            {
                oApiResult.RawMessage = "Site Error";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var tpId = (int)TpEnum.EVO;
            var gameType = (int)TpGameType.EVO_LiveCasino;
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName, gameType.ToString(), tpId.ToString() };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            var Login = new
            {
                uuid = Guid.NewGuid().ToString(),
                player = new
                {
                    id = tpCustInfo.TpUserName,
                    update = true,
                    country = countryList[tpCustInfo.TpCurrencyId],
                    language = TpLangId,
                    currency = tpCustInfo.TpCurrencyId,
                    session = new
                    {
                        id = sessionKey,
                        ip = inputs["ClientIP"]
                    },
                    group = new
                    {
                        id = siteName.ToString(),
                        action = "assign"
                    }
                },
                config = new
                {
                    brand = new
                    {
                        id = siteName.ToString(),
                        skin = "1"
                    },
                    game = new
                    {
                        category = "top_games",// baccarat
                        //table = new
                        //{
                        //    id = "k2oswnib7jjaaznw"
                        //},
                        //playMode = "reward_games"
                    },
                    channel = new
                    {
                        wrapped = mobile,
                        mobile = mobile
                    },
                }
            };

            var tmp = JsonConvert.SerializeObject(Login);
            var result = HttpPost(
                 ApiUrl,
                 JsonConvert.SerializeObject(Login),
                 "application/json"
             );

            if (!IsJSON(result))
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<LoginResponse>(result);
            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = $"{GameUrl}{json.entry}";

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
    }
}
