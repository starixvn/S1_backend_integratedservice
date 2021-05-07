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
    public class TpHelper : ThirdPartyHelperBase, IThirdPartyParticular
    {
        private const string ContentType = "application/x-www-form-urlencoded";

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            var config = new NineWConfig(inputs);
            var user = GetUser(tpCustInfo);

            var apiUrl = $"https://{config.ApiHost}/apiWallet/{config.WebSite}/checkBalanceOperation";
            var sParam = $"cert={config.Cert}&userId={user}&tsCode={Guid.NewGuid()}";

            var json = HttpPost(apiUrl, sParam, ContentType);
            var result = FromJson<NineWResult<NineWTransfer>>(json);

            if (result.status != "1012"  || !result.results.Any(d => d.tscode == TransID && d.userId == user))
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
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = new NineWConfig(inputs);
            var apiUrl = $"https://{config.ApiHost}/apiWallet/{config.WebSite}/getBalance";
            var sParam = $"cert={config.Cert}&alluser=0&userIds={GetUser(tpCustInfo)}";
            
            var json = HttpPost(apiUrl, sParam, ContentType);
            var result = FromJson<NineWResult<NineWBalance>>(json);
            
            if (result.status != "1")
            {
                return new ApiResult<UserBalance>()
                {
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
                    balance = result.results[0].balance,
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
                throw new NotImplementedException();
            }

            var config = new NineWConfig(inputs);
            var user = GetUser(tpCustInfo);

            var key = HttpUtility.UrlEncode(GetKey(user, tpCustInfo.TpCurrencyId, config));
            var apiUrl = $"https://{config.ApiHost}/apiWallet/player/{config.WebSite}/login";
            var sParam = $"key={key}&userId={user}";
            var html = HttpPost(apiUrl, sParam, ContentType);

            if (IsJSON(html))//回傳json代表有錯誤
            {
                var result = FromJson<NineWResult<NineWTransfer>>(html);
                return new ApiResult()
                {
                    RawErrorCode = result.status,
                    RawMessage = html,
                };
            }

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = html
            };
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            var config = new NineWConfig(inputs);
            var user = GetUser(tpCustInfo);

            var apiUrl = $"https://{config.ApiHost}/apiWallet/{config.WebSite}/deposit";
            var sParam = $"cert={config.Cert}&userId={user}&balance={Amount}&tsCode={TransID}";

            if (direction == Direction.ToMain)
            {
                apiUrl = $"https://{config.ApiHost}/apiWallet/{config.WebSite}/withdraw";
                sParam = $"cert={config.Cert}&userId={user}&withdrawtype=0&balance={Amount}&tsCode={TransID}";
            }

            var json = HttpPost(apiUrl, sParam, ContentType);
            var result = FromJson<NineWResult<NineWTransfer>>(json);
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
            var apiUrl = $"https://{config.ApiHost}/apiWallet/player/{config.WebSite}/logout";
            var sParam = $"cert={config.Cert}&userIds={GetUser(tpCustInfo)}";

            var json = HttpPost(apiUrl, sParam, ContentType);
            var result = FromJson<NineWResult<string>>(json);

            if (result.status != "1")
            {
                return new ApiResult()
                {
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
            var apiUrl = $"https://{config.ApiHost}/apiWallet/player/{config.WebSite}/getKey";
            var sParam = $"&cert={config.Cert}&userId={user}&userName={user}&agent={config.Agent}&currency={currency}";
            var json = HttpPost(apiUrl, sParam, ContentType);
            var result = FromJson<NineWResult<string>>(json);
            return result.status == "1" ? result.key : throw new Exception($"9w get key error status is : {result.status}");
        }

        private string GetUser(TpCustInfo tpCustInfo)
        {
            return tpCustInfo.TpUserName.Replace("_", "");
        }

        private class NineWConfig
        {
            public string ApiHost { get; set; }
            public string WebSite { get; set; }
            public string Cert { get; set; }
            public string Agent { get; set; }

            public NineWConfig(Dictionary<string, string> inputs)
            {
                ApiHost = inputs.ContainsKey($"apihost") && !string.IsNullOrEmpty(inputs[$"apihost"])
                ? inputs[$"apihost"] : throw new Exception($"apihost is empty");
                WebSite = inputs.ContainsKey($"website") && !string.IsNullOrEmpty(inputs[$"website"])
                ? inputs[$"website"] : throw new Exception($"website is empty");
                Cert = inputs.ContainsKey($"cert") && !string.IsNullOrEmpty(inputs[$"cert"])
                ? inputs[$"cert"] : throw new Exception($"cert is empty");
                Agent = inputs.ContainsKey($"agent") && !string.IsNullOrEmpty(inputs[$"agent"])
                ? inputs[$"agent"] : throw new Exception($"agent is empty");
            }
        }

        private class NineWResult<T>
        {
            public string status { get; set; }
            public string key { get; set; }
            public List<T> results { get; set; }
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
            public int balance { get; set; }
            public string createdate { get; set; }
        }

    }
}
