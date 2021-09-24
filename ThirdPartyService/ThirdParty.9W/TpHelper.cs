using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.NineW
{
    public class TpHelper : ThirdPartyHelperBase, IThirdPartyParticular, IThirdPartyGetBalanceParticular
    {
        private const string ContentType = "application/x-www-form-urlencoded";

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            var config = new NineWConfig(inputs);

            var apiUrl = $"{config.ApiUrl}/{config.WebSite}/checkBalanceOperation";
            var sParam = $"cert={config.Cert}&userId={tpCustInfo.TpUserName}&tsCode={TransID}";

            var json = HttpPost(apiUrl, sParam, ContentType, timeOutTime: 300);
            var result = FromJson<NineWResult>(json);

            if (result.status != "1012"
                || !result.TransferResult.Any(d => d.tscode == TransID
                && d.userId.Equals(tpCustInfo.TpUserName, StringComparison.OrdinalIgnoreCase)
                && Math.Abs(d.balance) == Amount))
            {
                return new ApiResult<TransferResult>()
                {
                    ErrorCode = ErrorCode.TransferFail,
                    RawErrorCode = result.status,
                    RawMessage = json,
                    Data = new TransferResult()
                    {
                        Status = TransferStatus.Fail
                    }
                };
            }

            return new ApiResult<TransferResult>()
            {
                ErrorCode = ErrorCode.Success,
                Data = new TransferResult()
                {
                    Status = TransferStatus.Success
                }
            };
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = new NineWConfig(inputs);
            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x").ToLower();
            _ = GetKey(tpCustInfo.TpUserName, tpCustInfo.TpCurrencyId, config);
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = new NineWConfig(inputs);
            var apiUrl = $"{config.ApiUrl}/{config.WebSite}/getBalance";
            var sParam = $"cert={config.Cert}&alluser=0&userIds={tpCustInfo.TpUserName}";

            var json = HttpPost(apiUrl, sParam, ContentType);
            var result = FromJson<NineWResult>(json);

            if (result.status != "1")
            {
                return new ApiResult<UserBalance>()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = result.status,
                    RawMessage = json,
                };
            }

            return new ApiResult<UserBalance>()
            {
                ErrorCode = ErrorCode.Success,
                Message = "ok",
                Data = new UserBalance()
                {
                    balance = result.BalaceResult[0].balance,
                }
            };
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new TransIdModel
            {
                Implemented = false
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.UserNotLogin,
                    Message = "Not support fun game.",
                };
            }

            var config = new NineWConfig(inputs);

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = JsonConvert.SerializeObject(new
                {
                    url = $"{config.ApiUrl}/player/{config.WebSite}/login",
                    key = GetKey(tpCustInfo.TpUserName, tpCustInfo.TpCurrencyId, config),
                    userId = tpCustInfo.TpUserName,
                })
            };
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            var config = new NineWConfig(inputs);
            var apiUrl = $"{config.ApiUrl}/{config.WebSite}/deposit";
            var sParam = $"cert={config.Cert}&userId={tpCustInfo.TpUserName}&balance={Amount}&tsCode={TransID}";

            if (direction == Direction.ToMain)
            {
                apiUrl = $"{config.ApiUrl}/{config.WebSite}/withdraw";
                sParam = $"cert={config.Cert}&userId={tpCustInfo.TpUserName}&withdrawtype=0&balance={Amount}&tsCode={TransID}";
            }

            var json = HttpPost(apiUrl, sParam, ContentType);
            var result = FromJson<NineWResult>(json);
            if (result.status != "1")
            {
                return new ApiResult<TransferResult>()
                {
                    ErrorCode = ErrorCode.TransferPending,
                    RawErrorCode = result.status,
                    RawMessage = json,
                    Data = new TransferResult()
                    {
                        Status = TransferStatus.Pending
                    }
                };
            }

            return new ApiResult<TransferResult>()
            {
                ErrorCode = ErrorCode.Success,
                Data = new TransferResult()
                {
                    Status = TransferStatus.Success
                }
            };
        }

        public ApiResult Logout(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = new NineWConfig(inputs);
            var apiUrl = $"{config.ApiUrl}/player/{config.WebSite}/logout";
            var sParam = $"cert={config.Cert}&userIds={tpCustInfo.TpUserName}";

            var json = HttpPost(apiUrl, sParam, ContentType);
            var result = FromJson<NineWResult>(json);

            if (result.status != "1")
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = result.status,
                    RawMessage = json,
                };
            }

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        private string GetKey(string user, string currency, NineWConfig config)
        {
            var apiUrl = $"{config.ApiUrl}/player/{config.WebSite}/getKey";
            var sParam = $"&cert={config.Cert}&userId={user}&userName={user}&agent={config.Agent}&currency={currency}";
            var json = HttpPost(apiUrl, sParam, ContentType);
            var result = FromJson<NineWResult>(json);
            return result.status == "1"
                ? HttpUtility.UrlEncode(result.key) : throw new Exception($"9w get key error status is : {result.status}");
        }

        /// <summary>
        /// 取得指定客戶的餘額資料
        /// </summary>
        /// <param name="inputs">第三方相關參數</param>
        /// <param name="tpUserNameString">第三方使用者名稱參數清單</param>
        /// <returns></returns>
        public ApiResult<List<ReceiveTpUserBalanceModel>> GetBalance(Dictionary<string, string> inputs, string tpUserNameString)
        {
            var config = new NineWConfig(inputs);
            var apiUrl = $"{config.ApiUrl}/{config.WebSite}/getBalance";
            var sParam = $"cert={config.Cert}&alluser=0&userIds={tpUserNameString}";

            var json = HttpPost(apiUrl, sParam, ContentType, timeOutTime: 120);
            var result = FromJson<NineWResult>(json);

            if (result.status != "1")
            {
                return new ApiResult<List<ReceiveTpUserBalanceModel>>()
                {
                    ErrorCode = ErrorCode.Fail,
                    Message = "fail",
                    RawErrorCode = result.status,
                    RawMessage = json,
                };
            }

            return new ApiResult<List<ReceiveTpUserBalanceModel>>()
            {
                ErrorCode = ErrorCode.Success,
                Message = "ok",
                Data = result.BalaceResult.Select(x => new ReceiveTpUserBalanceModel
                {
                    TpUserName = x.userId,
                    Balance = x.balance,
                    ErrorCode = ErrorCode.Success
                }).ToList()
            };
        }

        private class NineWConfig
        {
            public string ApiUrl { get; set; }
            public string WebSite { get; set; }
            public string Cert { get; set; }
            public string Agent { get; set; }

            public NineWConfig(Dictionary<string, string> inputs)
            {
                ApiUrl = inputs.ContainsKey($"ApiUrl") && !string.IsNullOrEmpty(inputs[$"ApiUrl"])
                ? inputs[$"ApiUrl"] : throw new Exception($"apihost is empty");
                WebSite = inputs.ContainsKey($"website") && !string.IsNullOrEmpty(inputs[$"website"])
                ? inputs[$"website"] : throw new Exception($"website is empty");
                Cert = inputs.ContainsKey($"cert") && !string.IsNullOrEmpty(inputs[$"cert"])
                ? inputs[$"cert"] : throw new Exception($"cert is empty");
                Agent = inputs.ContainsKey($"agent") && !string.IsNullOrEmpty(inputs[$"agent"])
                ? inputs[$"agent"] : throw new Exception($"agent is empty");
            }
        }

        private class NineWResult
        {
            public string status { get; set; }
            public string key { get; set; }

            [JsonProperty("results")]
            public List<NineWBalance> BalaceResult { get; set; }

            [JsonProperty("result")]
            public List<NineWTransfer> TransferResult { get; set; }
        }

        private class NineWBalance
        {
            public string userId { get; set; }
            public decimal balance { get; set; }
            public decimal exposure { get; set; }
        }

        private class NineWTransfer
        {
            public string tscode { get; set; }
            public string userId { get; set; }
            public decimal balance { get; set; }
            public string createdate { get; set; }
        }

    }
}
