using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.Mega
{
    public class TpHelper : ThirdPartyHelperBase
    {
        
        private class CreateMemberResponse
        {
            public string id { get; set; }
            public string error { get; set; }
            public string jsonrpc { get; set; }
            public CreateMemberResult result { get; set; }
        }

        private class CreateMemberResult
        {
            public string loginId { get; set; }
            public bool success { get; set; }
            public string nickname { get; set; }
            public int userId { get; set; }
            public string regType { get; set; }
        }

        private class CheckTransferResponse
        {
            public string id { get; set; }
            public string jsonrpc { get; set; }
            public string error { get; set; }
            public CheckTransferResult result { get; set; }
        }

        private class CheckTransferResult
        {
            public int total { get; set; }
            public IList<CheckTransferItems> items { get; set; }

        }

        private class CheckTransferItems
        {
            public int amount { get; set; }
            public string loginId { get; set; }
            public string bizId { get; set; }
        }

        private class BalanceResponse
        {
            public string id { get; set; }
            public string error { get; set; }
            public string jsonrpc { get; set; }
            public decimal? result { get; set; }
        }

        private class ErrorResponse
        {
            //public string result { get; set; }
            public Error error { get; set; }
            public string id { get; set; }
            public string jsonrpc { get; set; }
        }

        private class Error
        {
            public string code { get; set; }
            public string sn { get; set; }
            public string message { get; set; }
            public string reason { get; set; }
            public string action { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string agentLoginId = inputs.ContainsKey($"agentLoginId_{currency}") && !string.IsNullOrEmpty(inputs[$"agentLoginId_{currency}"]) ? inputs[$"agentLoginId_{currency}"] : throw new Exception($"agentLoginId_{currency}");
            string secretCode = inputs.ContainsKey($"secretCode_{currency}") && !string.IsNullOrEmpty(inputs[$"secretCode_{currency}"]) ? inputs[$"secretCode_{currency}"] : throw new Exception($"secretCode_{currency}");
            string sn = inputs.ContainsKey("sn") && !string.IsNullOrEmpty("sn") ? inputs["sn"] : throw new Exception("sn");

            var uuid = Guid.NewGuid();

            string sign = uuid + sn + secretCode;

            var CheckTransfer = new
            {
                id = uuid,
                method = "open.mega.balance.transfer.query",
                @params = new
                {
                    random = uuid,
                    digest = ToMD5(sign).ToUpper(),
                    sn = sn,
                    loginId = tpCustInfo.TpUserName,
                    agentLoginId = agentLoginId,
                    bizId = ThirdPartyTransID
                },
                jsonrpc = "2.0",
            };

            var result = HttpPost(
                ApiUrl,
                JsonConvert.SerializeObject(CheckTransfer),
                "application/json"
            );

            ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
            if (IsJSON(result))
            {
                var error = JsonConvert.DeserializeObject<ErrorResponse>(result);
                if (error.error != null)
                {
                    apiResult.RawMessage = error.error.message;
                    apiResult.RawErrorCode = error.error.code.ToString();
                    apiResult.ErrorCode = ErrorCode.TransferPending;
                    apiResult.Data.Status = TransferStatus.Pending;
                }
                else
                {
                    var json = JsonConvert.DeserializeObject<CheckTransferResponse>(result);
                    if (json.result.total != 0 && json.result.items.First().bizId == TransID)
                    {
                        apiResult.ErrorCode = ErrorCode.Success;
                        apiResult.Data.Status = TransferStatus.Success;
                        //apiResult.Data.ThirdId = TransID;
                    }
                    else
                    {
                        apiResult.RawMessage = json.error ?? "";
                        apiResult.RawErrorCode = json.error.ToString();
                        apiResult.ErrorCode = ErrorCode.Fail;
                        apiResult.Data.Status = TransferStatus.Fail;
                    }
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
            string agentLoginId = inputs.ContainsKey($"agentLoginId_{currency}") && !string.IsNullOrEmpty(inputs[$"agentLoginId_{currency}"]) ? inputs[$"agentLoginId_{currency}"] : throw new Exception($"agentLoginId_{currency}");
            string secretCode = inputs.ContainsKey($"secretCode_{currency}") && !string.IsNullOrEmpty(inputs[$"secretCode_{currency}"]) ? inputs[$"secretCode_{currency}"] : throw new Exception($"secretCode_{currency}");
            string sn = inputs.ContainsKey("sn") && !string.IsNullOrEmpty("sn") ? inputs["sn"] : throw new Exception("sn");

            var uuid = Guid.NewGuid();

            string sign = uuid + sn + secretCode;

            var SignUp = new
            {
                id = uuid,
                method = "open.mega.user.create",
                @params = new
                {
                    random = uuid,
                    digest = ToMD5(sign).ToUpper(),
                    sn = sn,
                    agentLoginId = agentLoginId,
                },
                jsonrpc = "2.0",
            };

            var result = HttpPost(
                ApiUrl,
                JsonConvert.SerializeObject(SignUp),
                "application/json"
            );

            ApiResult oApiResult = new ApiResult();
            oApiResult.ErrorCode = ErrorCode.Fail;
            oApiResult.RawMessage = "No Result";
            oApiResult.RawErrorCode = "1";


            if (IsJSON(result))
            {
                var error = JsonConvert.DeserializeObject<ErrorResponse>(result);
                if (error.error != null)
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = error.error.message ?? "";
                    oApiResult.RawErrorCode = error.error.code.ToString() ?? "";
                }
                else
                {
                    var json = JsonConvert.DeserializeObject<CreateMemberResponse>(result);
                    if (json.error == null && json.result.success == true)
                    {
                        oApiResult.ErrorCode = ErrorCode.Success;
                        tpCustInfo.TpUserName = json.result.loginId;
                        oApiResult.RawMessage = "";
                        oApiResult.RawErrorCode = "";
                    }
                }
            }

            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string agentLoginId = inputs.ContainsKey($"agentLoginId_{currency}") && !string.IsNullOrEmpty(inputs[$"agentLoginId_{currency}"]) ? inputs[$"agentLoginId_{currency}"] : throw new Exception($"agentLoginId_{currency}");
            string secretCode = inputs.ContainsKey($"secretCode_{currency}") && !string.IsNullOrEmpty(inputs[$"secretCode_{currency}"]) ? inputs[$"secretCode_{currency}"] : throw new Exception($"secretCode_{currency}");
            string sn = inputs.ContainsKey("sn") && !string.IsNullOrEmpty("sn") ? inputs["sn"] : throw new Exception("sn");

            var uuid = Guid.NewGuid();

            string sign = uuid + sn + tpCustInfo.TpUserName + secretCode;

            var Balance = new
            {
                id = uuid,
                method = "open.mega.balance.get",
                @params = new
                {
                    random = uuid,
                    digest = ToMD5(sign).ToUpper(),
                    sn = sn,
                    loginId = tpCustInfo.TpUserName,
                },
                jsonrpc = "2.0",
            };

            var result = HttpPost(
                ApiUrl,
                JsonConvert.SerializeObject(Balance),
                "application/json"
            );

            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            oApiResult.RawMessage = "No Result";
            oApiResult.RawErrorCode = "1";

            if (IsJSON(result))
            {
                var error = JsonConvert.DeserializeObject<ErrorResponse>(result);
                if (error.error != null)
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = error.error.message ?? "";
                    oApiResult.RawErrorCode = error.error.code.ToString() ?? "";
                }
                else
                {
                    var json = JsonConvert.DeserializeObject<BalanceResponse>(result);
                    if (json.error == null)
                    {
                        oApiResult.ErrorCode = ErrorCode.Success;
                        oApiResult.RawMessage = "";
                        oApiResult.RawErrorCode = "";
                        oApiResult.Data.balance = json.result ?? 0;
                    }
                }
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
            ApiResult<dynamic> oApiResult = new ApiResult<dynamic>
            {
                ErrorCode = ErrorCode.Success,

            };
            oApiResult.Data = new
            {
                account = tpCustInfo.TpUserName,
                password = tpCustInfo.TpPwd
            };
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string agentLoginId = inputs.ContainsKey($"agentLoginId_{currency}") && !string.IsNullOrEmpty(inputs[$"agentLoginId_{currency}"]) ? inputs[$"agentLoginId_{currency}"] : throw new Exception($"agentLoginId_{currency}");
            string secretCode = inputs.ContainsKey($"secretCode_{currency}") && !string.IsNullOrEmpty(inputs[$"secretCode_{currency}"]) ? inputs[$"secretCode_{currency}"] : throw new Exception($"secretCode_{currency}");
            string sn = inputs.ContainsKey("sn") && !string.IsNullOrEmpty("sn") ? inputs["sn"] : throw new Exception("sn");
            
            if (direction == Direction.ToMain)
            {
                Amount = Amount * -1;
            }
            
            var uuid = Guid.NewGuid();

            string sign = uuid + sn + tpCustInfo.TpUserName + Amount + secretCode;

            long bizId = 0;

            Match m = Regex.Match(TransID, @"\d+");
            long.TryParse(m.Value, out bizId);

            if (TransID.StartsWith("td", StringComparison.OrdinalIgnoreCase))
            {
                bizId *= -1;
            }

            var Transfer = new
            {
                id = uuid,
                method = "open.mega.balance.transfer",
                @params = new
                {
                    random = uuid,
                    digest = ToMD5(sign).ToUpper(),
                    sn = sn,
                    loginId = tpCustInfo.TpUserName,
                    amount = Amount,
                    bizId = bizId.ToString(),
                    checkBizId = "1",
                },
                jsonrpc = "2.0",
            };

            var result = HttpPost(
                ApiUrl,
                JsonConvert.SerializeObject(Transfer),
                "application/json"
            );

            ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
            apiResult.RawMessage = "No Result";
            apiResult.RawErrorCode = "1";
            apiResult.ErrorCode = ErrorCode.TransferPending;
            apiResult.Data.Status = TransferStatus.Pending;
            apiResult.Data.ThirdId = bizId.ToString();

            if (IsJSON(result))
            {
                var error = JsonConvert.DeserializeObject<ErrorResponse>(result);
                if (error.error != null)
                {
                    apiResult.RawMessage = error.error.message ?? "";
                    apiResult.RawErrorCode = error.error.code.ToString() ?? "";
                    if (error.error.code == "721")
                    {
                        // 加解密数据异常
                        apiResult.ErrorCode = ErrorCode.Fail;
                        apiResult.Data.Status = TransferStatus.Fail;
                    }
                    else if (error.error.code == "700")
                    {
                        // 缺少参数
                        apiResult.ErrorCode = ErrorCode.Fail;
                        apiResult.Data.Status = TransferStatus.Fail;
                    }
                    else if (error.error.code == "711")
                    {
                        // 参数类型不匹配
                        apiResult.ErrorCode = ErrorCode.Fail;
                        apiResult.Data.Status = TransferStatus.Fail;
                    }
                    else if (error.error.code == "110")
                    {
                        // HTTP 404 error, action not found!
                        apiResult.ErrorCode = ErrorCode.Fail;
                        apiResult.Data.Status = TransferStatus.Fail;
                    }
                    else if (error.error.code == "37122")
                    {
                        // 额度转换失败-入款失败 bizId已存在
                        apiResult.ErrorCode = ErrorCode.Fail;
                        apiResult.Data.Status = TransferStatus.Fail;
                    }
                    else if (error.error.code == "37123")
                    {
                        // 额度转换失败-用户余额不足
                        apiResult.ErrorCode = ErrorCode.Fail;
                        apiResult.Data.Status = TransferStatus.Fail;
                    }
                    else
                    {
                        /* 
                         * 当给玩家下分时，code=37124,即 msg=额度转换失败-扣款失败，分数已经存在玩家的账户，建议到玩家离线后重试转账
                         * 当给玩家上分时，code=37124,即 msg=额度转换失败-扣款失败，贵司需要扣除玩家对应的分数，分数已经存在玩家的账户，只需要重新登录即可
                         */
                        apiResult.ErrorCode = ErrorCode.TransferPending;
                        apiResult.Data.Status = TransferStatus.Pending;
                    }
                }
                else
                {
                    var json = JsonConvert.DeserializeObject<BalanceResponse>(result);
                    if (json.error == null)
                    {
                        apiResult.ErrorCode = ErrorCode.Success;
                        apiResult.Data.Status = TransferStatus.Success;
                        apiResult.RawMessage = "";
                        apiResult.RawErrorCode = "";
                        // apiResult.Data.ThirdId = TransID;
                    }
                }
            }
            return apiResult;
        }
    }
}
