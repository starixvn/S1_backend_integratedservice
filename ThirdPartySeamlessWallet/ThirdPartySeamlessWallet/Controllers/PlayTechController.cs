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
    public class PlayTechController : ControllerBase
    {
        private readonly AuthBLL _authBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Tools _tools;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Log2File _log2File;
        private readonly int _thisTp;
        private readonly string _tpName = Tp.PlayTech.ToString();
        // 通常是一直重试直到成功, 但是由于我们每月会对账,所以到第二个月初如果还有没有完结的话会转入手工处置
        private readonly int _partitionCheckDay = 15;

        Dictionary<string, string> _specialCurrency = new Dictionary<string, string>() {
            {"BRL","Playtech_N" },
        };

        public PlayTechController(IServiceProvider provider)
        {
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _log2File = provider.GetRequiredService<Log2File>();
            _thisTp = (int)Tp.PlayTech;
        }

        #region ErrorCode
        Dictionary<string, string> ErrorMassage = new Dictionary<string, string>()
        {   {"0","Success"},
            {"P_00","Server Error, internal server error"},
            {"P_01","Invalid request. This error can be returned if required parameters are missing or they have incorrect format"},
            {"P_02","Invalid hash"},
            {"P_03","Invalid brandId"},
            {"P_04","Player not found"},
            {"P_05","Invalid currency"},
            {"P_06","Invalid token or token expired"},
            {"P_07","Player is inactive and they cannot launch game"},
            {"P_08","Invalid session, the session does not exist"},
            {"P_09","Invalid language"},
            {"P_10","Invalid country"},
            {"P_11","IP deny, need to white list"},
            {"T_01","Player Insufficient Funds"},
            {"T_03","Transaction does not exist when cancel"},
            {"T_04","Bet limit was exceeded"},
            {"T_07","Transaction declined"},
            {"R_01","Round is closed when bet"},
            {"R_02","Invalid round when win"},
            {"G_01","Game not found"},
            {"G_02","Game disabled"},
            {"G_03","Game does not supported currency"},
            {"G_04","Game does not supported language"},
        };
        #endregion

        #region Request

        public class TokenInfo
        {
            public string shortTimeToken { get; set; }
            public string longTimeToken { get; set; }
            public string tpUserName { get; set; }
            public int gameType { get; set; }
        }

        public class CommonReq
        {
            public string requestId { get; set; }
            public int brandId { get; set; }
        }

        public class AuthReq : CommonReq
        {
            public string token { get; set; }
            public string ip { get; set; }
        }

        public class BalanceReq : CommonReq
        {
            public string playerId { get; set; }
            public string playerSessionId { get; set; }
            public string gameCode { get; set; }
        }


        #endregion

        #region Response

        public class CommonResp
        {
            public string requestId { get; set; }
            public string error { get; set; }
            public string message { get; set; }
        }

        public class AuthResp : CommonResp
        {
            public string playerSessionId { get; set; }
            public string playerId { get; set; }
            public string playerName { get; set; }
            public string currency { get; set; }
            public decimal balance { get; set; }
            public string country { get; set; }
        }

        public class BalanceResp : CommonResp
        {
            public string currency { get; set; }
            public decimal balance { get; set; }
            public decimal bonusBalance { get; set; }
        }

        #endregion

        [HttpPost]
        [Route("[controller]/{sitename}/auth")]
        public async Task<dynamic> AuthAsync(string sitename, [FromQuery] Dictionary<string, string> getQuery)
        {
            string logName = $"{_tpName}_auth";

            CommonResp errorResponse = new()
            {
                error = "P_00",
                message = ErrorMassage["P_00"]
            };

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var Content = await reader.ReadToEndAsync();

                var verifyHash = false;
                if (getQuery.TryGetValue("hash", out var hash))
                {
                    verifyHash = CheckHash(sitename, hash, Content, logName);
                }
                if (!verifyHash)
                {
                    errorResponse.error = "P_02";
                    errorResponse.message = ErrorMassage["P_02"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                AuthReq getReq = JsonConvert.DeserializeObject<AuthReq>(Content);
                errorResponse.requestId = getReq.requestId;

                var tokenInfo = GetTokenInfo(getReq.token, logName, out bool verifyToken);
                if (!verifyToken)
                {
                    errorResponse.error = "P_06";
                    errorResponse.message = ErrorMassage["P_06"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                int gameType = tokenInfo.gameType;

                var verify = _authBLL.Authenticate(tokenInfo.shortTimeToken, out TpCustInfoAuth tpCustInfoAuth);
                if (!verify)
                {
                    errorResponse.error = "P_06";
                    errorResponse.message = ErrorMassage["P_06"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(_thisTp, tpCustInfoAuth.TpUserName);
                if (tpCheck == null)
                {
                    errorResponse.error = "P_04";
                    errorResponse.message = ErrorMassage["P_04"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }
                else if (tpCheck.Locked)
                {
                    errorResponse.error = "P_07";
                    errorResponse.message = ErrorMassage["P_07"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                _tpCustInfoBLL.SetCustGotoGame(tpCustInfoAuth.CustId, _thisTp, gameType);
                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, tpCustInfoAuth.TpUserName, (int)gameType);
                AuthResp response = new()
                {
                    requestId = getReq.requestId,
                    playerSessionId = HttpUtility.UrlEncode(tokenInfo.shortTimeToken),
                    playerId = tpCustInfoAuth.TpUserName,
                    playerName = tpCustInfoAuth.TpUserName,
                    currency = ProcessCurrency(tpCustInfoAuth.TpCurrencyId),
                    balance = tpCustBalance.Balance,
                    country = GetTpCountryCode(tpCustInfoAuth.TpCurrencyId, logName),
                    error = "0",
                    message = ErrorMassage["0"],
                };
                return response;

            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
            return errorResponse;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/balance")]
        public async Task<dynamic> BalanceAsync(string sitename, [FromQuery] Dictionary<string, string> getQuery)
        {
            string logName = $"{_thisTp}_balance";

            CommonResp errorResponse = new()
            {
                error = "P_00",
                message = ErrorMassage["P_00"]
            };

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            var verifyHash = false;
            if (getQuery.TryGetValue("hash", out var hash))
            {
                verifyHash = CheckHash(sitename, hash, Content, logName);
            }
            if (!verifyHash)
            {
                errorResponse.error = "P_02";
                errorResponse.message = ErrorMassage["P_02"];
                _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                return errorResponse;
            }

            BalanceReq getReq = JsonConvert.DeserializeObject<BalanceReq>(Content);
            errorResponse.requestId = getReq.requestId;

            // check player account exist
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(_thisTp, getReq.playerId);
            if (tpCustInfo == null)
            {
                errorResponse.error = "P_04";
                errorResponse.message = ErrorMassage["P_04"];
                _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                return errorResponse;
            }

            // 對方的gamecode會有不正確的情況，改透過進入遊戲時auth取額度減少遊戲bonus額度顯示問題，但會有不準確的情況
            var gameType = _tpCustInfoBLL.GetCustGotoGameType(tpCustInfo.CustId, _thisTp);
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.playerId, (int)gameType);
            BalanceResp response = new()
            {
                requestId = getReq.requestId,
                currency = ProcessCurrency(tpCustBalance.TpCurrencyId),
                balance = tpCustBalance.Balance,
                bonusBalance = tpCustBalance.Balance,
                error = "0",
                message = ErrorMassage["0"],
            };
            return response;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/transaction")]
        public async Task<dynamic> TransactionAsync(string sitename, [FromQuery] Dictionary<string, string> getQuery)
        {
            string logName = $"{_thisTp}_transaction";

            CommonResp errorResponse = new()
            {
                error = "P_00",
                message = ErrorMassage["P_00"]
            };
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var Content = await reader.ReadToEndAsync();

                var verifyHash = false;
                if (getQuery.TryGetValue("hash", out var hash))
                {
                    verifyHash = CheckHash(sitename, hash, Content, logName);
                }
                if (!verifyHash)
                {
                    errorResponse.error = "P_02";
                    errorResponse.message = ErrorMassage["P_02"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                BettingData_PlayTech getReq = JsonConvert.DeserializeObject<BettingData_PlayTech>(Content);
                errorResponse.requestId = getReq.requestId;

                // 檢查帳號是否存在
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(_thisTp, getReq.playerId);
                if (tpCustInfo == null)
                {
                    errorResponse.error = "P_04";
                    errorResponse.message = ErrorMassage["P_04"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                // 暫不支援transType
                bool verifyTransType = CheckTransType(getReq);
                if (!verifyTransType)
                {
                    errorResponse.error = "P_01";
                    errorResponse.message = ErrorMassage["P_01"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                var gameType = _thirdPartyBLL.GetGameType(_thisTp, getReq.gameCode);
                var transCnt = getReq.trans.Count();

                /*
                 * 重试的扣款、存款、取消请求，有可能在首次请求的几个小时甚至几天后再次发送给代理方。
                 * 即便游戏会话（Session）已经过期，或游戏已经结束，代理的钱包系统也不能拒绝GM-Ag再次发送相同的请求。
                 * 对于已经成功的操作，也要允许GM-Ag再次发送相同参数值的请求。
                 */
                var betCnt = 0;
                var winCnt = 0;
                var cancelCnt = 0;
                foreach (var item in getReq.trans)
                {
                    switch (item.transType.ToLower())
                    {
                        case "bet":
                            betCnt++;
                            break;
                        case "win":
                            winCnt++;
                            break;
                        case "cancel":
                            cancelCnt++;
                            break;
                        default:
                            break;
                    }
                }

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.playerId, (int)gameType);
                if (betCnt == transCnt)
                {
                    // 與對方確認同一批Request: roundId 是相同的
                    // 全下注request
                    var result = _bettingBLL.PlaceBet_PlayTech(getReq, gameType, _partitionCheckDay, out decimal tpAffterAmount);
                    if (result > 0 || result == -1)
                    {
                        if (tpAffterAmount == 0 || result == -1)
                        {
                            tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, getReq.playerId, (int)gameType);
                            tpAffterAmount = tpCustBalance.Balance;
                        }
                        BalanceResp response = new()
                        {
                            requestId = getReq.requestId,
                            currency = ProcessCurrency(tpCustBalance.TpCurrencyId),
                            balance = tpAffterAmount,
                            bonusBalance = 0,
                            error = "0",
                            message = ErrorMassage["0"],
                        };
                        return response;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        errorResponse.error = "T_01";
                        errorResponse.message = ErrorMassage["T_01"];
                        _log2File.Error($"tpAffterAmount={tpAffterAmount}\r\n{JsonConvert.SerializeObject(errorResponse)}", logName);
                        return errorResponse;
                    }
                    else if (result == -3)
                    {
                        // 檢查已有派彩資料
                        errorResponse.error = "R_01";
                        errorResponse.message = ErrorMassage["R_01"];
                        _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                        return errorResponse;
                    }
                    else
                    {
                        _log2File.Error($"result={result}", logName);
                    }
                }
                else if (winCnt == transCnt)
                {
                    // 與對方確認同一批Request: roundId 是相同的
                    // 全派彩request
                    var result = _settledBLL.Payout_PlayTech(getReq, gameType, _partitionCheckDay, out decimal tpAffterAmount);
                    if (result > 0 || result == -1)
                    {
                        if (tpAffterAmount == 0 || result == -1)
                        {
                            tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, getReq.playerId, (int)gameType);
                            tpAffterAmount = tpCustBalance.Balance;
                        }
                        BalanceResp response = new()
                        {
                            requestId = getReq.requestId,
                            currency = ProcessCurrency(tpCustBalance.TpCurrencyId),
                            balance = tpAffterAmount,
                            bonusBalance = 0,
                            error = "0",
                            message = ErrorMassage["0"],
                        };
                        return response;
                    }
                    else if (result == -4)
                    {
                        // 檢查無下注資料
                        errorResponse.error = "R_02";
                        errorResponse.message = ErrorMassage["R_02"];
                        _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                        return errorResponse;
                    }
                    else if (result == -5)
                    {
                        // 與對方需要處理，在此情況下中獎金額將為0: 阻擋amount!=0
                        errorResponse.error = "P_01";
                        errorResponse.message = ErrorMassage["P_01"];
                        _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                        return errorResponse;
                    }
                    else
                    {
                        _log2File.Error($"result={result}", logName);
                    }
                }
                else if (cancelCnt == transCnt)
                {
                    // 全註銷request
                    var result = _settledBLL.CancelBet_PlayTech(getReq, gameType, _partitionCheckDay, out decimal tpAffterAmount);
                    if (result > 0 || result == -1)
                    {
                        if (tpAffterAmount == 0 || result == -1)
                        {
                            tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, getReq.playerId, (int)gameType);
                            tpAffterAmount = tpCustBalance.Balance;
                        }
                        BalanceResp response = new()
                        {
                            requestId = getReq.requestId,
                            currency = ProcessCurrency(tpCustBalance.TpCurrencyId),
                            balance = tpAffterAmount,
                            bonusBalance = 0,
                            error = "0",
                            message = ErrorMassage["0"],
                        };
                        return response;
                    }
                    else if (result == -2)
                    {
                        errorResponse.error = "T_03";
                        errorResponse.message = ErrorMassage["T_03"];
                        _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                        return errorResponse;
                    }
                    else
                    {
                        _log2File.Error($"result={result}", logName);
                    }
                }
                else
                {
                    // 與對方確認: 有的游戏供应商过来的一个请求 就是bet+win. 我们不会拆分所以也会按照这样的情况发送. 同一个trans 中不会出现 bet+win+ cancle/ bet+cancel 情况.
                    // 混合request(bet、win)
                    var result = _settledBLL.BetAndPayout_PlayTech(getReq, gameType, _partitionCheckDay, out decimal tpAffterAmount);
                    if (result > 0 || result == -1)
                    {
                        if (tpAffterAmount == 0 || result == -1)
                        {
                            tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, getReq.playerId, (int)gameType);
                            tpAffterAmount = tpCustBalance.Balance;
                        }
                        BalanceResp response = new()
                        {
                            requestId = getReq.requestId,
                            currency = ProcessCurrency(tpCustBalance.TpCurrencyId),
                            balance = tpAffterAmount,
                            bonusBalance = 0,
                            error = "0",
                            message = ErrorMassage["0"],
                        };
                        return response;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        errorResponse.error = "T_01";
                        errorResponse.message = ErrorMassage["T_01"];
                        _log2File.Error($"tpAffterAmount={tpAffterAmount}\r\n{JsonConvert.SerializeObject(errorResponse)}", logName);
                        return errorResponse;
                    }
                }

                _log2File.Error($"UNKNOW_ERROR\r\n{JsonConvert.SerializeObject(errorResponse)}", logName);
                return errorResponse;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName, ex);
                return errorResponse;
            }
        }

        [HttpPost]
        [Route("[controller]/{sitename}/payUp")]
        public async Task<dynamic> PayUpAsync(string sitename, [FromQuery] Dictionary<string, string> getQuery)
        {
            //payUp 这个接口 是玩家获得的收益 但是没有参与任何游戏
            string logName = $"{_thisTp}_payUp";

            CommonResp errorResponse = new()
            {
                error = "P_00",
                message = ErrorMassage["P_00"]
            };
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var Content = await reader.ReadToEndAsync();

                var verifyHash = false;
                if (getQuery.TryGetValue("hash", out var hash))
                {
                    verifyHash = CheckHash(sitename, hash, Content, logName);
                }
                if (!verifyHash)
                {
                    errorResponse.error = "P_02";
                    errorResponse.message = ErrorMassage["P_02"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                RewardData_PlayTech getReq = JsonConvert.DeserializeObject<RewardData_PlayTech>(Content);
                errorResponse.requestId = getReq.requestId;

                // 檢查帳號是否存在
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(_thisTp, getReq.playerId);
                if (tpCustInfo == null)
                {
                    errorResponse.error = "P_04";
                    errorResponse.message = ErrorMassage["P_04"];
                    _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                    return errorResponse;
                }

                var result = _settledBLL.Reward_PlayTech(getReq, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.playerId);
                    if (tpAffterAmount == 0 || result == -1)
                    {
                        tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, getReq.playerId);
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    BalanceResp response = new()
                    {
                        requestId = getReq.requestId,
                        currency = ProcessCurrency(tpCustBalance.TpCurrencyId),
                        balance = tpAffterAmount,
                        bonusBalance = 0,
                        error = "0",
                        message = ErrorMassage["0"],
                    };
                    return response;
                }
                else
                {
                    _log2File.Error($"result={result}", logName);
                }

                _log2File.Error($"UNKNOW_ERROR\r\n{JsonConvert.SerializeObject(errorResponse)}", logName);
                return errorResponse;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName, ex);
                return errorResponse;
            }
        }

        [HttpPost]
        [HttpGet]
        [Route("[controller]/{path1?}/{path2?}/{path3?}/{path4?}")]
        public async Task<CommonResp> NotFoundAsync()
        {
            string logName = $"{_tpName}_NotFound";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            CommonResp errorResponse = new()
            {
                error = "P_00",
                message = ErrorMassage["P_00"]
            };
            _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
            return errorResponse;
        }

        private TokenInfo GetTokenInfo(string token, string logName, out bool verifyToken)
        {
            try
            {
                verifyToken = true;
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(token));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                return new TokenInfo
                {
                    shortTimeToken = sessionKeyArr[0], /* 1 min*/
                    longTimeToken = sessionKeyArr[1],  /* Long Time*/
                    tpUserName = sessionKeyArr[2],
                    gameType = Convert.ToInt32(sessionKeyArr[3]),
                };
            }
            catch (FormatException ex)
            {
                verifyToken = false;
                _log2File.Error($"getTokenInfo Error{token}", logName, ex);
                return new TokenInfo();
            }
        }

        private static bool CheckTransType(BettingData_PlayTech bettingData_PlayTech)
        {
            // 暫不支援transType
            bool hasTransIn = bettingData_PlayTech.trans.Any(t => string.Equals(t.transType, "transIn", StringComparison.OrdinalIgnoreCase));
            bool hasTransOut = bettingData_PlayTech.trans.Any(t => string.Equals(t.transType, "transOut", StringComparison.OrdinalIgnoreCase));
            bool hasAmend = bettingData_PlayTech.trans.Any(t => string.Equals(t.transType, "amend", StringComparison.OrdinalIgnoreCase));

            if (!hasTransIn && !hasTransOut && !hasAmend)
            {
                return true;
            }

            return false;
        }

        private bool CheckHash(string sitename, string hash, string request, string logName)
        {
            var serverInfo = _thirdPartyBLL.GetServerInfoBySiteName(sitename);
            if (serverInfo == null || serverInfo.SiteId <= 0)
            {
                return false;
            }
            var thirdPartySetting = _thirdPartyBLL.GetThirdPartyBySite(_thisTp, serverInfo.SiteId);
            var dict = _tools.FromJson<Dictionary<string, dynamic>>(request);
            var dicP = ExceptHashAndSort(dict);
            var encryptHash = EncryptString(dicP + thirdPartySetting.TpSetting["SecretKey"]);
            if (hash == encryptHash)
            {
                return true;
            }

            _log2File.Error($"CheckHash Error\r\nhash={hash}\r\nencryptHash={encryptHash}", logName);
            return false;
        }

        private static string ExceptHashAndSort(Dictionary<string, dynamic> requestDict)
        {
            requestDict.Remove("hash");
            Dictionary<string, dynamic> sortedDict = requestDict.OrderBy(o => o.Key).ToDictionary(o => o.Key, p => p.Value);
            string plain = "";
            foreach (KeyValuePair<string, dynamic> dict in sortedDict)
            {
                plain += dict.Key + "=";

                if (dict.Value.GetType() == typeof(Newtonsoft.Json.Linq.JArray))
                {
                    plain += JsonConvert.SerializeObject(dict.Value, Formatting.None);
                }
                else
                {
                    plain += dict.Value;
                }

                plain += "&";
            }
            return plain.Substring(0, plain.Length - 1);
        }

        private static string EncryptString(string str)
        {
            MD5 md5 = MD5.Create();
            byte[] byteOld = Encoding.UTF8.GetBytes(str);
            byte[] byteNew = md5.ComputeHash(byteOld);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in byteNew)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        private string GetTpCountryCode(string tpCurrencyId, string logName)
        {
            TpCountryCode tpCountryCode = _tpCustInfoBLL.GetTpCurrencyCodeByCurrencyId(_thisTp, tpCurrencyId);
            if (tpCountryCode != null && !string.IsNullOrEmpty(tpCountryCode.CountryCode))
            {
                return tpCountryCode.CountryCode;
            }

            _log2File.Error($"GetTpCountryCode Error\r\n{tpCurrencyId}", logName);
            return "";
        }

        private string ProcessCurrency(string TpCurrencyId)
        {
            if (_specialCurrency.ContainsKey(TpCurrencyId))
            {
                return _specialCurrency[TpCurrencyId];
            }
            return TpCurrencyId;
        }
    }
}
