using Microsoft.AspNetCore.Mvc;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class AmigoController : ControllerBase
    {
        #region Fields

        private readonly AmigoBLL _amigoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Log2File _log2File;
        private readonly Tools _tools;
        private readonly Tp _thisTp;
        private readonly Dictionary<int, AmigoConfig> _siteConfigDic;

        #endregion

        #region Constructors

        public AmigoController(IServiceProvider provider, ILogger<AmigoController> logger)
        {
            _log2File = provider.GetRequiredService<Log2File>();
            _amigoBLL = provider.GetRequiredService<AmigoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _thisTp = Tp.Amigo;
            // 取共用，設定依機器分為(nova、aiking)
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<AmigoConfig>(Tp.Amigo.ToString());
        }

        #endregion

        #region Response

        public class CommonResp
        {
            public int error { get; set; }
            public string description { get; set; }
        }

        public class SessionExpiredResp : CommonResp
        {
        }

        #endregion

        [HttpGet]
        [Route("[controller]/ping")]
        public string PingTest() => "Ping Ok.";

        [HttpPost]
        [Route("[controller]/{sitename}/authenticate.html")]
        public dynamic Authenticate(string sitename, [FromForm] AuthReq getReq)
        {
            string logName = $"{_thisTp}_authenticate";
            CommonResp getErrorResponse = new()
            {
                error = (int)ErrorCodeOfAmigo.InternalError,
                description = ErrorCodeOfAmigo.InternalError.GetEnumDescription()
            };

            try
            {
                List<string> verifyParamKey = new()
                {
                    "token",
                    "providerId",
                    "gameId",
                    "hash"
                };

                foreach (var key in verifyParamKey)
                {
                    if (!Request.Form.ContainsKey(key) || string.IsNullOrEmpty(Request.Form[key]))
                    {
                        getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Empty mandatory field {key}";
                        return getErrorResponse;
                    }
                }

                var verifyHash = false;
                verifyHash = CheckHash(Request.Form, sitename, getReq.hash, logName);
                if (!verifyHash)
                {
                    getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Incorrect hash";
                    return getErrorResponse;
                }

                return _amigoBLL.Authenticate(getReq);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            return getErrorResponse;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/balance.html")]
        public dynamic Balance(string sitename, [FromForm] BalanceReq getReq)
        {
            string logName = $"{_thisTp}_balance";
            CommonResp getErrorResponse = new()
            {
                error = (int)ErrorCodeOfAmigo.InternalError,
                description = "Internal server error."
            };

            try
            {
                List<string> verifyParamKey = new()
                {
                    "hash",
                    "providerId",
                    "userId",
                    "token",
                    "gameId"
                };

                foreach (var key in verifyParamKey)
                {
                    if (!Request.Form.ContainsKey(key) || string.IsNullOrEmpty(Request.Form[key]))
                    {
                        getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Empty mandatory field {key}";
                        return getErrorResponse;
                    }
                }

                var verifyHash = false;
                verifyHash = CheckHash(Request.Form, sitename, getReq.hash, logName);
                if (!verifyHash)
                {
                    getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Incorrect hash";
                    return getErrorResponse;
                }

                return _amigoBLL.GetBalance(getReq);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            return getErrorResponse;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/bet.html")]
        public dynamic Bet(string sitename, [FromForm] BettingData_Amigo getReq)
        {
            string logName = $"{_thisTp}_bet";
            CommonResp getErrorResponse = new()
            {
                error = (int)ErrorCodeOfAmigo.InternalError,
                description = "Internal server error."
            };

            try
            {
                List<string> verifyParamKey = new()
                {
                    "hash",
                    "userId",
                    "gameId",
                    "roundId",
                    "amount",
                    "reference",
                    "providerId",
                    "timestamp",
                    "roundDetails",
                    "roundDetails",
                    "token",
                };

                foreach (var key in verifyParamKey)
                {
                    if (!Request.Form.ContainsKey(key) || string.IsNullOrEmpty(Request.Form[key]))
                    {
                        getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Empty mandatory field {key}";
                        return getErrorResponse;
                    }
                }
                var verifyHash = false;
                verifyHash = CheckHash(Request.Form, sitename, getReq.hash, logName);
                if (!verifyHash)
                {
                    getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Incorrect hash";
                    return getErrorResponse;
                }
                return _amigoBLL.PlaceBet(getReq);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            return getErrorResponse;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/result.html")]
        public dynamic Result(string sitename, [FromForm] PayoutData_Amigo getReq)
        {
            string logName = $"{_thisTp}_result";
            CommonResp getErrorResponse = new()
            {
                error = (int)ErrorCodeOfAmigo.InternalError,
                description = "Internal server error."
            };
            try
            {
                List<string> verifyParamKey = new()
                {
                    "hash",
                    "userId",
                    "gameId",
                    "roundId",
                    "amount",
                    "reference",
                    "providerId",
                    "timestamp",
                    "roundDetails",
                    "token",
                };

                foreach (var key in verifyParamKey)
                {
                    if (!Request.Form.ContainsKey(key) || string.IsNullOrEmpty(Request.Form[key]))
                    {
                        getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Empty mandatory field {key}";
                        return getErrorResponse;
                    }
                }

                var verifyHash = false;
                verifyHash = CheckHash(Request.Form, sitename, getReq.hash, logName);
                if (!verifyHash)
                {
                    getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Incorrect hash";
                    return getErrorResponse;
                }

                return _amigoBLL.Payout(getReq);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            return getErrorResponse;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/bonusWin.html")]
        public dynamic BonusWin(string sitename, [FromForm] PayoutData_Amigo getReq)
        {
            string logName = $"{_thisTp}_bonusWin";
            CommonResp getErrorResponse = new()
            {
                error = (int)ErrorCodeOfAmigo.InternalError,
                description = "Internal server error."
            };

            try
            {
                List<string> verifyParamKey = new()
                {
                    "hash",
                    "userId",
                    "amount",
                    "reference",
                    "providerId",
                    "timestamp",
                    "bonusCode",
                    "gameId",
                    "roundId",
                    "token",
                };

                foreach (var key in verifyParamKey)
                {
                    if (!Request.Form.ContainsKey(key) || string.IsNullOrEmpty(Request.Form[key]))
                    {
                        getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Empty mandatory field {key}";
                        return getErrorResponse;
                    }
                }

                var verifyHash = false;
                verifyHash = CheckHash(Request.Form, sitename, getReq.hash, logName);
                if (!verifyHash)
                {
                    getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Incorrect hash";
                    return getErrorResponse;
                }

                return _amigoBLL.BonusWin(getReq);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            return getErrorResponse;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/jackpotWin.html")]
        public dynamic JackpotWin(string sitename, [FromForm] PayoutData_Amigo getReq)
        {
            string logName = $"{_thisTp}_jackpotWin";
            CommonResp getErrorResponse = new()
            {
                error = (int)ErrorCodeOfAmigo.InternalError,
                description = "Internal server error."
            };

            try
            {
                List<string> verifyParamKey = new()
                {
                    "hash",
                    "userId",
                    "gameId",
                    "roundId",
                    "jackpotId",
                    "amount",
                    "reference",
                    "providerId",
                    "timestamp",
                    "token",
                };

                foreach (var key in verifyParamKey)
                {
                    if (!Request.Form.ContainsKey(key) || string.IsNullOrEmpty(Request.Form[key]))
                    {
                        getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Empty mandatory field {key}";
                        return getErrorResponse;
                    }
                }

                var verifyHash = false;
                verifyHash = CheckHash(Request.Form, sitename, getReq.hash, logName);
                if (!verifyHash)
                {
                    getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Incorrect hash";
                    return getErrorResponse;
                }

                return _amigoBLL.JackpotWin(getReq);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            return getErrorResponse;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/refund.html")]
        public dynamic Refund(string sitename, [FromForm] BettingData_Amigo getReq)
        {
            string logName = $"{_thisTp}_refund";
            CommonResp getErrorResponse = new()
            {
                error = (int)ErrorCodeOfAmigo.InternalError,
                description = "Internal server error."
            };

            try
            {
                List<string> verifyParamKey = new()
                {
                    "hash",
                    "userId",
                    "reference",
                    "providerId",
                    "gameId",
                    "roundId",
                    "amount",
                    "timestamp",
                    "roundDetails",
                    "token",
                };

                foreach (var key in verifyParamKey)
                {
                    if (!Request.Form.ContainsKey(key) || string.IsNullOrEmpty(Request.Form[key]))
                    {
                        getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Empty mandatory field {key}";
                        return getErrorResponse;
                    }
                }

                var verifyHash = false;
                verifyHash = CheckHash(Request.Form, sitename, getReq.hash, logName);
                if (!verifyHash)
                {
                    getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Incorrect hash";
                    return getErrorResponse;
                }

                return _amigoBLL.CancelBet(getReq);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            return getErrorResponse;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/session/expired")]
        public dynamic SessionExpired(string sitename, [FromForm] SessionExpiredReq getReq)
        {
            string logName = $"{_thisTp}_refund";
            CommonResp getErrorResponse = new()
            {
                error = (int)ErrorCodeOfAmigo.InternalError,
                description = "Internal server error."
            };

            try
            {
                List<string> verifyParamKey = new()
                {
                    "hash",
                    "providerId",
                    "sessionId",
                    "playerId",
                    "token",
                    "gameId",
                };

                foreach (var key in verifyParamKey)
                {
                    if (!Request.Form.ContainsKey(key) || string.IsNullOrEmpty(Request.Form[key]))
                    {
                        getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Empty mandatory field {key}";
                        return getErrorResponse;
                    }
                }

                var verifyHash = false;
                verifyHash = CheckHash(Request.Form, sitename, getReq.hash, logName);
                if (!verifyHash)
                {
                    getErrorResponse.description = $"{ErrorCodeOfAmigo.InternalError.GetEnumDescription()} Incorrect hash";
                    return getErrorResponse;
                }

                SessionExpiredResp response = new()
                {
                    error = (int)ErrorCodeOfAmigo.Success,
                    description = ErrorCodeOfAmigo.Success.GetEnumDescription(),
                };
                return response;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            return getErrorResponse;
        }

        private bool CheckHash(IFormCollection request, string siteName, string hash, string logName)
        {
            var serverInfo = _thirdPartyBLL.GetServerInfoBySiteName(siteName);
            if (serverInfo is not { SiteId: > 0 })
            {
                _log2File.Error($"SiteName Error\r\nsiteName={siteName}", logName);
                return false;
            }

            var siteId = serverInfo.SiteId;
            var secretKey = _siteConfigDic[siteId].SecretKey;

            var sortParam = SortParam(request, secretKey);
            var encryptHash = _tools.ToMD5(sortParam).ToLower();
            if (hash.ToLower() == encryptHash)
            {
                return true;
            }

            _log2File.Error($"CheckHash Error\r\nhash={hash}\r\nencryptHash={encryptHash}", logName);
            return false;
        }

        private string SortParam(IFormCollection request, string secretKey)
        {
            Dictionary<string, dynamic> dict = new();
            foreach (string key in request.Keys)
            {
                dict.Add(key, request[key]);
            }
            Dictionary<string, dynamic> dicP = dict.Where(x => x.Key != "hash").OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            return $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}{secretKey}";
        }
    }
}
