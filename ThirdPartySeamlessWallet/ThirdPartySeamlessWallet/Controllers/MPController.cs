using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Log2File _log2File;
        private readonly Tools _tools;
        private readonly Tp thisTp;

        public MPController(
            IServiceProvider provider,
            ILogger<MPController> logger,
            IConfiguration configuration,
            IHostEnvironment environment
        )
        {
            _logger = logger;
            _log2File = provider.GetRequiredService<Log2File>();
            _configuration = configuration;
            _environment = environment;
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tools = provider.GetRequiredService<Tools>();
            thisTp = Tp.MP;
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
        }

        #endregion

        [HttpGet]
        [Route("[controller]/getBalance1")]
        public dynamic GetBalance1([FromQuery] Dictionary<string, string> getReq)
        {
            Response.StatusCode = (int)Error.ERROR;
            var Fatal = new
            {
                code = 500
            };

            string logName = $"{thisTp}_getBalance1";
            var Content = JsonConvert.SerializeObject(getReq);
            var apiReq = JsonConvert.DeserializeObject<ApiReq>(Content);
            var encrypt = apiReq.param;

            var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            string Agent = thirdPartySetting.TpSetting["Agent"];
            string Deskey = thirdPartySetting.TpSetting["Deskey"];

            string sResult;
            var check = checkAuthorization(encrypt, Deskey, Agent, true);
            if (check.StatusCode != (int)Error.OK)
            {
                // 參數不符、簽名、解密失敗 Body留空只傳Http Status Code
                Response.StatusCode = check.StatusCode;
                _log2File.Error($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}", logName);
                return check.Result;
            }

            string account = check.account;
            string[] acc = account.Split('_');
            string player = acc[1];

            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, player);
            if (tpCustBalance != null)
            {
                Response.StatusCode = (int)Error.OK;
                var Param = new
                {
                    channelId = Agent,
                    account = account,
                    balance = tpCustBalance.Balance
                };

                sResult = DoEncrypt(Param, Deskey);
                _logger.LogInformation($"Response Decrypt: {JsonConvert.SerializeObject(Param)}");
                _log2File.Info($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Param)}", logName);
                return sResult;
            }

            sResult = DoEncrypt(Fatal, Deskey);
            _logger.LogError($"Response Decrypt: {JsonConvert.SerializeObject(Fatal)}");
            _log2File.Error($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Fatal)}", logName);
            return sResult;
        }

        [HttpPost]
        [Route("[controller]/withdraw")]
        public async Task<dynamic> WithdrawAsync([FromQuery] Dictionary<string, string> getReq)
        {
            Response.StatusCode = (int)Error.ERROR;
            var Fatal = new
            {
                code = 500
            };

            string sResult;
            string logName = $"{thisTp}_withdraw";
            var Content = JsonConvert.SerializeObject(getReq);
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var encrypt = await reader.ReadToEndAsync();

            var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            string Agent = thirdPartySetting.TpSetting["Agent"];
            string Deskey = thirdPartySetting.TpSetting["Deskey"];

            var check = checkAuthorization(encrypt, Deskey, Agent, true);
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

                if (_environment.IsDevelopment() && transfer.requestAmount >= 300)
                {
                    System.Threading.Thread.Sleep(60000);
                    sResult = DoEncrypt(Fatal, Deskey);
                    _log2File.Error($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Fatal)}", logName);
                    return sResult;
                }

                var result = _bettingBLL.MoneyOutPK_MP(transfer, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = _tpCustInfoBLL.GetBalance((int)thisTp, transfer.player).Balance;
                    }

                    Response.StatusCode = (int)Error.OK;
                    var Param = new
                    {
                        channelId = Agent,
                        account = transfer.account,
                        balance = tpAffterAmount
                    };

                    sResult = DoEncrypt(Param, Deskey);
                    _logger.LogInformation($"Response Decrypt: {JsonConvert.SerializeObject(Param)}");
                    _log2File.Info($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Param)}", logName);
                    return sResult;
                }
                else if (tpAffterAmount < 0)
                {
                    Response.StatusCode = (int)Error.INVALID_DATA;
                    var Param = new
                    {
                        code = 138
                    };

                    sResult = DoEncrypt(Param, Deskey);
                    _logger.LogError($"Response Decrypt: {JsonConvert.SerializeObject(Param)}");
                    _log2File.Error($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Param)}", logName);
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

        [HttpPost]
        [Route("[controller]/deposit")]
        public async Task<dynamic> DepositAsync([FromQuery] Dictionary<string, string> getReq)
        {
            Response.StatusCode = (int)Error.ERROR;
            var Fatal = new
            {
                code = 500
            };

            string sResult;
            string logName = $"{thisTp}_deposit";
            var Content = JsonConvert.SerializeObject(getReq);
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var encrypt = await reader.ReadToEndAsync();

            var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            string Agent = thirdPartySetting.TpSetting["Agent"];
            string Deskey = thirdPartySetting.TpSetting["Deskey"];

            var check = checkAuthorization(encrypt, Deskey, Agent, false);
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
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = _tpCustInfoBLL.GetBalance((int)thisTp, transfer.player).Balance;
                    }

                    Response.StatusCode = (int)Error.OK;
                    var Param = new
                    {
                        channelId = Agent,
                        account = transfer.account,
                        balance = tpAffterAmount
                    };

                    sResult = DoEncrypt(Param, Deskey);
                    _logger.LogInformation($"Response Decrypt: {JsonConvert.SerializeObject(Param)}");
                    _log2File.Info($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Param)}", logName);
                    return sResult;
                }
                else if (tpAffterAmount < 0)
                {
                    Response.StatusCode = (int)Error.INVALID_DATA;
                    var Param = new
                    {
                        code = 138
                    };

                    sResult = DoEncrypt(Param, Deskey);
                    _logger.LogError($"Response Decrypt: {JsonConvert.SerializeObject(Param)}");
                    _log2File.Error($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Param)}", logName);
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

        [HttpPost]
        [Route("[controller]/cancel")]
        public async Task<dynamic> CancelAsync([FromQuery] Dictionary<string, string> getReq)
        {
            Response.StatusCode = (int)Error.ERROR;
            var Fatal = new
            {
                code = 500
            };

            string sResult;
            string logName = $"{thisTp}_cancel";
            var Content = JsonConvert.SerializeObject(getReq);
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var encrypt = await reader.ReadToEndAsync();

            var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            string Agent = thirdPartySetting.TpSetting["Agent"];
            string Deskey = thirdPartySetting.TpSetting["Deskey"];

            var check = checkAuthorization(encrypt, Deskey, Agent, false);
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
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = _tpCustInfoBLL.GetBalance((int)Tp.MP, transfer.account).Balance;
                    }

                    Response.StatusCode = (int)Error.OK;
                    var Param = new
                    {
                        isCanceled = true,
                        channelId = Agent,
                        account = transfer.account,
                        balance = tpAffterAmount
                    };

                    sResult = DoEncrypt(Param, Deskey);
                    _logger.LogInformation($"Response Decrypt: {JsonConvert.SerializeObject(Param)}");
                    _log2File.Info($"{Content}\r\n{encrypt}\r\n{JsonConvert.SerializeObject(check)}\r\n{sResult}\r\n{JsonConvert.SerializeObject(Param)}", logName);
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

        private CheckResp checkAuthorization(string param, string desKey, string agent, bool checkOnline)
        {
            CheckResp resp = new CheckResp();
            resp.StatusCode = (int)Error.OK;
            resp.Result = "";
            try
            {
                var decrypt = _tools.AesDecrypt(HttpUtility.UrlEncode(param), desKey);
                var jsonParsedArray = JObject.Parse(decrypt);
                _logger.LogInformation($"Request Decrypt: {JsonConvert.SerializeObject(jsonParsedArray)}");

                var channelId = jsonParsedArray["channelId"]?.ToString();
                var account = jsonParsedArray["account"]?.ToString();
                var requestAmount = jsonParsedArray["requestAmount"]?.ToString();
                var gameId = jsonParsedArray.ContainsKey("gameId") ? jsonParsedArray["gameId"].ToString() : "";//jsonParsedArray["gameId"]?.ToString();
                var orderId = jsonParsedArray["orderId"]?.ToString();

                if (string.IsNullOrEmpty(channelId) || channelId != agent)
                {
                    resp.StatusCode = (int)Error.UAUTHORIZED;
                    _logger.LogError($"Request Check: {JsonConvert.SerializeObject(resp)}");
                    return resp;
                }

                if (string.IsNullOrEmpty(account) || account.Split('_').Count() != 2)
                {
                    var Param = new
                    {
                        code = 11
                    };
                    resp.StatusCode = (int)Error.INVALID_DATA;
                    resp.Result = DoEncrypt(Param, desKey);
                    resp.Code = Param.code;
                    _logger.LogError($"Request Check: {JsonConvert.SerializeObject(resp)}");
                    return resp;
                }

                string[] acc = account.Split('_');
                string player = acc[1];

                if (checkOnline)
                {
                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, player);
                    if (tpCheck == null || tpCheck.Locked)
                    {
                        var Param = new
                        {
                            code = 11
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
                    decimal amount = Convert.ToDecimal(requestAmount);
                    if (amount <= 0)
                    {
                        var Param = new
                        {
                            code = 31
                        };
                        resp.StatusCode = (int)Error.INVALID_DATA;
                        resp.Result = DoEncrypt(Param, desKey);
                        resp.Code = Param.code;
                        _logger.LogError($"Request Check: {JsonConvert.SerializeObject(resp)}");
                        return resp;
                    }
                }

                resp.channelId = channelId;
                resp.account = account;
                resp.requestAmount = requestAmount;
                resp.gameId = gameId;
                resp.orderId = orderId;
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
