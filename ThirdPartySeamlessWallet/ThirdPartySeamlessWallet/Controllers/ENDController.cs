using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class ENDController : ControllerBase
    {
        private readonly AuthBLL _authBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Tools _tools;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Log2File _log2File;
        private readonly IHostEnvironment _environment;
        private readonly int _thisTp;
        private readonly string _tpName = Tp.Endorphina.ToString();
        private readonly int _gameType = (int)GameType.Endorphina_RNG;

        public ENDController(IServiceProvider provider)
        {
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _log2File = provider.GetRequiredService<Log2File>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _thisTp = (int)Tp.Endorphina;
        }

        #region ErrorCode
        Dictionary<string, string> ErrorMassage = new Dictionary<string, string>()
        {   
            // 401
            {"ACCESS_DENIED","Request signature is incorrect"},
            // 402
            {"INSUFFICIENT_FUNDS","Player has insufficient funds"},
            // 403
            {"TOKEN_EXPIRED","The session token expired"},
            {"LIMIT_REACHED","System is busy"},
            // 404
            {"TOKEN_NOT_FOUND","The session token is invalid"},
            // 500
            {"INTERNAL_ERROR", "Internal server error"},
        };
        #endregion

        #region Request

        public class CheckReq
        {
            public string param { get; set; }
            public string sign { get; set; }
        }

        public class SessionReq
        {
            public string token { get; set; }
            public string sign { get; set; }
        }

        public class BalanceReq : SessionReq
        {
            public string currency { get; set; }
            public string game { get; set; }
            public string player { get; set; }
        }

        #endregion

        #region Response

        public class ErrorResp
        {
            public string code { get; set; }
            public string message { get; set; }
        }

        public class CheckResp
        {
            public int nodeId { get; set; }
            public string param { get; set; }
            public string sign { get; set; }
        }

        public class SessionResp
        {
            public string player { get; set; }
            public string currency { get; set; }
            public string game { get; set; }
        }

        public class BalanceResp
        {
            public long balance { get; set; }
        }

        public class BetResp : BalanceResp
        {
            public string transactionId { get; set; }
        }

        public class RefundResp : BetResp
        {
        }

        public class WinResp : BetResp
        {
        }

        #endregion

        [HttpGet]
        [Route("[controller]/check")]
        public dynamic Check([FromQuery] CheckReq getReq)
        {
            string logName = $"{_tpName}_check";

            ErrorResp errorResponse = new()
            {
                code = "INTERNAL_ERROR",
                message = ErrorMassage["INTERNAL_ERROR"]
            };

            try
            {
                var verifySign = false;
                if (!string.IsNullOrEmpty(getReq.sign))
                {
                    verifySign = CheckSign(getReq.sign, JsonConvert.SerializeObject(getReq), logName);
                }

                if (string.IsNullOrEmpty(getReq.param) || !verifySign)
                {
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.code = "ACCESS_DENIED";
                    errorResponse.message = ErrorMassage["ACCESS_DENIED"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting(_thisTp);
                var secretKey = thirdPartySetting.TpSetting["SecretKey"];
                var nodeId = Convert.ToInt32(thirdPartySetting.TpSetting["NodeId"]);

                CheckResp response = new()
                {
                    nodeId = nodeId,
                    param = getReq.param,
                };
                var sParam = SortParam(JsonConvert.SerializeObject(response), secretKey);
                response.sign = EncryptSha1(sParam).ToLower();
                _log2File.Info($"sParam=\r\n{sParam}", logName);

                Response.StatusCode = (int)HttpStatusCode.OK;
                return response;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return errorResponse;
        }

        [HttpGet]
        [Route("[controller]/session")]
        public dynamic Session([FromQuery] SessionReq getReq)
        {
            string logName = $"{_tpName}_session";

            ErrorResp errorResponse = new()
            {
                code = "INTERNAL_ERROR",
                message = ErrorMassage["INTERNAL_ERROR"]
            };

            try
            {
                var verifySign = false;
                if (!string.IsNullOrEmpty(getReq.sign))
                {
                    verifySign = CheckSign(getReq.sign, JsonConvert.SerializeObject(getReq), logName);
                }

                if (string.IsNullOrEmpty(getReq.token) || !verifySign)
                {
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.code = "ACCESS_DENIED";
                    errorResponse.message = ErrorMassage["ACCESS_DENIED"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                var verify = _authBLL.Authenticate(getReq.token, out TpCustInfoAuth tpCustInfoAuth);
                if (!verify)
                {
                    Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    errorResponse.code = "TOKEN_EXPIRED";
                    errorResponse.message = ErrorMassage["TOKEN_EXPIRED"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(_thisTp, tpCustInfoAuth.TpUserName);
                if (tpCheck == null)
                {
                    Response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.code = "TOKEN_NOT_FOUND";
                    errorResponse.message = ErrorMassage["TOKEN_NOT_FOUND"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }
                else if (tpCheck.Locked)
                {
                    Response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.code = "TOKEN_NOT_FOUND";
                    errorResponse.message = ErrorMassage["TOKEN_NOT_FOUND"];
                    _log2File.Error($"Locked {JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                SessionResp response = new()
                {
                    player = tpCustInfoAuth.TpUserName,
                    currency = tpCustInfoAuth.TpCurrencyId,
                    game = tpCustInfoAuth.GameId,
                };

                Response.StatusCode = (int)HttpStatusCode.OK;
                return response;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return errorResponse;
        }

        [HttpGet]
        [Route("[controller]/balance")]
        public dynamic Balance([FromQuery] BalanceReq getReq)
        {
            string logName = $"{_tpName}_balance";

            ErrorResp errorResponse = new()
            {
                code = "INTERNAL_ERROR",
                message = ErrorMassage["INTERNAL_ERROR"]
            };

            try
            {
                var verifySign = false;
                if (!string.IsNullOrEmpty(getReq.sign))
                {
                    verifySign = CheckSign(getReq.sign, JsonConvert.SerializeObject(getReq), logName);
                }

                if (string.IsNullOrEmpty(getReq.token) || !verifySign)
                {
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.code = "ACCESS_DENIED";
                    errorResponse.message = ErrorMassage["ACCESS_DENIED"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                // check player account exist
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(_thisTp, getReq.player);
                if (tpCustInfo == null)
                {
                    Response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.code = "TOKEN_NOT_FOUND";
                    errorResponse.message = ErrorMassage["TOKEN_NOT_FOUND"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                var tpCustBalance = _environment.IsDevelopment() ? _tpCustInfoBLL.GetBalance(_thisTp, getReq.player, _gameType) : _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.player, _gameType);
                BalanceResp response = new()
                {
                    balance = ProcessBalance(tpCustBalance.Balance)
                };

                Response.StatusCode = (int)HttpStatusCode.OK;
                return response;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return errorResponse;
        }

        [HttpPost]
        [Route("[controller]/bet")]
        public dynamic Bet([FromForm] BettingData_Endorphina getReq)
        {
            string logName = $"{_tpName}_bet";

            ErrorResp errorResponse = new()
            {
                code = "INTERNAL_ERROR",
                message = ErrorMassage["INTERNAL_ERROR"]
            };

            try
            {
                var verifySign = false;
                if (!string.IsNullOrEmpty(getReq.sign))
                {
                    verifySign = CheckSign(getReq.sign, JsonConvert.SerializeObject(getReq, Formatting.Indented), logName);
                }

                if (!verifySign)
                {
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.code = "ACCESS_DENIED";
                    errorResponse.message = ErrorMassage["ACCESS_DENIED"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                if (string.IsNullOrEmpty(getReq.player))
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.code = "INTERNAL_ERROR";
                    errorResponse.message = $"{ErrorMassage["INTERNAL_ERROR"]} (player empty)";
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(_thisTp, getReq.player);
                if (tpCheck == null)
                {
                    Response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.code = "TOKEN_NOT_FOUND";
                    errorResponse.message = ErrorMassage["TOKEN_NOT_FOUND"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }
                else if (tpCheck.Locked)
                {
                    Response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.code = "TOKEN_NOT_FOUND";
                    errorResponse.message = ErrorMassage["TOKEN_NOT_FOUND"];
                    _log2File.Error($"Locked {JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                var result = _bettingBLL.PlaceBetRNG_Endorphina(getReq, _gameType, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0 || result == -1)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, getReq.player, _gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    BetResp response = new()
                    {
                        balance = ProcessBalance(tpAffterAmount),
                        transactionId = getReq.id.ToString()
                    };

                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return response;
                }
                else if (tpAffterAmount < 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.PaymentRequired;
                    errorResponse.code = "INSUFFICIENT_FUNDS";
                    errorResponse.message = ErrorMassage["INSUFFICIENT_FUNDS"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }
                else
                {
                    _log2File.Error($"result={result}", logName);
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return errorResponse;
        }

        [HttpPost]
        [Route("[controller]/refund")]
        public dynamic Refund([FromForm] CancelBetData_Endorphina getReq)
        {
            string logName = $"{_tpName}_refund";

            ErrorResp errorResponse = new()
            {
                code = "INTERNAL_ERROR",
                message = ErrorMassage["INTERNAL_ERROR"]
            };

            try
            {
                var verifySign = false;
                if (!string.IsNullOrEmpty(getReq.sign))
                {
                    verifySign = CheckSign(getReq.sign, JsonConvert.SerializeObject(getReq, Formatting.Indented), logName);
                }

                if (!verifySign)
                {
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.code = "ACCESS_DENIED";
                    errorResponse.message = ErrorMassage["ACCESS_DENIED"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                if (string.IsNullOrEmpty(getReq.player))
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.code = "INTERNAL_ERROR";
                    errorResponse.message = $"{ErrorMassage["INTERNAL_ERROR"]} (player empty)";
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                var result = _settledBLL.CancelBetRNG_Endorphina(getReq, _gameType, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0 || result == -1)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, getReq.player, _gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    RefundResp response = new()
                    {
                        balance = ProcessBalance(tpAffterAmount),
                        transactionId = getReq.id.ToString()
                    };

                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return response;
                }
                else
                {
                    _log2File.Error($"result={result}", logName);
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return errorResponse;
        }

        [HttpPost]
        [Route("[controller]/win")]
        public dynamic Win([FromForm] PayoutData_Endorphina getReq)
        {
            string logName = $"{_tpName}_win";

            ErrorResp errorResponse = new()
            {
                code = "INTERNAL_ERROR",
                message = ErrorMassage["INTERNAL_ERROR"]
            };

            try
            {
                var verifySign = false;
                if (!string.IsNullOrEmpty(getReq.sign))
                {
                    verifySign = CheckSign(getReq.sign, JsonConvert.SerializeObject(getReq, Formatting.Indented), logName);
                }

                if (!verifySign)
                {
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.code = "ACCESS_DENIED";
                    errorResponse.message = ErrorMassage["ACCESS_DENIED"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                if (string.IsNullOrEmpty(getReq.player))
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.code = "INTERNAL_ERROR";
                    errorResponse.message = $"{ErrorMassage["INTERNAL_ERROR"]} (player empty)";
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                var result = _settledBLL.PayoutRNG_Endorphina(getReq, _gameType, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0 || result == -1)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, getReq.player, _gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }

                    WinResp response = new()
                    {
                        balance = ProcessBalance(tpAffterAmount),
                        transactionId = getReq.id.ToString()
                    };

                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return response;
                }
                else
                {
                    _log2File.Error($"result={result}", logName);
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return errorResponse;
        }

        [HttpPost]
        [Route("[controller]/endSession")]
        public dynamic EndSession([FromForm] EndSessionData_Endorphina getReq)
        {
            string logName = $"{_tpName}_endSession";

            ErrorResp errorResponse = new()
            {
                code = "INTERNAL_ERROR",
                message = ErrorMassage["INTERNAL_ERROR"]
            };

            try
            {
                var verifySign = false;
                if (!string.IsNullOrEmpty(getReq.sign))
                {
                    verifySign = CheckSign(getReq.sign, JsonConvert.SerializeObject(getReq, Formatting.Indented), logName);
                }

                if (!verifySign)
                {
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.code = "ACCESS_DENIED";
                    errorResponse.message = ErrorMassage["ACCESS_DENIED"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                if (string.IsNullOrEmpty(getReq.player))
                {
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.code = "INTERNAL_ERROR";
                    errorResponse.message = $"{ErrorMassage["INTERNAL_ERROR"]} (player empty)";
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                var result = _settledBLL.EndSessionRNG_Endorphina(getReq, _gameType, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    return true;
                }
                else
                {
                    _log2File.Error($"result={result}", logName);
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return errorResponse;
        }

        private long ProcessBalance(decimal balance)
        {
            // All money values are transferred in thousandths of a money unit.
            return (long)_tools.Floor(balance, 0);
        }

        private bool CheckSign(string sign, string request, string logName)
        {
            var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting(_thisTp);
            var secretKey = thirdPartySetting.TpSetting["SecretKey"];

            var sortParam = SortParam(request, secretKey);
            var encryptSign = EncryptSha1(sortParam).ToLower();
            if (sign.ToLower() == encryptSign)
            {
                return true;
            }

            _log2File.Error($"CheckHash Error\r\nsign={sign}\r\nencryptSign={encryptSign}", logName);
            return false;
        }

        private string SortParam(string request, string secretKey)
        {
            var dict = _tools.FromJson<Dictionary<string, dynamic>>(request);
            Dictionary<string, dynamic> dicP = dict.Where(x => x.Key != "sign").OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            if (dicP.ContainsKey("progressive") && dicP["progressive"] != null)
            {
                dicP["progressive"] = dicP["progressive"].ToString().ToLower();
            }
            if (dicP.ContainsKey("id") && dicP["id"] == 0)
            {
                dicP.Remove("id");
            }
            if (dicP.ContainsKey("game") && dicP["game"] != null)
            {
                dicP["game"] = HttpUtility.UrlDecode(dicP["game"]);
            }
            return $"{string.Join("", dicP.Select(kp => $"{kp.Value}").ToArray())}{secretKey}";
        }

        private string EncryptSha1(string str)
        {
            using SHA1 sha1Hash = SHA1.Create();
            byte[] sourceBytes = Encoding.UTF8.GetBytes(str);
            byte[] hashBytes = sha1Hash.ComputeHash(sourceBytes);
            string hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            return hash;
        }
    }
}
