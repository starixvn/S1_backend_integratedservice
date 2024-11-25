using System.Text.RegularExpressions;

using Newtonsoft.Json;

using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Mega
{
    public class TpHelper : ThirdPartyHelperBase, IThirdPartyParticular
    {
        #region Response

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
            public decimal amount { get; set; }
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

        private class LogoutResponse
        {
            public string result { get; set; }
            public string error { get; set; }
            public string jsonrpc { get; set; }
        }

        private class ErrorResponse
        {
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

        #endregion

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string agentLoginId = inputs.ContainsKey($"agentLoginId_{currency}") && !string.IsNullOrEmpty(inputs[$"agentLoginId_{currency}"]) ? inputs[$"agentLoginId_{currency}"] : throw new Exception($"agentLoginId_{currency}");
            string secretCode = inputs.ContainsKey($"secretCode_{currency}") && !string.IsNullOrEmpty(inputs[$"secretCode_{currency}"]) ? inputs[$"secretCode_{currency}"] : throw new Exception($"secretCode_{currency}");
            string sn = inputs.ContainsKey("sn") && !string.IsNullOrEmpty(inputs["sn"]) ? inputs["sn"] : throw new Exception("sn");

            var uuid = Guid.NewGuid();

            string sign = $"{uuid}{sn}{secretCode}";

            Match m = Regex.Match(TransID, @"\d+");
            long.TryParse(m.Value, out long bizId);
            bizId += 220000000000;

            if (IsDevelopment)
            {
                bizId *= -1;
            }

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
                    bizId = bizId
                },
                jsonrpc = "2.0",
            };

            var result = HttpPost(
                ApiUrl,
                JsonConvert.SerializeObject(CheckTransfer),
                "application/json"
            );

            ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
            apiResult.Data.ThirdId = bizId.ToString();
            apiResult.RawMessage = "No Result";
            apiResult.RawErrorCode = "1";
            apiResult.ErrorCode = ErrorCode.TransferPending;
            apiResult.Data.Status = TransferStatus.Pending;

            if (!IsJSON(result))
            {
                return apiResult;
            }

            var error = JsonConvert.DeserializeObject<ErrorResponse>(result);
            if (error.error != null)
            {
                apiResult.RawMessage = error.error.message;
                apiResult.RawErrorCode = error.error.code.ToString();
                apiResult.ErrorCode = ErrorCode.TransferPending;
                apiResult.Data.Status = TransferStatus.Pending;
                return apiResult;
            }

            if (ThirdPartyTransID == null)
            {
                ThirdPartyTransID = bizId.ToString();
            }

            var json = JsonConvert.DeserializeObject<CheckTransferResponse>(result);
            if (json.result.total != 0 && json.result.items.Any(x => x.bizId == ThirdPartyTransID && x.loginId == tpCustInfo.TpUserName))
            {
                //json.result.items.First().bizId == ThirdPartyTransID && json.result.items.First().loginId == tpCustInfo.TpUserName
                apiResult.ErrorCode = ErrorCode.Success;
                apiResult.Data.Status = TransferStatus.Success;
                apiResult.Data.ThirdId = bizId.ToString();
            }
            else
            {
                apiResult.RawMessage = json.error ?? "";
                apiResult.RawErrorCode = json.error ?? "";
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Data.Status = TransferStatus.Fail;
            }

            return apiResult;
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string agentLoginId = inputs.ContainsKey($"agentLoginId_{currency}") && !string.IsNullOrEmpty(inputs[$"agentLoginId_{currency}"]) ? inputs[$"agentLoginId_{currency}"] : throw new Exception($"agentLoginId_{currency}");
            string secretCode = inputs.ContainsKey($"secretCode_{currency}") && !string.IsNullOrEmpty(inputs[$"secretCode_{currency}"]) ? inputs[$"secretCode_{currency}"] : throw new Exception($"secretCode_{currency}");
            string sn = inputs.ContainsKey("sn") && !string.IsNullOrEmpty(inputs["sn"]) ? inputs["sn"] : throw new Exception("sn");

            var uuid = Guid.NewGuid();

            string sign = $"{uuid}{sn}{secretCode}";

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

            if (!IsJSON(result))
            {
                return oApiResult;
            }

            var error = JsonConvert.DeserializeObject<ErrorResponse>(result);
            if (error.error != null)
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = error.error.message ?? "";
                oApiResult.RawErrorCode = error.error.code.ToString() ?? "";
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<CreateMemberResponse>(result);
            if (json.error == null && json.result.success)
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                tpCustInfo.TpUserName = json.result.loginId;
                oApiResult.RawMessage = "";
                oApiResult.RawErrorCode = "";
            }

            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string secretCode = inputs.ContainsKey($"secretCode_{currency}") && !string.IsNullOrEmpty(inputs[$"secretCode_{currency}"]) ? inputs[$"secretCode_{currency}"] : throw new Exception($"secretCode_{currency}");
            string sn = inputs.ContainsKey("sn") && !string.IsNullOrEmpty(inputs["sn"]) ? inputs["sn"] : throw new Exception("sn");

            var uuid = Guid.NewGuid();

            string sign = $"{uuid}{sn}{tpCustInfo.TpUserName}{secretCode}";

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

            if (!IsJSON(result))
            {
                return oApiResult;
            }

            var error = JsonConvert.DeserializeObject<ErrorResponse>(result);
            if (error.error != null)
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = error.error.message ?? "";
                oApiResult.RawErrorCode = error.error.code.ToString() ?? "";
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<BalanceResponse>(result);
            if (json.error == null)
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.RawMessage = "";
                oApiResult.RawErrorCode = "";
                oApiResult.Data.balance = json.result ?? 0;
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
                    account = "",
                    password = ""
                })
            };

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string secretCode = inputs.ContainsKey($"secretCode_{currency}") && !string.IsNullOrEmpty(inputs[$"secretCode_{currency}"]) ? inputs[$"secretCode_{currency}"] : throw new Exception($"secretCode_{currency}");
            string sn = inputs.ContainsKey("sn") && !string.IsNullOrEmpty(inputs["sn"]) ? inputs["sn"] : throw new Exception("sn");
            ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();

            if (direction == Direction.ToMain)
            {
                Amount *= -1;
            }

            var uuid = Guid.NewGuid();

            string sign = $"{uuid}{sn}{tpCustInfo.TpUserName}{Amount}{secretCode}";

            Match m = Regex.Match(TransID, @"\d+");
            long.TryParse(m.Value, out long bizId);
            bizId += 220000000000;

            if (IsDevelopment)
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
                    amount = Amount.ToString(),
                    bizId = bizId,
                    checkBizId = "1"
                },
                jsonrpc = "2.0",
            };

            var result = HttpPost(
                ApiUrl,
                JsonConvert.SerializeObject(Transfer),
                "application/json"
            );


            apiResult.RawMessage = "No Result";
            apiResult.RawErrorCode = "1";
            apiResult.ErrorCode = ErrorCode.TransferPending;
            apiResult.Data.Status = TransferStatus.Pending;
            apiResult.Data.ThirdId = bizId.ToString();

            if (!IsJSON(result))
            {
                return apiResult;
            }

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
                else if (error.error.code == "37131")
                {
                    // 玩家正在游戏中，不能下分
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
                return apiResult;
            }

            var json = JsonConvert.DeserializeObject<BalanceResponse>(result);
            if (json.error == null)
            {
                apiResult.ErrorCode = ErrorCode.Success;
                apiResult.Data.Status = TransferStatus.Success;
                apiResult.RawMessage = "";
                apiResult.RawErrorCode = "";
            }

            return apiResult;
        }

        public ApiResult Logout(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string secretCode = inputs.ContainsKey($"secretCode_{currency}") && !string.IsNullOrEmpty(inputs[$"secretCode_{currency}"]) ? inputs[$"secretCode_{currency}"] : throw new Exception($"secretCode_{currency}");
            string sn = inputs.ContainsKey("sn") && !string.IsNullOrEmpty(inputs["sn"]) ? inputs["sn"] : throw new Exception("sn");

            var uuid = Guid.NewGuid();

            string sign = $"{uuid}{sn}{tpCustInfo.TpUserName}{secretCode}";

            var Balance = new
            {
                id = uuid,
                method = "open.mega.user.logout",
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

            ApiResult oApiResult = new ApiResult();
            if (!IsJSON(result))
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var error = JsonConvert.DeserializeObject<ErrorResponse>(result);
            if (error.error != null)
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = error.error.message ?? "";
                oApiResult.RawErrorCode = error.error.code.ToString() ?? "";
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<LogoutResponse>(result);
            if (json.result == "1")
            {
                oApiResult.ErrorCode = ErrorCode.Success;
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
            }
            return oApiResult;
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpReportRng_Mega_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            var detail = ToJson(wagerDetail);
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.Casino => new CasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = "",
                    TpUserName = wagerDetail.GetValueOrDefault("Account")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("TransTime", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("SettleTime", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("Stake") * tpCurrency.ExchangeRate,
                    WinlostAmount = ((decimal)wagerDetail.GetValueOrDefault("WinLostAmount") * tpCurrency.ExchangeRate),
                    GameCode = "",
                    GameName = "",
                },
                _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                }
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                Title = tpGameType.TypeName,
            };
        }
    }
}
