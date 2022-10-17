using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Kiss918
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private class ApiResponse
        {
            public string account { get; set; }
            public int code { get; set; }
            public string msg { get; set; }
            public bool success { get; set; }
        }

        private class BalanceResponse : ApiResponse
        {
            public string Account { get; set; }
            public string Agent { get; set; }
            public int DisOnlineDay { get; set; }
            public int GameID { get; set; }
            public int LobbyIdx { get; set; }
            public decimal? MoneyNum { get; set; }
            public string Name { get; set; }
            //public string Redme { get; set; }
            //public int Rownum { get; set; }
            public decimal? ScoreNum { get; set; }
            public int Status { get; set; }
            public int TableID { get; set; }
            //public string Tel { get; set; }
            public int Type { get; set; }
            public int UserAreaID { get; set; }
            public int areaid { get; set; }
            public long idx { get; set; }
        }

        private class TransferResponse : ApiResponse
        {
            public string acc { get; set; }
            public int db_index { get; set; }
            public string faildbs { get; set; }
            public bool isDeal { get; set; }
            public bool isProTime { get; set; }
            public bool isalldbok { get; set; }
            public decimal maxsetmoney { get; set; }
            public decimal money { get; set; }
            public string settime { get; set; }
        }
        private class CheckTransferResponse : ApiResponse
        {
            public string Account { get; set; }
            public decimal AfterNum { get; set; }
            public string C_DateTime { get; set; }
            public string OrderId { get; set; }
            public decimal ScoreNum { get; set; }
            public string UserName { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string WagerUrl = inputs.ContainsKey("WagerUrl") && !string.IsNullOrEmpty(inputs["WagerUrl"]) ? inputs["WagerUrl"] : throw new Exception("WagerUrl");
            string authcode = inputs.ContainsKey($"authcode_{currency}") && !string.IsNullOrEmpty(inputs[$"authcode_{currency}"]) ? inputs[$"authcode_{currency}"] : throw new Exception($"authcode_{currency}");
            string secretkey = inputs.ContainsKey($"secretkey_{currency}") && !string.IsNullOrEmpty(inputs[$"secretkey_{currency}"]) ? inputs[$"secretkey_{currency}"] : throw new Exception($"secretkey_{currency}");
            WagerUrl = $"{WagerUrl}/getOrder.ashx";

            long current = GetTimestemp1();
            string sign = authcode + TransID + current + secretkey;

            var CheckTransfer = new
            {
                orderid = TransID,
                authcode = authcode,
                time = current,
                sign = ToMD5(sign.ToLower()).ToUpper()
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(CheckTransfer));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPost(
                WagerUrl,
                sParam,
                "application/x-www-form-urlencoded"
            );

            ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<CheckTransferResponse>(result);
                if (json.code == 1 && json.success == true)
                {
                    apiResult.ErrorCode = ErrorCode.Success;
                    apiResult.Data.Status = TransferStatus.Success;
                    //apiResult.Data.ThirdId = TransID;
                }
                else if (json.code == -1)
                {
                    apiResult.RawMessage = json.msg ?? "";
                    apiResult.RawErrorCode = json.code.ToString();
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Status = TransferStatus.Fail;
                }
                else
                {
                    apiResult.RawMessage = json.msg;
                    apiResult.RawErrorCode = json.code.ToString();
                    apiResult.ErrorCode = ErrorCode.TransferPending;
                    apiResult.Data.Status = TransferStatus.Pending;
                }
            }
            else
            {
                apiResult.RawMessage = "No Result";
                apiResult.RawErrorCode = "1";
                apiResult.ErrorCode = ErrorCode.TransferPending;
                apiResult.Data.Status = TransferStatus.Pending;
            }

            return apiResult;
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string authcode = inputs.ContainsKey($"authcode_{currency}") && !string.IsNullOrEmpty(inputs[$"authcode_{currency}"]) ? inputs[$"authcode_{currency}"] : throw new Exception($"authcode_{currency}");
            string userName = inputs.ContainsKey($"userName_{currency}") && !string.IsNullOrEmpty(inputs[$"userName_{currency}"]) ? inputs[$"userName_{currency}"] : throw new Exception($"userName_{currency}");
            string secretkey = inputs.ContainsKey($"secretkey_{currency}") && !string.IsNullOrEmpty(inputs[$"secretkey_{currency}"]) ? inputs[$"secretkey_{currency}"] : throw new Exception($"secretkey_{currency}");
            string UserAreaId = inputs.ContainsKey($"UserAreaId_{currency}") && !string.IsNullOrEmpty(inputs[$"UserAreaId_{currency}"]) ? inputs[$"UserAreaId_{currency}"] : throw new Exception($"UserAreaId_{currency}");
            ApiUrl = $"{ApiUrl}/account.ashx";

            // 產生帳號
            long current = GetTimestemp1();
            string sign = authcode + userName + current + secretkey;
            var GenerateAccount = new
            {
                action = "RandomUserName",
                userName = userName,
                authcode = authcode,
                time = current,
                UserAreaId = UserAreaId,
                sign = ToMD5(sign.ToLower()).ToUpper()
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(GenerateAccount));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPost(
                ApiUrl,
                sParam,
                "application/x-www-form-urlencoded"
            );

            bool create = false;
            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<ApiResponse>(result);
                if (json.code == 0)
                {
                    create = true;
                    tpCustInfo.TpUserName = json.account;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.msg;
                    oApiResult.RawErrorCode = json.code.ToString();
                }
            }
            else
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
            }

            // 建立帳號
            if (create == true)
            {
                current = GetTimestemp1();
                sign = authcode + tpCustInfo.TpUserName + current + secretkey;
                var SignUp = new
                {
                    action = "addUser",
                    userName = tpCustInfo.TpUserName,
                    authcode = authcode,
                    time = current,
                    PassWd = tpCustInfo.TpPwd,
                    pwdtype = 1,
                    agent = userName,
                    Name = tpCustInfo.TpUserName,
                    UserType = 1,
                    UserAreaId = UserAreaId,
                    sign = ToMD5(sign.ToLower()).ToUpper()
                };

                dicP = FromJson<Dictionary<string, string>>(ToJson(SignUp));
                sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

                result = HttpPost(
                    ApiUrl,
                    sParam,
                    "application/x-www-form-urlencoded"
                );

                if (IsJSON(result))
                {
                    var json = JsonConvert.DeserializeObject<ApiResponse>(result);
                    if (json.code == 0)
                    {
                        oApiResult.ErrorCode = ErrorCode.Success;
                    }
                    else
                    {
                        oApiResult.ErrorCode = ErrorCode.Fail;
                        oApiResult.RawMessage = json.msg;
                        oApiResult.RawErrorCode = json.code.ToString();
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

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string authcode = inputs.ContainsKey($"authcode_{currency}") && !string.IsNullOrEmpty(inputs[$"authcode_{currency}"]) ? inputs[$"authcode_{currency}"] : throw new Exception($"authcode_{currency}");
            string secretkey = inputs.ContainsKey($"secretkey_{currency}") && !string.IsNullOrEmpty(inputs[$"secretkey_{currency}"]) ? inputs[$"secretkey_{currency}"] : throw new Exception($"secretkey_{currency}");
            ApiUrl = $"{ApiUrl}/account.ashx";

            long current = GetTimestemp1();
            string sign = authcode + tpCustInfo.TpUserName + current + secretkey;
            var Balance = new
            {
                action = "getUserInfo",
                userName = tpCustInfo.TpUserName,
                authcode = authcode,
                time = current,
                sign = ToMD5(sign.ToLower()).ToUpper()
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(Balance));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPost(
                ApiUrl,
                sParam,
                "application/x-www-form-urlencoded"
            );

            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<BalanceResponse>(result);
                if (json.success == true)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Data.balance = json.ScoreNum ?? 0;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.msg;
                    oApiResult.RawErrorCode = json.code.ToString();
                }
            }
            else
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
            }
            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            TransIdModel transIdModel = new TransIdModel();
            transIdModel.Implemented = false;
            return transIdModel;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new ApiResult
            {
                ErrorCode = ErrorCode.Success,
                Message = JsonConvert.SerializeObject(new
                {
                    account = tpCustInfo.TpUserName,
                    password = tpCustInfo.TpPwd
                })
            };

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string authcode = inputs.ContainsKey($"authcode_{currency}") && !string.IsNullOrEmpty(inputs[$"authcode_{currency}"]) ? inputs[$"authcode_{currency}"] : throw new Exception($"authcode_{currency}");
            string userName = inputs.ContainsKey($"userName_{currency}") && !string.IsNullOrEmpty(inputs[$"userName_{currency}"]) ? inputs[$"userName_{currency}"] : throw new Exception($"userName_{currency}");
            string secretkey = inputs.ContainsKey($"secretkey_{currency}") && !string.IsNullOrEmpty(inputs[$"secretkey_{currency}"]) ? inputs[$"secretkey_{currency}"] : throw new Exception($"secretkey_{currency}");
            ApiUrl = $"{ApiUrl}/setScore.ashx";

            long current = GetTimestemp1();
            string sign = authcode + tpCustInfo.TpUserName + current + secretkey;
            
            if(direction == Direction.ToMain)
            {
                Amount = Amount * - 1;
            }

            var Transfer = new
            {
                action = "setServerScore",
                userName = tpCustInfo.TpUserName,
                orderid = TransID,
                scoreNum = Amount,
                ActionUser = userName,
                authcode = authcode,
                time = current,
                sign = ToMD5(sign.ToLower()).ToUpper()
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(Transfer));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPost(
                ApiUrl,
                sParam,
                "application/x-www-form-urlencoded"
            );

            ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<TransferResponse>(result);
                if (json.code == 0 && json.success == true)
                {
                    apiResult.ErrorCode = ErrorCode.Success;
                    apiResult.Data.Status = TransferStatus.Success;
                    // apiResult.Data.ThirdId = TransID;
                }
                else if (json.code == -1 || json.code == -2 || json.code == -5 || json.code == -7 || json.code == -8 || json.code == -9 || json.code == -99)
                {
                    apiResult.RawMessage = json.msg;
                    apiResult.RawErrorCode = json.code.ToString();
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Status = TransferStatus.Fail;
                }
                else
                {
                    apiResult.RawMessage = json.msg;
                    apiResult.RawErrorCode = json.code.ToString();
                    apiResult.ErrorCode = ErrorCode.TransferPending;
                    apiResult.Data.Status = TransferStatus.Pending;
                }
            }
            else
            {
                apiResult.RawMessage = "No Result";
                apiResult.RawErrorCode = "1";
                apiResult.ErrorCode = ErrorCode.TransferPending;
                apiResult.Data.Status = TransferStatus.Pending;
            }

            return apiResult;
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
    }
}
