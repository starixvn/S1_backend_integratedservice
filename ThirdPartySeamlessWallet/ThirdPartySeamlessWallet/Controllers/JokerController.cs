using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class JokerController : ControllerBase
    {
        #region Fields

        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly IJokerBLL _jokerBll;
        private readonly Tools _tools;

        private readonly ILogger<JokerController> _logger;
        private readonly Log2File _log2File;
        private readonly int _thisTp;
        private readonly string _tpName = Tp.Joker.ToString();
        private readonly JokerConfig _defaultConfig;
        private readonly Dictionary<string, JokerConfig> _siteNameConfigs;

        #endregion

        #region Constructor

        public JokerController(IServiceProvider provider)
        {
            _authBLL = provider.GetService<AuthBLL>();
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _bettingBLL = provider.GetService<BettingBLL>();
            _settledBLL = provider.GetService<SettledBLL>();
            _tools = provider.GetService<Tools>();

            _logger = provider.GetService<ILogger<JokerController>>();
            _jokerBll = provider.GetService<IJokerBLL>();
            _log2File = provider.GetService<Log2File>();
            _thisTp = (int)Tp.Joker;
            _defaultConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<JokerConfig>(_tpName);
            _siteNameConfigs = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteName<JokerConfig>(_tpName);
        }

        #endregion

        #region Request Model

        public class AuthenticateTokenReq
        {
            public string appid { get; set; }
            public string hash { get; set; }
            public string ip { get; set; }
            public long timestamp { get; set; }
            public string token { get; set; }
        }

        public class AuthenticateTokenRep
        {
            public string Username { get; set; }
            public decimal Balance { get; set; }
            public string Message { get; set; }
            public int Status { get; set; }
        }

        public class BalanceReq
        {
            public string appid { get; set; }
            [JsonIgnore]
            public string hash { get; set; }
            public long timestamp { get; set; }
            public string username { get; set; }
        }
        public class Rep
        {
            public decimal Balance { get; set; }
            public string Message { get; set; }
            public int Status { get; set; }
        }

        public class BetReq : BettingDataRNG_Joker
        {
            public string hash { get; set; }
        }

        public class SettleBetReq : BettingDataRNG_Joker
        {
            public string hash { get; set; }
        }

        public class CancelBetReq : CancelBetDataRNG_Joker
        {
            public string hash { get; set; }
        }

        #endregion

        [Route("[controller]/authenticate-token")]
        [Route("[controller]/{sitename}/authenticate-token")]
        public AuthenticateTokenRep AuthenticateToken([FromForm] AuthenticateTokenReq authenticateTokenReq, string sitename = "")
        {
            var logName = $"{_tpName}_AuthenticateToken";
            _log2File.Info(JsonConvert.SerializeObject(authenticateTokenReq), logName);

            AuthenticateTokenRep authenticateTokenRep = new AuthenticateTokenRep
            {
                Status = 1000,
                Message = "Other"
            };

            var dicP = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(authenticateTokenReq)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            if (!CheckSign(authenticateTokenReq.appid, authenticateTokenReq.hash, dicP, sitename))
            {
                authenticateTokenRep.Status = 5;
                authenticateTokenRep.Message = "Invalid signature";
                return authenticateTokenRep;
            }

            var (token, gameCode) = GetToken(authenticateTokenReq.token);
            if (_authBLL.Authenticate(token, out TpCustInfoAuth tpCustInfoAuth))
            {
                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(_thisTp, tpCustInfoAuth.TpUserName);
                if (tpCheck == null || tpCheck.Locked)
                {
                    authenticateTokenRep.Status = 3;
                    authenticateTokenRep.Message = "Invalid token";
                }
                else
                {
                    var gameType = (int)_thirdPartyBLL.GetGameType(_thisTp, gameCode);
                    _tpCustInfoBLL.SetCustGotoGame(tpCustInfoAuth.CustId, _thisTp, gameType);
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, tpCustInfoAuth.TpUserName, gameType);
                    authenticateTokenRep.Status = 0;
                    authenticateTokenRep.Message = "Success";
                    authenticateTokenRep.Balance = tpCustBalance.Balance;
                    authenticateTokenRep.Username = tpCustInfoAuth.TpUserName;
                }
            }
            else
            {
                authenticateTokenRep.Status = 3;
                authenticateTokenRep.Message = "Invalid token";
            }

            _log2File.Info($"{JsonConvert.SerializeObject(authenticateTokenReq)}\r\n{JsonConvert.SerializeObject(authenticateTokenRep)}", logName);
            return authenticateTokenRep;
        }

        [Route("[controller]/balance")]
        [Route("[controller]/{sitename}/balance")]
        public Rep Balance([FromForm] BalanceReq balanceReq, string sitename = "")
        {
            var logName = $"{_tpName}_Balance";
            Rep rep = new Rep
            {
                Status = 1000,
                Message = "Other"
            };

            var dicP = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(balanceReq)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            if (!CheckSign(balanceReq.appid, balanceReq.hash, dicP, sitename))
            {
                rep.Status = 5;
                rep.Message = "Invalid signature";
                return rep;
            }

            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(_thisTp, balanceReq.username);
            var gameType = _tpCustInfoBLL.GetCustGotoGameType(tpCustInfo.CustId, _thisTp);
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, balanceReq.username, gameType);
            rep.Status = 0;
            rep.Message = "Success";
            rep.Balance = tpCustBalance.Balance;

            _log2File.Info($"{JsonConvert.SerializeObject(balanceReq)}\r\n{JsonConvert.SerializeObject(rep)}", logName);
            return rep;
        }

        #region Slot Action

        [Route("[controller]/bet")]
        [Route("[controller]/{sitename}/bet")]
        public Rep Bet([FromForm] Dictionary<string, string> betReqq, string sitename = "")
        {
            var logName = $"{_tpName}_Bet";
            var gameType = (int)GameType.Joker_RNG;
            var betReq = JsonConvert.DeserializeObject<BetReq>(JsonConvert.SerializeObject(betReqq));

            if (!CheckSign(betReq.appid, betReq.hash, betReqq, sitename))
            {
                return new Rep
                {
                    Status = 5,
                    Message = "Invalid signature"
                };
            }

            Rep rep = new Rep
            {
                Status = 1000,
                Message = "Other"
            };

            if (betReq.amount < 0)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, betReq.username, gameType);
                rep.Balance = tpCustBalance.Balance;
                rep.Status = 4;
                rep.Message = "InvalidParameters";
                return rep;
            }

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(_thisTp, betReq.username);
            if (tpCheck == null)
            {
                rep.Status = 1000;
                rep.Message = "Other";
            }
            else if (tpCheck.Locked)
            {
                rep.Status = 1000;
                rep.Message = "Other";
            }
            else
            {
                try
                {
                    if (_bettingBLL.PlaceBetRNG_Joker(betReq, out decimal tpAffterAmount) > 0)
                    {
                        rep.Status = 0;
                        rep.Message = "Success";
                        if (betReq.amount == 0)
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, betReq.username, gameType);
                            rep.Balance = tpCustBalance.Balance;
                        }
                        else
                        {
                            rep.Balance = tpAffterAmount;
                        }
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, betReq.username, gameType);
                        rep.Status = 100;
                        rep.Balance = tpCustBalance.Balance;
                        rep.Message = "Insufficient fund";
                    }
                    else
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, betReq.username, gameType);
                        rep.Balance = tpCustBalance.Balance;
                        rep.Status = 0;
                        rep.Message = "The Bet already existed";
                    }
                }
                catch (Exception ex)
                {
                    rep.Status = 1000;
                    rep.Message = "Other";
                    _log2File.Error($"{JsonConvert.SerializeObject(betReqq)}\r\n{JsonConvert.SerializeObject(rep)}\r\n{ex}", logName, ex);
                }
            }

            _log2File.Info($"{JsonConvert.SerializeObject(betReqq)}\r\n{JsonConvert.SerializeObject(rep)}", logName);
            return rep;
        }

        [Route("[controller]/settle-bet")]
        [Route("[controller]/{sitename}/settle-bet")]
        public Rep SettleBet([FromForm] Dictionary<string, string> settleBetReqq, string sitename = "")
        {
            var logName = $"{_tpName}_SettleBet";
            var gameType = (int)GameType.Joker_RNG;
            var settleBetReq = JsonConvert.DeserializeObject<SettleBetReq>(JsonConvert.SerializeObject(settleBetReqq));

            if (!CheckSign(settleBetReq.appid, settleBetReq.hash, settleBetReqq, sitename))
            {
                return new Rep
                {
                    Status = 5,
                    Message = "Invalid signature"
                };
            }

            Rep rep = new Rep
            {
                Status = 1000,
                Message = "Other"
            };

            if (settleBetReq.amount < 0)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, settleBetReq.username, gameType);
                rep.Balance = tpCustBalance.Balance;
                rep.Status = 4;
                rep.Message = "InvalidParameters";
                return rep;
            }

            try
            {
                settleBetReq.type = "settled";
                int result = _settledBLL.SettledRNG_Joker(settleBetReq, out decimal tpAffterAmount);

                if (result > 0)
                {
                    if (settleBetReq.amount > 0)
                    {
                        rep.Status = 0;
                        rep.Message = "Success";
                        rep.Balance = tpAffterAmount;
                    }
                    else if (settleBetReq.amount == 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, settleBetReq.username, gameType);
                        rep.Balance = tpCustBalance.Balance;
                        rep.Status = 0;
                        rep.Message = "Success";
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, settleBetReq.username, gameType);
                    rep.Balance = tpCustBalance.Balance;
                    rep.Status = 0;
                    rep.Message = "Transaction can not be found";
                }
                else if (result == -2)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, settleBetReq.username, gameType);
                    rep.Balance = tpCustBalance.Balance;
                    rep.Status = 0;
                    rep.Message = "Success";
                }
            }
            catch (Exception ex)
            {
                rep.Status = 1000;
                rep.Message = "Other";
                _log2File.Error($"{JsonConvert.SerializeObject(settleBetReqq)}\r\n{JsonConvert.SerializeObject(rep)}\r\n{ex}", logName, ex);
            }

            _log2File.Info($"{JsonConvert.SerializeObject(settleBetReqq)}\r\n{JsonConvert.SerializeObject(rep)}", logName);
            return rep;
        }

        [Route("[controller]/cancel-bet")]
        [Route("[controller]/{sitename}/cancel-bet")]
        public Rep CancelBet([FromForm] Dictionary<string, string> cancelBetReqq, string sitename = "")
        {
            var logName = $"{_tpName}_CancelBet";
            var gameType = (int)GameType.Joker_RNG;
            _log2File.Info(JsonConvert.SerializeObject(cancelBetReqq), logName);

            var cancelBetReq = JsonConvert.DeserializeObject<CancelBetReq>(JsonConvert.SerializeObject(cancelBetReqq));
            if (!CheckSign(cancelBetReq.appid, cancelBetReq.hash, cancelBetReqq, sitename))
            {
                return new Rep
                {
                    Status = 5,
                    Message = "Invalid signature"
                };
            }

            Rep rep = new Rep
            {
                Status = 1000,
                Message = "Other"
            };

            int result = _settledBLL.CancelBetRNG_Joker(cancelBetReq, out decimal tpAffterAmount);

            if (result > 0)
            {
                rep.Status = 0;
                rep.Message = "Success";
                rep.Balance = tpAffterAmount;

                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, cancelBetReq.username, gameType);
                    rep.Balance = tpCustBalance.Balance;
                }
            }
            else if (result == -2)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, cancelBetReq.username, gameType);
                rep.Balance = tpCustBalance.Balance;
                rep.Status = 1000;
                rep.Message = "Bet was settled";
            }
            else if (result == -1)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, cancelBetReq.username, gameType);
                rep.Balance = tpCustBalance.Balance;
                rep.Status = 0;
                rep.Message = "Success";
            }

            _log2File.Info($"{JsonConvert.SerializeObject(cancelBetReqq)}\r\n{JsonConvert.SerializeObject(rep)}", logName);
            return rep;
        }

        #endregion

        #region Bonus And Jackpot Action

        [Route("[controller]/bonus-win")]
        [Route("[controller]/{sitename}/bonus-win")]
        public Rep Bonus([FromForm] Dictionary<string, string> bonusReqq, string sitename = "")
        {
            var logName = $"{_tpName}_Bonus";
            var gameType = (int)GameType.Joker_RNG;
            _log2File.Info(JsonConvert.SerializeObject(bonusReqq), logName);

            var settleBetReq = JsonConvert.DeserializeObject<SettleBetReq>(JsonConvert.SerializeObject(bonusReqq));
            Rep rep = new Rep
            {
                Status = 1000,
                Message = "Other"
            };

            if (settleBetReq.amount < 0)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, settleBetReq.username, gameType);
                rep.Balance = tpCustBalance.Balance;
                rep.Status = 4;
                rep.Message = "InvalidParameters";
                return rep;
            }

            if (!CheckSign(settleBetReq.appid, settleBetReq.hash, bonusReqq, sitename))
            {
                return new Rep
                {
                    Status = 5,
                    Message = "Invalid signature"
                };
            }

            settleBetReq.type = "bonus";
            int result = _settledBLL.SettledRNG_Joker(settleBetReq, out decimal tpAffterAmount);

            if (result > 0)
            {
                if (settleBetReq.amount > 0)
                {
                    rep.Status = 0;
                    rep.Message = "Success";
                    rep.Balance = tpAffterAmount;
                }
                else if (settleBetReq.amount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, settleBetReq.username, gameType);
                    rep.Status = 0;
                    rep.Message = "Success";
                    rep.Balance = tpAffterAmount;
                }
            }
            else if (result == -1)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, settleBetReq.username, gameType);
                rep.Balance = tpCustBalance.Balance;
                rep.Status = 0;
                rep.Message = "Success";
            }
            else if (result == -2)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, settleBetReq.username, gameType);
                rep.Balance = tpCustBalance.Balance;
                rep.Status = 0;
                rep.Message = "Success";
            }

            _log2File.Info(JsonConvert.SerializeObject(rep), logName);
            return rep;
        }

        [Route("[controller]/jackpot-win")]
        [Route("[controller]/{sitename}/jackpot-win")]
        public Rep Jackpot([FromForm] Dictionary<string, string> jackpotReqq, string sitename = "")
        {
            var logName = $"{_tpName}_Jackpot";
            var gameType = (int)GameType.Joker_RNG;
            _log2File.Info(JsonConvert.SerializeObject(jackpotReqq), logName);

            var settleBetReq = JsonConvert.DeserializeObject<SettleBetReq>(JsonConvert.SerializeObject(jackpotReqq));
            Rep rep = new Rep
            {
                Status = 1000,
                Message = "Other"
            };

            if (settleBetReq.amount < 0)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, settleBetReq.username, gameType);
                rep.Balance = tpCustBalance.Balance;
                rep.Status = 4;
                rep.Message = "InvalidParameters";
                return rep;
            }

            if (!CheckSign(settleBetReq.appid, settleBetReq.hash, jackpotReqq, sitename))
            {
                return new Rep
                {
                    Status = 5,
                    Message = "Invalid signature"
                };
            }

            settleBetReq.type = "jackpot";
            int result = _settledBLL.SettledRNG_Joker(settleBetReq, out decimal tpAffterAmount);

            if (result > 0)
            {
                if (settleBetReq.amount > 0)
                {
                    rep.Status = 0;
                    rep.Message = "Success";
                    rep.Balance = tpAffterAmount;
                }
                else if (settleBetReq.amount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, settleBetReq.username, gameType);
                    rep.Status = 0;
                    rep.Message = "Success";
                    rep.Balance = tpAffterAmount;
                }
            }
            else if (result == -1)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, settleBetReq.username, gameType);
                rep.Balance = tpCustBalance.Balance;
                rep.Status = 0;
                rep.Message = "Success";
            }
            else if (result == -2)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, settleBetReq.username, gameType);
                rep.Balance = tpCustBalance.Balance;
                rep.Status = 0;
                rep.Message = "Success";
            }

            _log2File.Info(JsonConvert.SerializeObject(rep), logName);
            return rep;
        }

        #endregion

        #region Fish Action

        [Route("[controller]/transaction")]
        [Route("[controller]/{sitename}/transaction")]
        public Rep Transaction([FromForm] Dictionary<string, string> parameter, string sitename = "")
        {
            return FishBet(JokerFishTransType.Transaction, parameter, sitename);
        }

        [Route("[controller]/withdraw")]
        [Route("[controller]/{sitename}/withdraw")]
        public Rep Withdraw([FromForm] Dictionary<string, string> parameter, string sitename = "")
        {
            return FishBet(JokerFishTransType.Withdraw, parameter, sitename);
        }

        [Route("[controller]/deposit")]
        [Route("[controller]/{sitename}/deposit")]
        public Rep Deposit([FromForm] Dictionary<string, string> parameter, string sitename = "")
        {
            return FishBet(JokerFishTransType.Deposit, parameter, sitename);
        }

        #endregion

        #region Tournament Action

        /// <summary>
        /// 參加錦標賽
        /// </summary>
        /// <returns></returns>
        [Route("[controller]/join-tournament")]
        [Route("[controller]/{sitename}/join-tournament")]
        public async Task<JokerResponse> JoinTournamentAsync([FromForm] Dictionary<string, string> parameter, string sitename = "")
        {
            var request = JsonConvert.DeserializeObject<JoinAndWinRequest>(JsonConvert.SerializeObject(parameter));

            if (!CheckSign(request.appid, request.hash, parameter, sitename))
            {
                return new JokerResponse
                {
                    Status = JokerErrorCode.InvalidSignature,
                    Message = "Invalid signature"
                };
            }

            return await _jokerBll.JoinTournament(request);
        }

        /// <summary>
        /// 派彩錦標賽
        /// </summary>
        /// <returns></returns>
        [Route("[controller]/win-tournament")]
        [Route("[controller]{sitename}/win-tournament")]
        public async Task<JokerResponse> WinTournamentAsync([FromForm] Dictionary<string, string> parameter, string sitename = "")
        {
            var request = JsonConvert.DeserializeObject<JoinAndWinRequest>(JsonConvert.SerializeObject(parameter));

            if (!CheckSign(request.appid, request.hash, parameter, sitename))
            {
                return new JokerResponse
                {
                    Status = JokerErrorCode.InvalidSignature,
                    Message = "Invalid signature"
                };
            }

            return await _jokerBll.WinTournament(request);
        }

        /// <summary>
        /// 取消參加錦標賽
        /// </summary>
        /// <returns></returns>
        [Route("[controller]/cancel-join-tournament")]
        [Route("[controller]/{sitename}/cancel-join-tournament")]
        public async Task<JokerResponse> CancelTournamentAsync([FromForm] Dictionary<string, string> parameter, string sitename = "")
        {
            var request = JsonConvert.DeserializeObject<CancelRequest>(JsonConvert.SerializeObject(parameter));

            if (!CheckSign(request.appid, request.hash, parameter, sitename))
            {
                return new JokerResponse
                {
                    Status = JokerErrorCode.InvalidSignature,
                    Message = "Invalid signature"
                };
            }

            return await _jokerBll.CancelTournament(request);
        }

        #endregion

        #region Private Methods

        private string GetMD5Signature(string rawData)
        {
            using MD5 md5Hash = MD5.Create();
            string md5Content = GetMd5Hash(md5Hash, rawData);
            return md5Content;
        }

        private static string GetMd5Hash(MD5 md5Hash, string rawData)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        private Rep FishBet(JokerFishTransType transType, Dictionary<string, string> parameter, string sitename)
        {
            try
            {
                _log2File.Info(JsonConvert.SerializeObject(parameter), $"Joker_{transType}");
                var request = JsonConvert.DeserializeObject<BettingDataFish_Joker>(JsonConvert.SerializeObject(parameter));

                if (request.amount < 0)
                {
                    return new Rep
                    {
                        Balance = _tpCustInfoBLL.GetBalance(_thisTp, request.username, (int)GameType.Joker_Fish).Balance,
                        Status = 4,
                        Message = "InvalidParameters"
                    };
                }

                request.amount = _tools.Floor(request.amount, 2);
                parameter["amount"] = request.amount.ToString("0.00");

                if (transType == JokerFishTransType.Transaction)
                {
                    request.result = _tools.Floor(request.result, 2);
                    parameter["result"] = request.result.ToString("0.00");

                    request.startbalance = _tools.Floor(request.startbalance, 2);
                    parameter["startbalance"] = request.startbalance.ToString("0.00");

                    request.endbalance = _tools.Floor(request.endbalance, 2);
                    parameter["endbalance"] = request.endbalance.ToString("0.00");
                }

                if (!CheckSign(request.appid, request.hash, parameter, sitename))
                {
                    return new Rep
                    {
                        Status = 5,
                        Message = "Invalid signature"
                    };
                }

                if (transType == JokerFishTransType.Withdraw)
                {
                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(_thisTp, request.username);
                    if (tpCheck == null || tpCheck.Locked)
                    {
                        return new Rep
                        {
                            Status = 1000,
                            Message = "Other"
                        };
                    }
                }

                decimal tpAffterAmount;
                var result = transType switch
                {
                    JokerFishTransType.Withdraw => _bettingBLL.WithdrawFish_Joker(request, out tpAffterAmount),
                    JokerFishTransType.Deposit => _settledBLL.DepositFish_Joker(request, out tpAffterAmount),
                    JokerFishTransType.Transaction => _settledBLL.TransactionFish_Joker(request, out tpAffterAmount),
                    _ => throw new NotImplementedException(),
                };

                if (result > 0)
                {
                    return new Rep
                    {
                        Balance = tpAffterAmount,
                        Status = 0,
                        Message = "Success"
                    };
                }
                // InsufficientFund
                if (result == 0)
                {
                    return new Rep
                    {
                        Balance = _tpCustInfoBLL.GetBalance(_thisTp, request.username, (int)GameType.Joker_Fish).Balance,
                        Status = 100,
                        Message = "InsufficientFund"
                    };
                }
                // Duplicated
                if (result == -1)
                {
                    return new Rep
                    {
                        Balance = _tpCustInfoBLL.GetBalance(_thisTp, request.username, (int)GameType.Joker_Fish).Balance,
                        Status = 0,
                        Message = "Success"
                    };
                }

                return new Rep
                {
                    Status = 1000,
                    Message = "Other"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new Rep
                {
                    Status = 1000,
                    Message = "Other"
                };
            }
        }

        private bool CheckSign(string appid, string hash, Dictionary<string, string> data,string siteName)
        {
            var config = GetSiteConfig(siteName);
            var appIdInfo = new Dictionary<string, string>();
            foreach (var item in config.CurrencyData)
            {
                var currency = item.Key;
                appIdInfo.Add(config.CurrencyData[item.Key].AppID, config.CurrencyData[item.Key].Secret);
                if (config.CurrencyData[item.Key].AppID != config.CurrencyData[item.Key].AppID_Test)
                {
                    appIdInfo.Add(config.CurrencyData[item.Key].AppID_Test, config.CurrencyData[item.Key].Secret_Test);
                }
            }
            var secret = appIdInfo.ContainsKey(appid) ? appIdInfo[appid] : "";

            data.Remove("hash");
            data.Remove("extendedinfo");
            if (data.ContainsKey("amount"))
            {
                data["amount"] = String.Format("{0:0.00}", Convert.ToDecimal(data["amount"]));
            }
            var rawData = $"{string.Join("&", data.OrderBy(kp => kp.Key).Select(kp => $"{kp.Key}={kp.Value}"))}{secret}";

            return GetMD5Signature(rawData) == hash;
        }

        private static (string Token, string gameCode) GetToken(string token)
        {
            try
            {
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(token));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                return (sessionKeyArr[0], sessionKeyArr[1]);
            }
            catch
            {
                return (null, null);
            }
        }

        private JokerConfig GetSiteConfig(string siteName)
        {
            if (siteName == "")
            {
                return _defaultConfig;
            }

            return _siteNameConfigs.TryGetValue(siteName, out var result) ? result : _defaultConfig;
        }
        #endregion
    }
}