using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class V8Controller : ControllerBase
    {
        public enum SubType
        {
            GetBalance = 1001,
            RequestBet = 1002,
            ReturnBalance = 1003,
            GetOrderStatus = 1004,
            CancelBet = 1005,
        }

        // 当状态为 2 或 4 时，会进行重新派奬, 成功后修正订单为已处理
        public enum StatusType
        {
            // 成功
            Success = 1,
            // 失败
            Failure = 2,
            // 处理中
            Process = 3,
            // 查无订单
            NoResult = 4,
            // 其他错误
            OtherError = 5,
        }

        #region Request

        public class ApiReq
        {
            public string agent { get; set; }
            public long timestamp { get; set; }
            [JsonProperty("@param")]
            public string param { get; set; }
            public string key { get; set; }
        }

        public class CommonP
        {
            public int s { get; set; }
        }

        public class GetBalanceP : CommonP
        {
            public string account { get; set; }
        }

        #endregion

        #region Response

        public class GetApiResp
        {
            public int s { get; set; }
            public string m { get; set; }
            public info d { get; set; }
        }

        public class info
        {
            public int code { get; set; }
        }

        public class GetBalanceResp
        {
            public int code { get; set; }
            public decimal money { get; set; }
        }

        public class GetBalanceRespOfSeamless
        {
            public int s { get; set; }
            public string m { get; set; }
            public GetBalanceD d { get; set; }
        }

        public class PlaceBetResp : GetBalanceRespOfSeamless
        {
        }

        public class PayoutResp : GetBalanceRespOfSeamless
        {
        }

        public class GetOrderStatusResp
        {
            public int s { get; set; }
            public string m { get; set; }
            public GetOrderStatusD d { get; set; }
        }

        public class CancelBetResp : GetOrderStatusResp
        {
        }

        public class GetBalanceD
        {
            public int code { get; set; }
            public string account { get; set; }
            public decimal money { get; set; }
        }

        public class GetOrderStatusD
        {
            public int code { get; set; }
            public int status { get; set; }
        }

        #endregion

        #region ErrorCode

        public enum Error
        {
            Success = 0,
            // Insufficient balance
            Insufficient = 1,
            // Account does not exist
            Account = 2,
            // maintains
            Maintains = 3,
            // token validation error
            Token = 4,
            // data format error
            Format = 5,
            // decryption error
            Decryption = 6,
            // MD5 key error
            Md5 = 7,
            // unknown sub-type operation
            Subtype = 8,
            // Duplicate order
            Duplicate = 9,
            // Agent does not exist
            Agent = 10,
            // Account under deposit or withdrawn
            Transaction = 11,
        }

        #endregion

        private readonly ILogger<V8Controller> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly V8BLL _v8BLL;
        private readonly Log2File _log2File;
        private readonly Tools _tools;
        private readonly Tp _thisTp;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _apiUrl;
        private readonly int _gameType = (int)GameType.V8_Poker;

        public V8Controller(
            ILogger<V8Controller> logger,
            IServiceProvider provider,
            IHttpClientFactory clientFactory,
            IConfiguration configuration
        )
        {
            _logger = logger;
            _log2File = provider.GetRequiredService<Log2File>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _v8BLL = provider.GetRequiredService<V8BLL>();
            _tools = provider.GetRequiredService<Tools>();
            _thisTp = Tp.V8;
            _apiUrl = configuration.GetValue<string>("TpService");
            _clientFactory = clientFactory;

        }

        #region Transfer

        public class ApiResult<TData>
        {
            public ApiResult()
            {
                ErrorCode = 9000;
            }
            public int ErrorCode { get; set; }
            public string Message { get; set; }
            public TData Data { get; set; }
        }
        public class TransferResult
        {
            public TransferResult()
            {
                Status = 2;
            }
            public int Status { get; set; }
            public string ThirdId { get; set; }
            public decimal Difference { get; set; }
            public decimal Bonus { get; set; }
            public decimal Rollover { get; set; }
            public decimal Turnover { get; set; }
            public int BonusStatus { get; set; }
        }

        [HttpGet]
        [Route("[controller]/channelHandle")]
        public async Task<GetApiResp> channelHandleAsync([FromQuery] Dictionary<string, string> getReq)
        {
            string logName = $"{_thisTp}";
            var subName = "";
            var json = JsonConvert.SerializeObject(getReq);
            var apiReq = JsonConvert.DeserializeObject<ApiReq>(json);

            GetApiResp getApiResponse = new GetApiResp
            {
                s = 101,
                m = "/channelHandle",
                d = new info
                {
                    code = 999
                }
            };
            try
            {
                var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)_thisTp);
                var param = _tools.AesDecrypt(HttpUtility.UrlEncode(apiReq.param), thirdPartySetting.TpSetting["Deskey"]);
                var s = HttpUtility.ParseQueryString(param).Get("s");

                if (s == "11")
                {
                    subName = "notify";
                    _log2File.Info($"{json}\r\n{param}", $"{logName}_{subName}");

                    string account = HttpUtility.ParseQueryString(param).Get("account");

                    if (account.IndexOf("_") > -1)
                    {
                        account = account[(account.IndexOf("_") + 1)..];
                    }

                    var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, account);

                    /*Transfer In*/
                    var client = _clientFactory.CreateClient();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    Dictionary<string, string> input = new Dictionary<string, string>();
                    input.Add("AllOut", null);

                    var fooJSON = JsonConvert.SerializeObject(new
                    {
                        TpId = (int)_thisTp,
                        CustId = tpCustInfo.CustId,
                        Amount = 1,
                        direction = 2,
                        inputs = input
                    });

                    var fooContent = new StringContent(fooJSON, Encoding.UTF8, "application/json");
                    var myUrl = $"{_apiUrl}Transfer";
                    HttpResponseMessage response = await client.PostAsync(myUrl, fooContent);
                    ApiResult<TransferResult> oApiResult = new ApiResult<TransferResult>();

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        oApiResult = JsonConvert.DeserializeObject<ApiResult<TransferResult>>(responseBody);
                    }

                    if (oApiResult.ErrorCode == 0)
                    {
                        getApiResponse.d.code = 0;
                        _log2File.Info($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(oApiResult)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", $"{logName}_{subName}");
                    }
                    else
                    {
                        _log2File.Error($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(oApiResult)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", $"{logName}_{subName}");
                    }

                    return getApiResponse;
                }

                _log2File.Error($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getApiResponse)}", $"{logName}_{subName}");
                return getApiResponse;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex.Message}", logName, ex);
            }
            return getApiResponse;
        }

        [HttpGet]
        [Route("[controller]")]
        public async Task<GetBalanceResp> SeamlessWalletAsync([FromQuery] Dictionary<string, string> getReq)
        {
            string logName = $"{_thisTp}";
            var subName = "";
            var json = JsonConvert.SerializeObject(getReq);
            var apiReq = JsonConvert.DeserializeObject<ApiReq>(json);

            GetBalanceResp getBalanceResponse = new GetBalanceResp();
            getBalanceResponse.code = 999;

            var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)_thisTp);
            var param = _tools.AesDecrypt(HttpUtility.UrlEncode(apiReq.param), thirdPartySetting.TpSetting["Deskey"]);
            var s = HttpUtility.ParseQueryString(param).Get("s");

            if (s == "12")
            {
                subName = "balance";
                var account = HttpUtility.ParseQueryString(param).Get("account");
                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, account);
                if (tpCheck == null || tpCheck.Locked)
                {
                    getBalanceResponse.code = 4;
                    _log2File.Error($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}", $"{logName}_{subName}");
                    return getBalanceResponse;
                }

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, account);
                if (tpCustBalance != null)
                {
                    getBalanceResponse.code = 0;
                    getBalanceResponse.money = tpCustBalance.Balance;
                    _log2File.Info($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}", $"{logName}_{subName}");
                }
                else
                {
                    _log2File.Error($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}", $"{logName}_{subName}");
                }
                return getBalanceResponse;
            }
            else if (s == "13")
            {
                subName = "TransferOut";
                var account = HttpUtility.ParseQueryString(param).Get("account");
                var money = HttpUtility.ParseQueryString(param).Get("money");
                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, account);
                if (tpCheck == null || tpCheck.Locked)
                {
                    getBalanceResponse.code = 4;
                    _log2File.Error($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}", $"{logName}_{subName}");
                    return getBalanceResponse;
                }

                try
                {
                    var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, account);
                    /*Transfer Out*/

                    var client = _clientFactory.CreateClient();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var fooJSON = JsonConvert.SerializeObject(new
                    {
                        TpId = (int)_thisTp,
                        CustId = tpCustInfo.CustId,
                        Amount = Convert.ToDecimal(money),
                        direction = 1
                    });

                    var fooContent = new StringContent(fooJSON, Encoding.UTF8, "application/json");
                    var myUrl = $"{_apiUrl}Transfer";
                    HttpResponseMessage response = await client.PostAsync(myUrl, fooContent);
                    ApiResult<TransferResult> oApiResult = new ApiResult<TransferResult>();

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        oApiResult = JsonConvert.DeserializeObject<ApiResult<TransferResult>>(responseBody);
                    }

                    if (oApiResult.ErrorCode == 0)
                    {
                        getBalanceResponse.code = 0;
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, account);
                        getBalanceResponse.money = tpCustBalance.Balance;
                        _log2File.Info($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}\r\n{JsonConvert.SerializeObject(oApiResult)}", $"{logName}_{subName}");
                    }
                    else if (oApiResult.Data.Difference != 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, account);
                        getBalanceResponse.code = 1005;
                        getBalanceResponse.money = tpCustBalance.Balance;
                        _log2File.Error($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}\r\n{JsonConvert.SerializeObject(oApiResult)}", $"{logName}_{subName}");
                    }
                    else
                    {
                        _log2File.Error($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}\r\n{response.ReasonPhrase}", $"{logName}_{subName}");
                    }
                    return getBalanceResponse;
                }
                catch (Exception ex)
                {
                    _log2File.Error($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}\r\n{ex.Message}", $"{logName}_{subName}", ex);
                    return getBalanceResponse;
                }
            }

            _log2File.Error($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}", $"{logName}_{subName}");
            return getBalanceResponse;
        }

        #endregion

        #region Seamless

        [HttpGet]
        [Route("[controller]/seamlesswallet")]
        public dynamic SeamlessWalletAsync([FromQuery] ApiReq getReq)
        {
            string logName = $"{_thisTp}";
            GetApiResp getApiResponse = new GetApiResp
            {
                s = 0,
                m = "/channelHandle",
                d = new info
                {
                    code = 999
                }
            };
            try
            {
                if (string.IsNullOrEmpty(getReq.agent))
                {
                    getApiResponse.d.code = (int)Error.Agent;
                    _log2File.Error($"Agent Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                if (string.IsNullOrEmpty(getReq.param))
                {
                    getApiResponse.d.code = (int)Error.Decryption;
                    _log2File.Error($"Decryption Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var param = DecryptParam(getReq.param);

                if (string.IsNullOrEmpty(param))
                {
                    getApiResponse.d.code = (int)Error.Decryption;
                    _log2File.Error($"Decryption Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var query = HttpUtility.ParseQueryString(param);
                var json = JsonConvert.SerializeObject(query.AllKeys.ToDictionary(y => y, y => query[y]));
                _log2File.Info($"{json}", logName);
                var req = JsonConvert.DeserializeObject<CommonP>(json);
                getApiResponse.s = req.s;

                if (!Enum.IsDefined(typeof(SubType), req.s))
                {
                    getApiResponse.d.code = (int)Error.Subtype;
                    _log2File.Error($"Subtype Error {req.s}", logName);
                    return getApiResponse;
                }

                var verifyKey = false;
                if (!string.IsNullOrEmpty(getReq.key))
                {
                    verifyKey = CheckKey(getReq.key, getReq.timestamp);
                }
                if (!verifyKey)
                {
                    getApiResponse.d.code = (int)Error.Md5;
                    _log2File.Error($"CheckKey Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                switch (req.s)
                {
                    case (int)SubType.GetBalance:
                        return GetBalance(json);
                    case (int)SubType.RequestBet:
                        return PlaceBet(getReq, json);
                    case (int)SubType.ReturnBalance:
                        return Payout(getReq, json);
                    case (int)SubType.GetOrderStatus:
                        return GetOrderStatus(getReq, json);
                    case (int)SubType.CancelBet:
                        return CancelBet(getReq, json);
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            _log2File.Error($"Unknow Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        private dynamic GetBalance(string reqParam)
        {
            string logName = $"{_thisTp}_getBalance";
            var subType = SubType.GetBalance;
            GetApiResp getApiResponse = new GetApiResp
            {
                s = (int)subType,
                m = "/channelHandle",
                d = new info
                {
                    code = 999
                }
            };

            var req = JsonConvert.DeserializeObject<GetBalanceP>(reqParam);

            if (req.s != (int)subType)
            {
                getApiResponse.d.code = (int)Error.Subtype;
                _log2File.Error($"Subtype Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            if (req.account == null)
            {
                getApiResponse.d.code = (int)Error.Account;
                _log2File.Error($"Account Empty {JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, req.account);
            if (tpCustBalance != null)
            {
                GetBalanceRespOfSeamless getBalanceResponse = new()
                {
                    s = (int)subType,
                    m = "/channelHandle",
                    d = new GetBalanceD
                    {
                        code = (int)Error.Success,
                        account = req.account,
                        money = tpCustBalance.Balance
                    }
                };
                return getBalanceResponse;
            }

            _log2File.Error($"Unknow Error {JsonConvert.SerializeObject(req)} {JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        private dynamic PlaceBet(ApiReq getReq, string reqParam)
        {
            var result = 0;
            string logName = $"{_thisTp}_PlaceBet";
            var subType = SubType.RequestBet;
            GetApiResp getApiResponse = new GetApiResp
            {
                s = (int)subType,
                m = "/channelHandle",
                d = new info
                {
                    code = 999
                }
            };

            try
            {
                var req = JsonConvert.DeserializeObject<BettingDataPK_V8>(reqParam);
                _log2File.Info($"{JsonConvert.SerializeObject(req)}", logName);

                req.timestamp = getReq.timestamp;

                if (req.s != (int)subType)
                {
                    getApiResponse.d.code = (int)Error.Subtype;
                    _log2File.Error($"Subtype Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                if (req.account == null)
                {
                    getApiResponse.d.code = (int)Error.Account;
                    _log2File.Error($"Account Empty {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, req.account);
                if (tpCheck == null)
                {
                    getApiResponse.d.code = (int)Error.Account;
                    _log2File.Error($"{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (tpCheck.Locked)
                {
                    getApiResponse.d.code = (int)Error.Account;
                    _log2File.Error($"Locked {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                result = _v8BLL.PlaceBetPK_V8(req, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0 || result == -1)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, req.account, _gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    PlaceBetResp getPlaceBetResponse = new()
                    {
                        s = (int)subType,
                        m = "/channelHandle",
                        d = new GetBalanceD
                        {
                            code = (int)Error.Success,
                            account = req.account,
                            money = tpAffterAmount
                        }
                    };
                    return getPlaceBetResponse;
                }
                else if (tpAffterAmount < 0)
                {
                    getApiResponse.d.code = (int)Error.Insufficient;
                    _log2File.Error($"{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            _log2File.Error($"Unknow Error result={result} {JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        private dynamic Payout(ApiReq getReq, string reqParam)
        {
            var result = 0;
            string logName = $"{_thisTp}_Payout";
            var subType = SubType.ReturnBalance;
            GetApiResp getApiResponse = new GetApiResp
            {
                s = (int)subType,
                m = "/channelHandle",
                d = new info
                {
                    code = 999
                }
            };

            try
            {
                var req = JsonConvert.DeserializeObject<BettingDataPK_V8>(reqParam);
                _log2File.Info($"{JsonConvert.SerializeObject(req)}", logName);

                req.timestamp = getReq.timestamp;

                if (req.s != (int)subType)
                {
                    getApiResponse.d.code = (int)Error.Subtype;
                    _log2File.Error($"Subtype Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                if (req.account == null)
                {
                    getApiResponse.d.code = (int)Error.Account;
                    _log2File.Error($"Account Empty {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                result = _v8BLL.PayoutPK_V8(req, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0 || result == -1)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, req.account, _gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    PayoutResp getPayoutResponse = new()
                    {
                        s = (int)subType,
                        m = "/channelHandle",
                        d = new GetBalanceD
                        {
                            code = (int)Error.Success,
                            account = req.account,
                            money = tpAffterAmount
                        }
                    };
                    return getPayoutResponse;
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            _log2File.Error($"Unknow Error result={result} {JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        private dynamic CancelBet(ApiReq getReq, string reqParam)
        {
            var result = 0;
            string logName = $"{_thisTp}_CancelBet";
            var subType = SubType.CancelBet;
            CancelBetResp getApiResponse = new()
            {
                s = (int)subType,
                m = "/channelHandle",
                d = new GetOrderStatusD
                {
                    code = 999,
                    status = (int)StatusType.Failure
                }
            };

            try
            {
                var req = JsonConvert.DeserializeObject<BettingDataPK_V8>(reqParam);
                _log2File.Info($"{JsonConvert.SerializeObject(req)}", logName);

                req.timestamp = getReq.timestamp;

                if (req.s != (int)subType)
                {
                    getApiResponse.d.code = (int)Error.Subtype;
                    _log2File.Error($"Subtype Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                if (req.account == null)
                {
                    getApiResponse.d.code = (int)Error.Account;
                    _log2File.Error($"Account Empty {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                result = _v8BLL.CancelBetPK_V8(req, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    getApiResponse.d.code = (int)Error.Success;
                    getApiResponse.d.status = (int)StatusType.Success;
                    return getApiResponse;
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            _log2File.Error($"Unknow Error result={result} {JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        private dynamic GetOrderStatus(ApiReq getReq, string reqParam)
        {
            string logName = $"{_thisTp}_GetOrderStatus";
            var subType = SubType.GetOrderStatus;
            GetOrderStatusResp getApiResponse = new()
            {
                s = (int)subType,
                m = "/channelHandle",
                d = new GetOrderStatusD
                {
                    code = 999,
                    status = (int)StatusType.OtherError,
                }
            };

            try
            {
                var req = JsonConvert.DeserializeObject<OrderDataPK_V8>(reqParam);
                _log2File.Info($"{JsonConvert.SerializeObject(req)}", logName);

                req.timestamp = getReq.timestamp;

                if (req.s != (int)subType)
                {
                    getApiResponse.d.code = (int)Error.Subtype;
                    _log2File.Error($"Subtype Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var bettrans = _v8BLL.GetRecordPK_V8(req);
                if (!bettrans.Any() || bettrans == null)
                {
                    // 4:查无订单
                    getApiResponse.d.code = (int)Error.Success;
                    getApiResponse.d.status = (int)StatusType.NoResult;
                    return getApiResponse;
                }
                else if (bettrans.Any())
                {
                    // 1:成功
                    getApiResponse.d.code = (int)Error.Success;
                    getApiResponse.d.status = (int)StatusType.Success;
                    return getApiResponse;
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            _log2File.Error($"Unknow Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        private string DecryptParam(string getReq)
        {
            var param = getReq;

            try
            {
                var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)_thisTp);
                var deskey = thirdPartySetting.TpSetting["Deskey_SW"];

                param = _tools.AesDecrypt(HttpUtility.UrlEncode(getReq), deskey);

                _logger.LogInformation(param);
            }
            catch (Exception)
            {
                _logger.LogError($"DecryptParam Error\r\nkey={getReq}");
            }

            return param;
        }

        private bool CheckKey(string key, long timestamp)
        {
            var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)_thisTp);
            var md5key = thirdPartySetting.TpSetting["Md5key_SW"];
            var agent = thirdPartySetting.TpSetting["Agent_SW"];

            var encryptKey = _tools.ToMD5($"{agent}{timestamp}{md5key}").ToLower();
            if (key.ToLower() == encryptKey)
            {
                return true;
            }

            _logger.LogError($"CheckKey Error\r\nkey={key}\r\nencryptKey={encryptKey}");
            return false;
        }

        #endregion
    }
}
