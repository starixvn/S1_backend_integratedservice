using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class MPController : ControllerBase
    {
        private readonly ILogger<MPController> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Log2File _log2File;
        private readonly Tools _tools;
        private readonly Tp _thisTp;
        private readonly Dictionary<string, MPConfig> _siteConfigDic;
        private readonly MPConfig _siteConfig;

        public MPController(IServiceProvider provider, ILogger<MPController> logger)
        {
            _logger = logger;
            _log2File = provider.GetRequiredService<Log2File>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _thisTp = Tp.MP;
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteName<MPConfig>(Tp.MP.ToString());
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<MPConfig>(Tp.MP.ToString());
        }

        #region HttpStatusCode

        public enum Error
        {
            OK = 200,
            INVALID_DATA = 400,
            UAUTHORIZED = 401,
            ERROR = 500,
        }

        #endregion

        #region Request

        public class ApiReq
        {
            public string agent { get; set; }
            public string channelId { get; set; }
            public string account { get; set; }
            public long timestamp { get; set; }
            [JsonProperty("param")]
            public string param { get; set; }
            public string key { get; set; }
        }

        #endregion

        #region Response

        [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
        public class CheckResp
        {
            public int StatusCode { get; set; }
            public string Result { get; set; }
            public int? Code { get; set; }
            public string channelId { get; set; }
            public string account { get; set; }
            public string requestAmount { get; set; }
            public string gameId { get; set; }
            public string orderId { get; set; }
            public string roundId { get; set; }
            public string gameNo { get; set; }
            public string playerId { get; set; }
            public decimal totalBet { get; set; }
            public decimal validBet { get; set; }
            public int betCount { get; set; }
            public decimal totalWithdraw { get; set; }
            public decimal revenue { get; set; }
            public long timestamp { get; set; }
        }

        #endregion

        [HttpGet]
        [Route("[controller]/getBalance1")]
        [Route("[controller]/{sitename}/getBalance1")]
        public dynamic GetBalance1([FromQuery] Dictionary<string, string> getReq, [FromRoute] string sitename = "")
        {
            Response.StatusCode = (int)Error.ERROR;
            var Fatal = new
            {
                code = (int)MPErrorCode.Error
            };

            string logName = $"{_thisTp}_getBalance1";
            var Content = JsonConvert.SerializeObject(getReq);
            var apiReq = JsonConvert.DeserializeObject<ApiReq>(Content);
            var encrypt = apiReq.param;

            var config = string.IsNullOrWhiteSpace(sitename) ? _siteConfig : _siteConfigDic[sitename];

            string Agent = config.Agent;
            string Deskey = config.Deskey;

            string sResult;
            var check = CheckAuthorization(encrypt, Deskey, Agent, true);
            if (check.StatusCode != (int)Error.OK)
            {
                // 參數不符、簽名、解密失敗 Body留空只傳Http Status Code
                Response.StatusCode = check.StatusCode;
                _log2File.Error($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}", logName);
                return check.Result;
            }

            var balance = JsonConvert.DeserializeObject<BalanceData_MP>(JsonConvert.SerializeObject(check));

            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, balance.playerId);
            if (tpCustBalance != null)
            {
                Response.StatusCode = (int)Error.OK;
                var Param = new
                {
                    channelId = Agent,
                    balance.account,
                    balance = tpCustBalance.Balance
                };

                sResult = DoEncrypt(Param, Deskey);
                _log2File.Info($"Response Decrypt: {Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Param)}", logName);
                return sResult;
            }

            sResult = DoEncrypt(Fatal, Deskey);
            _log2File.Error($"Response Decrypt: {Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Fatal)}", logName);
            return sResult;
        }

        [HttpPost]
        [Route("[controller]/withdraw")]
        [Route("[controller]/{sitename}/withdraw")]
        public async Task<dynamic> WithdrawAsync([FromQuery] Dictionary<string, string> getReq, [FromRoute] string sitename = "")
        {
            Response.StatusCode = (int)Error.ERROR;
            var Fatal = new
            {
                code = (int)MPErrorCode.Error
            };

            string sResult;
            string logName = $"{_thisTp}_withdraw";
            var Content = JsonConvert.SerializeObject(getReq);
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var encrypt = await reader.ReadToEndAsync();

            var config = string.IsNullOrWhiteSpace(sitename) ? _siteConfig : _siteConfigDic[sitename];

            string Agent = config.Agent;
            string Deskey = config.Deskey;

            var check = CheckAuthorization(encrypt, Deskey, Agent, true);
            if (check.StatusCode != (int)Error.OK)
            {
                // 參數不符、簽名、解密失敗 Body留空只傳Http Status Code
                Response.StatusCode = check.StatusCode;
                _log2File.Error($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}", logName);
                return check.Result;
            }

            try
            {
                var transfer = JsonConvert.DeserializeObject<TransferData_MP>(JsonConvert.SerializeObject(check));
                var result = _bettingBLL.MoneyOutPK_MP(transfer, out decimal tpAffterAmount);
                if (result > 0)
                {
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = _tpCustInfoBLL.GetBalance((int)_thisTp, transfer.playerId).Balance;
                    }

                    Response.StatusCode = (int)Error.OK;
                    var Param = new
                    {
                        channelId = Agent,
                        transfer.account,
                        balance = tpAffterAmount
                    };

                    sResult = DoEncrypt(Param, Deskey);
                    _log2File.Info($"Response Decrypt: {Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Param)}", logName);
                    return sResult;
                }
                else if (result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, transfer.playerId);

                    Response.StatusCode = (int)Error.INVALID_DATA;
                    var Param = new
                    {
                        code = (int)MPErrorCode.DuplicateRequest,
                        balance = tpCustBalance.Balance
                    };

                    sResult = DoEncrypt(Param, Deskey);
                    _log2File.Error($"Response Decrypt: {Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Param)}", logName);
                    return sResult;
                }
                else if (tpAffterAmount < 0)
                {
                    Response.StatusCode = (int)Error.INVALID_DATA;
                    var Param = new
                    {
                        code = (int)MPErrorCode.BalanceInsufficient
                    };

                    sResult = DoEncrypt(Param, Deskey);
                    _log2File.Error($"Response Decrypt: {Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Param)}", logName);
                    return sResult;
                }

                sResult = DoEncrypt(Fatal, Deskey);
                _log2File.Error($"Response Decrypt: {Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Fatal)}", logName);
                return sResult;
            }
            catch (Exception ex)
            {
                sResult = DoEncrypt(Fatal, Deskey);
                _log2File.Error($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Fatal)}\r\n{ex.Message}", logName, ex);
                return sResult;
            }
        }

        [HttpPost]
        [Route("[controller]/deposit")]
        [Route("[controller]/{sitename}/deposit")]
        public async Task<dynamic> DepositAsync([FromQuery] Dictionary<string, string> getReq, [FromRoute] string sitename = "")
        {
            Response.StatusCode = (int)Error.ERROR;
            var Fatal = new
            {
                code = (int)MPErrorCode.Error
            };

            string sResult;
            string logName = $"{_thisTp}_deposit";
            var Content = JsonConvert.SerializeObject(getReq);
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var encrypt = await reader.ReadToEndAsync();

            var config = string.IsNullOrWhiteSpace(sitename) ? _siteConfig : _siteConfigDic[sitename];

            string Agent = config.Agent;
            string Deskey = config.Deskey;

            var check = CheckAuthorization(encrypt, Deskey, Agent, false);
            if (check.StatusCode != (int)Error.OK)
            {
                // 參數不符、簽名、解密失敗 Body留空只傳Http Status Code
                Response.StatusCode = check.StatusCode;
                _log2File.Error($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}", logName);
                return check.Result;
            }

            try
            {
                var transfer = JsonConvert.DeserializeObject<TransferData_MP>(JsonConvert.SerializeObject(check));
                var result = _settledBLL.MoneyInPK_MP(transfer, out decimal tpAffterAmount);
                if (result > 0)
                {
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = _tpCustInfoBLL.GetBalance((int)_thisTp, transfer.playerId).Balance;
                    }

                    Response.StatusCode = (int)Error.OK;
                    var Param = new
                    {
                        channelId = Agent,
                        transfer.account,
                        balance = tpAffterAmount
                    };

                    sResult = DoEncrypt(Param, Deskey);
                    _log2File.Info($"Response Decrypt: {Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Param)}", logName);
                    return sResult;
                }
                else if (result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, transfer.playerId);

                    Response.StatusCode = (int)Error.INVALID_DATA;
                    var Param = new
                    {
                        code = (int)MPErrorCode.DuplicateRequest,
                        balance = tpCustBalance.Balance
                    };

                    sResult = DoEncrypt(Param, Deskey);
                    _log2File.Error($"Response Decrypt: {Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Param)}", logName);
                    return sResult;
                }
                else if (tpAffterAmount < 0)
                {
                    Response.StatusCode = (int)Error.INVALID_DATA;
                    var Param = new
                    {
                        code = (int)MPErrorCode.BalanceInsufficient
                    };

                    sResult = DoEncrypt(Param, Deskey);
                    _log2File.Error($"Response Decrypt: {Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Param)}", logName);
                    return sResult;
                }

                sResult = DoEncrypt(Fatal, Deskey);
                _log2File.Error($"Response Decrypt: {Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Fatal)}", logName);
                return sResult;
            }
            catch (Exception ex)
            {
                sResult = DoEncrypt(Fatal, Deskey);
                _log2File.Error($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Fatal)}\r\n{ex.Message}", logName, ex);
                return sResult;
            }
        }

        [HttpPost]
        [Route("[controller]/cancel")]
        [Route("[controller]/{sitename}/cancel")]
        public async Task<dynamic> CancelAsync([FromQuery] Dictionary<string, string> getReq, [FromRoute] string sitename = "")
        {
            Response.StatusCode = (int)Error.ERROR;
            var Fatal = new
            {
                code = (int)MPErrorCode.Error
            };

            string sResult;
            string logName = $"{_thisTp}_cancel";
            var Content = JsonConvert.SerializeObject(getReq);
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var encrypt = await reader.ReadToEndAsync();

            var config = string.IsNullOrWhiteSpace(sitename) ? _siteConfig : _siteConfigDic[sitename];

            string Agent = config.Agent;
            string Deskey = config.Deskey;

            var check = CheckAuthorization(encrypt, Deskey, Agent, false);
            if (check.StatusCode != (int)Error.OK)
            {
                // 參數不符、簽名、解密失敗 Body留空只傳Http Status Code
                Response.StatusCode = check.StatusCode;
                _log2File.Error($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}", logName);
                return check.Result;
            }

            try
            {
                var transfer = JsonConvert.DeserializeObject<TransferData_MP>(JsonConvert.SerializeObject(check));
                var result = _settledBLL.CancelMoneyInPK_MP(transfer, out decimal tpAffterAmount);
                if (result > 0)
                {
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = _tpCustInfoBLL.GetBalance((int)Tp.MP, transfer.playerId).Balance;
                    }

                    Response.StatusCode = (int)Error.OK;
                    var Param = new
                    {
                        isCanceled = true,
                        channelId = Agent,
                        transfer.account,
                        balance = tpAffterAmount
                    };

                    sResult = DoEncrypt(Param, Deskey);
                    _log2File.Info($"Response Decrypt: {Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Param)}", logName);
                    return sResult;
                }
                else if (result == -1 || result == -2)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, transfer.playerId);

                    Response.StatusCode = (int)Error.INVALID_DATA;
                    var Param = new
                    {
                        code = (result == -1) ? (int)MPErrorCode.DuplicateRequest : (int)MPErrorCode.InvalidOrder,
                        balance = tpCustBalance.Balance
                    };

                    sResult = DoEncrypt(Param, Deskey);
                    _log2File.Error($"Response Decrypt: {Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Param)}", logName);
                    return sResult;
                }

                sResult = DoEncrypt(Fatal, Deskey);
                _logger.LogError($"Response Decrypt: {JsonConvert.SerializeObject(Fatal)}");
                _log2File.Error($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Fatal)}", logName);
                return sResult;
            }
            catch (Exception ex)
            {
                sResult = DoEncrypt(Fatal, Deskey);
                _log2File.Error($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Fatal)}\r\n{ex.Message}", logName, ex);
                return sResult;
            }
        }

        private CheckResp CheckAuthorization(string param, string desKey, string agent, bool checkOnline)
        {
            CheckResp resp = new()
            {
                StatusCode = (int)Error.OK,
                Result = ""
            };
            try
            {
                var decrypt = _tools.AesDecrypt(HttpUtility.UrlEncode(param), desKey);
                var jsonParsedArray = JObject.Parse(decrypt);
                _logger.LogInformation($"Request Decrypt: {JsonConvert.SerializeObject(jsonParsedArray)}");

                var requestAmount = jsonParsedArray["requestAmount"]?.ToString();
                var decryptRequest = JsonConvert.DeserializeObject<TransferData_MP>(JsonConvert.SerializeObject(jsonParsedArray));

                if (string.IsNullOrEmpty(decryptRequest.channelId) || decryptRequest.channelId != agent)
                {
                    resp.StatusCode = (int)Error.UAUTHORIZED;
                    _logger.LogError($"Request Check: {JsonConvert.SerializeObject(resp)}");
                    return resp;
                }

                if (string.IsNullOrEmpty(decryptRequest.account))
                {
                    var Param = new
                    {
                        code = (int)MPErrorCode.InvalidAccount
                    };
                    resp.StatusCode = (int)Error.INVALID_DATA;
                    resp.Result = DoEncrypt(Param, desKey);
                    resp.Code = Param.code;
                    _logger.LogError($"Request Check: {JsonConvert.SerializeObject(resp)}");
                    return resp;
                }

                if (checkOnline)
                {
                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, decryptRequest.playerId);
                    if (tpCheck == null || tpCheck.Locked)
                    {
                        var Param = new
                        {
                            code = (int)MPErrorCode.InvalidAccount
                        };
                        resp.StatusCode = (int)Error.INVALID_DATA;
                        resp.Result = DoEncrypt(Param, desKey);
                        resp.Code = Param.code;
                        _logger.LogError($"Request Check: {JsonConvert.SerializeObject(resp)}");
                        return resp;
                    }
                }

                if (!string.IsNullOrEmpty(requestAmount))
                {
                    decimal amount = decryptRequest.requestAmount;
                    if (amount < 0)
                    {
                        var Param = new
                        {
                            code = (int)MPErrorCode.InvalidRequestAmount
                        };
                        resp.StatusCode = (int)Error.INVALID_DATA;
                        resp.Result = DoEncrypt(Param, desKey);
                        resp.Code = Param.code;
                        _logger.LogError($"Request Check: {JsonConvert.SerializeObject(resp)}\r\nrequestAmount={decryptRequest.requestAmount}");
                        return resp;
                    }
                }

                resp.channelId = decryptRequest.channelId;
                resp.account = decryptRequest.account;
                resp.requestAmount = requestAmount;
                resp.gameId = decryptRequest.gameId;
                resp.roundId = decryptRequest.roundId;
                resp.gameNo = decryptRequest.gameNo;
                resp.orderId = decryptRequest.orderId;
                resp.playerId = decryptRequest.playerId;
                resp.totalBet = decryptRequest.totalBet;
                resp.validBet = decryptRequest.validBet;
                resp.betCount = decryptRequest.betCount;
                resp.totalWithdraw = decryptRequest.totalWithdraw;
                resp.revenue = decryptRequest.revenue;
                resp.timestamp = decryptRequest.timestamp;
                _logger.LogInformation($"Request Check: {JsonConvert.SerializeObject(resp)}");
                return resp;
            }
            catch (CryptographicException ex)
            {
                resp.StatusCode = (int)Error.UAUTHORIZED;
                _logger.LogError(ex, ex.Message);
                return resp;
            }
            catch (Exception ex)
            {
                resp.StatusCode = (int)Error.ERROR;
                _logger.LogError(ex, ex.Message);
                return resp;
            }
        }

        private string DoEncrypt(object param, string desKey)
        {
            var sParam = JsonConvert.SerializeObject(param);
            return HttpUtility.UrlDecode(_tools.AesEncrypt(sParam, desKey));
        }
    }
}
