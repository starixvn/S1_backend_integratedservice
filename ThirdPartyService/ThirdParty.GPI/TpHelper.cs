using System;
using System.Collections.Generic;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.GPI
{
    public class TpHelper : ThirdPartyHelperBase
    {
        Dictionary<int, string> ErrorMassage = new Dictionary<int, string>()
        {   {-1,"Unknown error"},
            {-2,"Customer not found"},
            {-7,"Customer is blocked"},
            {-27,"Data type mismatch"},
            {-29,"Currency mismatch"},
            {-33,"Invalid amount"},
            {-48,"Invalid password"},
            {-119,"Missing input parameter"},
            {-201,"Transaction not exist"},
            {-202,"Operator not exist"}
        };
        private class LoginResult
        {
            public int error_code { get; set; }
            public string error_msg { get; set; }
            public string cust_id { get; set; }
            public string cust_name { get; set; }
            public string currency_code { get; set; }
            public string language { get; set; }
            public string country { get; set; }
            public string ip { get; set; }
            public string date_of_birth { get; set; }
            public bool test_cust { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            //throw new NotImplementedException();
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
            string RslotUrl = inputs.ContainsKey("RslotUrl") && !string.IsNullOrEmpty(inputs["RslotUrl"]) ? inputs["RslotUrl"] : throw new Exception();
            string LiveCasinoUrl = inputs.ContainsKey("LiveCasinoUrl") && !string.IsNullOrEmpty(inputs["LiveCasinoUrl"]) ? inputs["LiveCasinoUrl"] : throw new Exception();
            string LotteryUrl = inputs.ContainsKey("LotteryUrl") && !string.IsNullOrEmpty(inputs["LotteryUrl"]) ? inputs["LotteryUrl"] : throw new Exception();
            string P2PUrl = inputs.ContainsKey("P2PUrl") && !string.IsNullOrEmpty(inputs["P2PUrl"]) ? inputs["P2PUrl"] : throw new Exception();
            string OperatorId = inputs.ContainsKey("OperatorId") && !string.IsNullOrEmpty(inputs["OperatorId"]) ? inputs["OperatorId"] : throw new Exception();
            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            string cashierUrl = inputs.ContainsKey("CashierUrl") && !string.IsNullOrEmpty(inputs["CashierUrl"]) ? inputs["CashierUrl"] : "";
            string lobbyUrl = inputs.ContainsKey("LobbyUrl") && !string.IsNullOrEmpty(inputs["LobbyUrl"]) ? inputs["LobbyUrl"] : "";
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";


            ApiResult oApiResult = new ApiResult();
            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = $"{RslotUrl}{gameId}?fun={fun}&op={OperatorId}&lang={TpLangId}&fundsURL={cashierUrl}&homeURL={homeUrl}&lobbyURL={lobbyUrl}";
                return oApiResult;
            }

            var applyTokenInputs = FromJson<Dictionary<string, string>>(ToJson(tpCustInfo));
            applyTokenInputs.Add("TpLangId", TpLangId);
            applyTokenInputs.Add("ClientIP", inputs["ClientIP"]);

            string token = ApplyToken(applyTokenInputs);
            string gameType = inputs.ContainsKey("gameType") && !string.IsNullOrEmpty(inputs["gameType"]) ? inputs["gameType"] : "slots";
            
            oApiResult.ErrorCode = ErrorCode.Success;
            switch (gameType.ToLower())
            {
                case "slots":
                    oApiResult.Message = $"{RslotUrl}{gameId}?token={token}&fun={fun}&op={OperatorId}&lang={TpLangId}&fundsURL={cashierUrl}&homeURL={homeUrl}&lobbyURL={lobbyUrl}";//&hidelobby=1
                    break;
                case "livecasino":
                    if (appType == AppType.Native)
                    {
                        oApiResult.Message = $"{LiveCasinoUrl}v2?token=​{token}​&op={OperatorId}&lang={TpLangId}&sys=CUSTOM";
                    }
                    else if (appType == AppType.Mobile)
                    {
                        oApiResult.Message = $"{LiveCasinoUrl}html5/mobile?token=​{token}​&op={OperatorId}&lang={TpLangId}&sys=CUSTOM";
                    }
                    else
                    {
                        oApiResult.Message = $"{LiveCasinoUrl}html5/casino?token=​{token}​&op={OperatorId}&lang={TpLangId}&sys=CUSTOM";
                    }
                    break;
                case "lottery":
                    oApiResult.Message = $"{LotteryUrl}Login/?lang={TpLangId}&vendor={OperatorId}&ticket={token}&game={gameId}";
                    break;
                case "p2p":
                    oApiResult.Message = $"{P2PUrl}{gameId}/?op={OperatorId}&lang={TpLangId}&fun={fun}&token={token}";
                    break;
            }
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
    }
}
