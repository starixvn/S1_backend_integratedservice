using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class PPController : ControllerBase
    {
        private readonly Tp _thisTp;
        private readonly Log2File _log2File;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly AuthBLL _authBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Tools _tools;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpRequestFailBLL _tpRequestFailBll;
        private readonly string _tpName = Tp.PP.ToString();
        private readonly Dictionary<int, PPConfig> _siteConfigDic;

        public PPController(IServiceProvider provider)
        {
            _thisTp = Tp.PP;
            _log2File = provider.GetRequiredService<Log2File>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpRequestFailBll = provider.GetRequiredService<TpRequestFailBLL>();
            // 取共用，設定依機器分為(nova、aiking)
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<PPConfig>(_tpName);
        }

        #region Request

        public class AuthenticateReq
        {
            public string gameId { get; set; }
            public string providerId { get; set; }
            public string ipAddress { get; set; }
            public string token { get; set; }
        }

        public class BalanceReq
        {
            public string providerId { get; set; }
            public string userId { get; set; }
        }

        public class BetReq : BettingDataRNG_PP
        {

        }

        public partial class PromoWinReq : PromoBonus
        {

        }

        public class RefundReq : BettingDataRNG_PP
        {
            public string providerId { get; set; }
        }

        public class AdjustmentReq : BettingDataRNG_PP
        {
            public string hash { get; set; }
            public string token { get; set; }
            public string providerId { get; set; }
        }

        #endregion

        #region Response

        public class AuthenticateResp
        {
            private decimal _cash = 0m;
            public string userId { get; set; }
            public string currency { get; set; }
            public decimal cash
            {
                get
                {
                    decimal.TryParse(_cash.ToString("#.00"), out _cash);
                    return _cash;
                }
                set
                {
                    _cash = value;
                }
            }
            public decimal bonus { get; set; }
            public int error { get; set; }
            public string description { get; set; }
        }

        public class BalanceResp
        {
            private decimal _cash = 0m;
            public string currency { get; set; }
            public decimal cash
            {
                get
                {
                    decimal.TryParse(_cash.ToString("#.00"), out _cash);
                    return _cash;
                }
                set
                {
                    _cash = value;
                }
            }
            public decimal bonus { get; set; }
            public int error { get; set; }
            public string description { get; set; }
        }

        public class BetResp
        {
            private decimal _cash = 0m;
            public string transactionId { get; set; }
            public string currency { get; set; }
            public decimal cash
            {
                get
                {
                    decimal.TryParse(_cash.ToString("#.00"), out _cash);
                    return _cash;
                }
                set
                {
                    _cash = value;
                }
            }
            public decimal bonus { get; set; }
            public decimal usedPromo { get; set; }
            public int error { get; set; }
            public string description { get; set; }
        }

        public class PayoutResp
        {
            private decimal _cash = 0m;
            public string transactionId { get; set; }
            public string currency { get; set; }
            public decimal cash
            {
                get
                {
                    decimal.TryParse(_cash.ToString("#.00"), out _cash);
                    return _cash;
                }
                set
                {
                    _cash = value;
                }
            }
            public decimal bonus { get; set; }
            public int error { get; set; }
            public string description { get; set; }
        }

        public class EndRoundResp
        {
            private decimal _cash = 0m;
            public decimal cash
            {
                get
                {
                    decimal.TryParse(_cash.ToString("#.00"), out _cash);
                    return _cash;
                }
                set
                {
                    _cash = value;
                }
            }
            public decimal bonus { get; set; }
            public int error { get; set; }
            public string description { get; set; }
        }

        public class RefundResp
        {
            public string transactionId { get; set; }
            public int error { get; set; }
            public string description { get; set; }
        }

        public class AdjustmentResp
        {
            private decimal _cash = 0m;
            public string transactionId { get; set; }
            public string currency { get; set; }
            public decimal cash
            {
                get
                {
                    decimal.TryParse(_cash.ToString("#.00"), out _cash);
                    return _cash;
                }
                set
                {
                    _cash = value;
                }
            }
            public decimal bonus { get; set; }
            public int error { get; set; }
            public string description { get; set; }
        }

        #endregion

        [Route("[controller]/authenticate")]
        [Route("[controller]/{agentId}/authenticate")]
        public AuthenticateResp Authenticate([FromForm] Dictionary<string, string> authenticateReqq, string agentId)
        {
            var logName = $"{_tpName}_Authenticate";

            AuthenticateResp authenticateResp = new()
            {
                error = 100,
                description = "Internal server error."
            };

            try
            {
                if (HashCheck(authenticateReqq, agentId))
                {
                    AuthenticateReq authenticateReq = JsonConvert.DeserializeObject<AuthenticateReq>(JsonConvert.SerializeObject(authenticateReqq));

                    if (_authBLL.Authenticate(authenticateReq.token, out TpCustInfoAuth tpCustInfoAuth))
                    {
                        var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, tpCustInfoAuth.TpUserName);
                        if (tpCheck == null)
                        {
                            authenticateResp.error = 2;
                            authenticateResp.description = "Player authentication failed due to invalid, not found or expired token.";
                        }
                        else if (tpCheck.Locked)
                        {
                            authenticateResp.error = 6;
                            authenticateResp.description = "Player is frozen.";
                        }
                        else
                        {
                            var gameType = _thirdPartyBLL.GetGameType_PP(authenticateReq.gameId);
                            _tpCustInfoBLL.SetCustGotoGame(tpCustInfoAuth.CustId, (int)_thisTp, (int)gameType);
                            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, tpCustInfoAuth.TpUserName, (int)gameType);
                            authenticateResp.userId = tpCustInfoAuth.TpUserName;
                            authenticateResp.currency = tpCustInfoAuth.TpCurrencyId;
                            authenticateResp.cash = tpCustBalance.Balance;
                            authenticateResp.bonus = 0;
                            authenticateResp.error = 0;
                            authenticateResp.description = "Success.";
                        }
                    }
                    else
                    {
                        authenticateResp.error = 4;
                        authenticateResp.description = "Player authentication failed due to invalid, not found or expired token.";
                    }
                }
                else
                {
                    authenticateResp.error = 5;
                    authenticateResp.description = "Invalid hash code.";
                }
            }
            catch (Exception ex)
            {
                authenticateResp.error = 100;
                _log2File.Error($"{JsonConvert.SerializeObject(authenticateReqq)}\r\n{JsonConvert.SerializeObject(authenticateResp)}\r\n{ex.Message}", logName, ex);
            }

            _log2File.Info($"{JsonConvert.SerializeObject(authenticateReqq)}\r\n{JsonConvert.SerializeObject(authenticateResp)}", logName);
            return authenticateResp;
        }

        [Route("[controller]/balance")]
        [Route("[controller]/{agentId}/balance")]
        public BalanceResp Balance([FromForm] Dictionary<string, string> balanceReqq, string agentId)
        {
            var logName = $"{_tpName}_Balance";

            BalanceResp balanceResp = new()
            {
                error = 100,
                description = "Internal server error."
            };

            try
            {
                if (HashCheck(balanceReqq, agentId))
                {
                    BalanceReq balanceReq = JsonConvert.DeserializeObject<BalanceReq>(JsonConvert.SerializeObject(balanceReqq));

                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, balanceReq.userId);
                    if (tpCheck == null)
                    {
                        balanceResp.error = 2;
                        balanceResp.description = "Player authentication failed due to invalid, not found or expired token.";
                    }
                    else if (tpCheck.Locked)
                    {
                        balanceResp.error = 6;
                        balanceResp.description = "Player is frozen.";
                    }
                    else
                    {
                        //var gameType = thirdPartyBLL.GetGameType_PP(balanceReq.gameId);
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, balanceReq.userId);
                        var gameType = _tpCustInfoBLL.GetCustGotoGameType(tpCustInfo.CustId, (int)_thisTp);
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, balanceReq.userId, gameType);

                        balanceResp.currency = tpCustBalance.TpCurrencyId;
                        balanceResp.cash = tpCustBalance.Balance;
                        balanceResp.bonus = 0;
                        balanceResp.error = 0;
                        balanceResp.description = "Success.";
                    }
                }
                else
                {
                    balanceResp.error = 5;
                    balanceResp.description = "Invalid hash code.";
                }
            }
            catch (Exception ex)
            {
                balanceResp.error = 100;
                _log2File.Error($"{JsonConvert.SerializeObject(balanceReqq)}\r\n{JsonConvert.SerializeObject(balanceResp)}\r\n{ex.Message}", logName, ex);
            }

            _log2File.Info($"{JsonConvert.SerializeObject(balanceReqq)}\r\n{JsonConvert.SerializeObject(balanceResp)}", logName);
            return balanceResp;
        }

        [Route("[controller]/bet")]
        [Route("[controller]/{agentId}/bet")]
        public BetResp Bet([FromForm] Dictionary<string, string> betReqq, string agentId)
        {
            var logName = $"{_tpName}_Bet";

            BetResp betResp = new()
            {
                error = 100,
                description = "Internal server error."
            };

            try
            {
                if (HashCheck(betReqq, agentId))
                {
                    BetReq betReq = JsonConvert.DeserializeObject<BetReq>(JsonConvert.SerializeObject(betReqq));
                    betResp.transactionId = betReq.reference;

                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, betReq.userId);
                    if (tpCheck == null)
                    {
                        betResp.error = 2;
                        betResp.description = "Player authentication failed due to invalid, not found or expired token.";
                    }
                    else if (tpCheck.Locked)
                    {
                        betResp.error = 6;
                        betResp.description = "Player is frozen.";
                    }
                    else
                    {
                        var gameType = _thirdPartyBLL.GetGameType_PP(betReq.gameId);
                        if (_bettingBLL.PlaceBetRNG_PP(betReq, (int)gameType, out decimal tpAffterAmount) > 0)
                        {
                            if (betReq.amount == 0)
                            {
                                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, betReq.userId, (int)gameType);
                                betResp.cash = tpCustBalance.Balance;
                                betResp.currency = tpCustBalance.TpCurrencyId;
                            }
                            else
                            {
                                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, betReq.userId, (int)gameType);
                                betResp.cash = tpAffterAmount;
                                betResp.currency = tpCustBalance.TpCurrencyId;
                            }
                            betResp.bonus = 0;
                            betResp.error = 0;
                            betResp.description = "Success.";
                        }
                        else if (tpAffterAmount < 0)
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, betReq.userId, (int)gameType);
                            betResp.cash = tpCustBalance.Balance;
                            betResp.currency = tpCustBalance.TpCurrencyId;
                            betResp.bonus = 0;
                            betResp.error = 1;
                            betResp.description = "Insufficient balance.";
                        }
                        else
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, betReq.userId, (int)gameType);
                            betResp.cash = tpCustBalance.Balance;
                            betResp.currency = tpCustBalance.TpCurrencyId;
                            betResp.bonus = 0;
                            betResp.error = 100;
                            betResp.description = "Internal server error.";
                        }
                    }
                }
                else
                {
                    betResp.error = 5;
                    betResp.description = "Invalid hash code.";
                }
            }
            catch (Exception ex)
            {
                betResp.error = 100;
                _log2File.Error($"{JsonConvert.SerializeObject(betReqq)}\r\n{JsonConvert.SerializeObject(betResp)}\r\n{ex.Message}", logName, ex);
            }

            _log2File.Info($"{JsonConvert.SerializeObject(betReqq)}\r\n{JsonConvert.SerializeObject(betResp)}", logName);
            return betResp;
        }

        [Route("[controller]/result")]
        [Route("[controller]/{agentId}/result")]
        public PayoutResp Result([FromForm] Dictionary<string, string> resultReqq, string agentId)
        {
            var logName = $"{_tpName}_Result";

            PayoutResp resultResp = new()
            {
                error = 100,
                description = "Internal server error."
            };

            try
            {
                if (HashCheck(resultReqq, agentId))
                {
                    BetReq betReq = JsonConvert.DeserializeObject<BetReq>(JsonConvert.SerializeObject(resultReqq));

                    resultResp.transactionId = betReq.reference;
                    betReq.type = BettransType.Payout;
                    var gameType = _thirdPartyBLL.GetGameType_PP(betReq.gameId);
                    if (_settledBLL.PayoutRNG_PP(betReq, (int)gameType, out decimal tpAffterAmount) > 0)
                    {
                        if (betReq.amount == 0)
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, betReq.userId, (int)gameType);
                            resultResp.cash = tpCustBalance.Balance;
                            resultResp.currency = tpCustBalance.TpCurrencyId;
                        }
                        else
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, betReq.userId, (int)gameType);
                            resultResp.cash = tpAffterAmount;
                            resultResp.currency = tpCustBalance.TpCurrencyId;
                        }
                        resultResp.bonus = 0;
                        resultResp.error = 0;
                        resultResp.description = "Success.";
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, betReq.userId, (int)gameType);
                        resultResp.cash = tpCustBalance.Balance;
                        resultResp.currency = tpCustBalance.TpCurrencyId;
                        resultResp.bonus = 0;
                        resultResp.error = 1;
                        resultResp.description = "Insufficient balance.";
                    }
                    else
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, betReq.userId, (int)gameType);
                        resultResp.cash = tpCustBalance.Balance;
                        resultResp.currency = tpCustBalance.TpCurrencyId;
                        resultResp.bonus = 0;
                        resultResp.error = 0;
                        resultResp.description = "Success.";
                    }
                }
                else
                {
                    resultResp.error = 5;
                    resultResp.description = "Invalid hash code.";
                }
            }
            catch (Exception ex)
            {
                resultResp.error = 100;
                _log2File.Error($"{JsonConvert.SerializeObject(resultReqq)}\r\n{JsonConvert.SerializeObject(resultResp)}\r\n{ex.Message}", logName, ex);
            }

            _log2File.Info($"{JsonConvert.SerializeObject(resultReqq)}\r\n{JsonConvert.SerializeObject(resultResp)}", logName);
            return resultResp;
        }

        [Route("[controller]/bonusWin")]
        [Route("[controller]/{agentId}/bonusWin")]
        public PayoutResp BonusWin([FromForm] Dictionary<string, string> bonusWinReqq, string agentId)
        {
            var logName = $"{_tpName}_BonusWin";

            PayoutResp payoutResp = new()
            {
                error = 100,
                description = "Internal server error."
            };

            try
            {
                if (HashCheck(bonusWinReqq, agentId))
                {
                    BetReq betReq = JsonConvert.DeserializeObject<BetReq>(JsonConvert.SerializeObject(bonusWinReqq));
                    payoutResp.transactionId = betReq.reference;
                    betReq.type = BettransType.Bonus;
                    var gameType = (int)GameType.PP_RNG;
                    _settledBLL.BonusWinRNG_PP(betReq, out decimal tpAffterAmount);
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, betReq.userId, (int)gameType);
                    payoutResp.cash = tpAffterAmount;
                    payoutResp.currency = tpCustBalance.TpCurrencyId;
                    payoutResp.bonus = 0;
                    payoutResp.error = 0;
                    payoutResp.description = "Success.";
                }
                else
                {
                    payoutResp.error = 5;
                    payoutResp.description = "Invalid hash code.";
                }
            }
            catch (Exception ex)
            {
                payoutResp.error = 100;
                _log2File.Error($"{JsonConvert.SerializeObject(bonusWinReqq)}\r\n{JsonConvert.SerializeObject(payoutResp)}\r\n{ex.Message}", logName, ex);
            }

            _log2File.Info($"{JsonConvert.SerializeObject(bonusWinReqq)}\r\n{JsonConvert.SerializeObject(payoutResp)}", logName);
            return payoutResp;
        }

        [Route("[controller]/JackpotWin")]
        [Route("[controller]/{agentId}/JackpotWin")]
        public PayoutResp JackpotWin([FromForm] Dictionary<string, string> jackpotWinReqq, string agentId)
        {
            var logName = $"{_tpName}_JackpotWin";

            PayoutResp payoutResp = new()
            {
                error = 100,
                description = "Internal server error."
            };

            try
            {
                if (HashCheck(jackpotWinReqq, agentId))
                {
                    BetReq betReq = JsonConvert.DeserializeObject<BetReq>(JsonConvert.SerializeObject(jackpotWinReqq));
                    payoutResp.transactionId = betReq.reference;
                    betReq.type = BettransType.Jackpot;
                    var gameType = _thirdPartyBLL.GetGameType_PP(betReq.gameId);
                    if (_settledBLL.PayoutRNG_PP(betReq, (int)gameType, out decimal tpAffterAmount) > 0)
                    {
                        if (betReq.amount == 0)
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, betReq.userId, (int)gameType);
                            payoutResp.cash = tpCustBalance.Balance;
                            payoutResp.currency = tpCustBalance.TpCurrencyId;
                        }
                        else
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, betReq.userId, (int)gameType);
                            payoutResp.cash = tpAffterAmount;
                            payoutResp.currency = tpCustBalance.TpCurrencyId;
                        }
                        payoutResp.bonus = 0;
                        payoutResp.error = 0;
                        payoutResp.description = "Success.";
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, betReq.userId, (int)gameType);
                        payoutResp.cash = tpCustBalance.Balance;
                        payoutResp.currency = tpCustBalance.TpCurrencyId;
                        payoutResp.bonus = 0;
                        payoutResp.error = 1;
                        payoutResp.description = "Insufficient balance.";
                    }
                    else
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, betReq.userId, (int)gameType);
                        payoutResp.cash = tpCustBalance.Balance;
                        payoutResp.currency = tpCustBalance.TpCurrencyId;
                        payoutResp.bonus = 0;
                        payoutResp.error = 0;
                        payoutResp.description = "Success.";
                    }
                }
                else
                {
                    payoutResp.error = 5;
                    payoutResp.description = "Invalid hash code.";
                }
            }
            catch (Exception ex)
            {
                payoutResp.error = 100;
                _log2File.Error($"{JsonConvert.SerializeObject(jackpotWinReqq)}\r\n{JsonConvert.SerializeObject(payoutResp)}\r\n{ex.Message}", logName, ex);
            }

            _log2File.Info($"{JsonConvert.SerializeObject(jackpotWinReqq)}\r\n{JsonConvert.SerializeObject(payoutResp)}", logName);
            return payoutResp;
        }

        [Route("[controller]/promoWin")]
        [Route("[controller]/{agentId}/promoWin")]
        public PayoutResp PromoWin([FromForm] Dictionary<string, string> promoWinReqq, string agentId)
        {
            var logName = $"{_tpName}_PromoWin";

            PayoutResp payoutResp = new()
            {
                error = 100,
                description = "Internal server error."
            };

            try
            {
                if (HashCheck(promoWinReqq, agentId))
                {
                    PromoWinReq promoWinReq = JsonConvert.DeserializeObject<PromoWinReq>(JsonConvert.SerializeObject(promoWinReqq));
                    payoutResp.transactionId = promoWinReq.Reference;

                    if (_settledBLL.PromoBonus_PP(promoWinReq, out decimal tpAffterAmount) > 0)
                    {
                        if (promoWinReq.Amount == 0)
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, promoWinReq.UserId, (int)GameType.PP_RNG);
                            payoutResp.cash = tpCustBalance.Balance;
                            payoutResp.currency = tpCustBalance.TpCurrencyId;
                        }
                        else
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, promoWinReq.UserId, (int)GameType.PP_RNG);
                            payoutResp.cash = tpAffterAmount;
                            payoutResp.currency = tpCustBalance.TpCurrencyId;
                        }
                        payoutResp.bonus = 0;
                        payoutResp.error = 0;
                        payoutResp.description = "Success.";
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, promoWinReq.UserId, (int)GameType.PP_RNG);
                        payoutResp.cash = tpCustBalance.Balance;
                        payoutResp.currency = tpCustBalance.TpCurrencyId;
                        payoutResp.bonus = 0;
                        payoutResp.error = 1;
                        payoutResp.description = "Insufficient balance.";
                    }
                    else
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, promoWinReq.UserId, (int)GameType.PP_RNG);
                        payoutResp.cash = tpCustBalance.Balance;
                        payoutResp.currency = tpCustBalance.TpCurrencyId;
                        payoutResp.bonus = 0;
                        payoutResp.error = 0;
                        payoutResp.description = "Success.";
                    }
                }
                else
                {
                    payoutResp.error = 5;
                    payoutResp.description = "Invalid hash code.";
                }
            }
            catch (Exception ex)
            {
                payoutResp.error = 100;
                _log2File.Error($"{JsonConvert.SerializeObject(promoWinReqq)}\r\n{JsonConvert.SerializeObject(payoutResp)}\r\n{ex.Message}", logName, ex);
            }

            _log2File.Info($"{JsonConvert.SerializeObject(promoWinReqq)}\r\n{JsonConvert.SerializeObject(payoutResp)}", logName);
            return payoutResp;
        }

        [Route("[controller]/endRound")]
        [Route("[controller]/{agentId}/endRound")]
        public EndRoundResp EndRound([FromForm] Dictionary<string, string> endRoundReqq, string agentId)
        {
            var logName = $"{_tpName}_EndRound";

            var endRoundResp = new EndRoundResp
            {
                error = 100,
                description = "Internal server error."
            };
            var betReq = JsonConvert.DeserializeObject<BetReq>(JsonConvert.SerializeObject(endRoundReqq));

            try
            {
                if (HashCheck(endRoundReqq, agentId))
                {
                    var gameType = _thirdPartyBLL.GetGameType_PP(betReq.gameId);

                    var result = _tpUnSettledBLL.InsertTpUnsettle(_thisTp, gameType, betReq.roundId.ToString());
                    if (result > 0)
                    {
                        endRoundResp.cash = 0;
                        endRoundResp.bonus = 0;
                        endRoundResp.error = 0;
                        endRoundResp.description = "Success.";
                    }
                }
                else
                {
                    endRoundResp.error = 5;
                    endRoundResp.description = "Invalid hash code.";
                }
            }
            catch (Exception ex)
            {
                endRoundResp.error = 100;
                _tpRequestFailBll.SetRequestFail(RequestFailureType.Tp, Tp.PP, TpRequestFailureCategory.EndRound, JsonConvert.SerializeObject(betReq), ex.Message);
                _log2File.Error($"{JsonConvert.SerializeObject(endRoundReqq)}\r\n{JsonConvert.SerializeObject(endRoundResp)}\r\n{ex.Message}", logName, ex);
            }

            _log2File.Info($"{JsonConvert.SerializeObject(endRoundReqq)}\r\n{JsonConvert.SerializeObject(endRoundResp)}", logName);
            return endRoundResp;
        }

        [Route("[controller]/refund")]
        [Route("[controller]/{agentId}/refund")]
        public RefundResp Refund([FromForm] Dictionary<string, string> refundReqq, string agentId)
        {
            var logName = $"{_tpName}_Refund";

            RefundResp refundResp = new()
            {
                error = 100,
                description = "Internal server error."
            };

            try
            {
                if (HashCheck(refundReqq, agentId))
                {
                    RefundReq refundReq = JsonConvert.DeserializeObject<RefundReq>(JsonConvert.SerializeObject(refundReqq));
                    refundResp.transactionId = $"{refundReq.reference}_refund";
                    var gameType = _thirdPartyBLL.GetGameType_PP(refundReq.gameId);
                    if (_settledBLL.CancelBetRNG_PP(refundReq, (int)gameType, out decimal tpAffterAmount) > 0)
                    {
                        refundResp.error = 0;
                        refundResp.description = "Success.";
                    }
                    else
                    {
                        refundResp.error = 0;
                        refundResp.description = "Success.";
                    }
                }
                else
                {
                    refundResp.error = 5;
                    refundResp.description = "Invalid hash code.";
                }
            }
            catch (Exception ex)
            {
                refundResp.error = 100;
                _log2File.Error($"{JsonConvert.SerializeObject(refundReqq)}\r\n{JsonConvert.SerializeObject(refundResp)}\r\n{ex.Message}", logName, ex);
            }

            _log2File.Info($"{JsonConvert.SerializeObject(refundReqq)}\r\n{JsonConvert.SerializeObject(refundResp)}", logName);
            return refundResp;
        }

        [Route("[controller]/adjustment")]
        [Route("[controller]/{agentId}/adjustment")]
        public AdjustmentResp Adjustment([FromForm] Dictionary<string, string> adjustmentReqq, string agentId)
        {
            var logName = $"{_tpName}_Adjustment";

            AdjustmentResp adjustmentResp = new()
            {
                error = 100,
                description = "Internal server error."
            };

            try
            {
                if (!HashCheck(adjustmentReqq, agentId))
                {
                    adjustmentResp.error = 5;
                    adjustmentResp.description = "Invalid hash code.";
                    _log2File.Error($"{JsonConvert.SerializeObject(adjustmentReqq)}\r\n{JsonConvert.SerializeObject(adjustmentResp)}", logName);
                    return adjustmentResp;
                }

                AdjustmentReq adjustmentReq = JsonConvert.DeserializeObject<AdjustmentReq>(JsonConvert.SerializeObject(adjustmentReqq));
                adjustmentResp.transactionId = adjustmentReq.reference;
                adjustmentResp.bonus = 0;

                var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, adjustmentReq.gameId);

                var result = _settledBLL.AdjustmentRNG_PP(adjustmentReq, (int)gameType, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, adjustmentReq.userId, (int)gameType);
                    if (tpAffterAmount == 0)
                    {
                        tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, adjustmentReq.userId, (int)gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }

                    adjustmentResp.cash = tpAffterAmount;
                    adjustmentResp.currency = tpCustBalance.TpCurrencyId;
                    adjustmentResp.error = 0;
                    adjustmentResp.description = "Success.";
                    _log2File.Info($"{JsonConvert.SerializeObject(adjustmentReqq)}\r\n{JsonConvert.SerializeObject(adjustmentResp)}", logName);
                    return adjustmentResp;
                }
                else if (tpAffterAmount < 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, adjustmentReq.userId, (int)gameType);
                    adjustmentResp.currency = tpCustBalance.TpCurrencyId;
                    adjustmentResp.cash = tpCustBalance.Balance;
                    adjustmentResp.error = 1;
                    adjustmentResp.description = "Insufficient balance.";
                }
            }
            catch (Exception ex)
            {
                adjustmentResp.error = 100;
                _log2File.Error($"{JsonConvert.SerializeObject(adjustmentReqq)}\r\n{JsonConvert.SerializeObject(adjustmentResp)}\r\n{ex.Message}", logName, ex);
            }

            _log2File.Error($"{JsonConvert.SerializeObject(adjustmentReqq)}\r\n{JsonConvert.SerializeObject(adjustmentResp)}", logName);
            return adjustmentResp;
        }

        private bool HashCheck(Dictionary<string, string> req, string agentId)
        {
            // 因SecretKey都是同一組共用，只取第一筆siteid = 0即可，如要使用其他欄位請先確認是否可共用
            var config = _siteConfigDic.FirstOrDefault().Value;
            var secureKey = config.SecureKey;

            // Stream 需要使用agentId取出對應site的SecretKey
            var siteName = GetSiteNameBySuffixAgentId(agentId);
            if (siteName != "")
            {
                var serverInfo = _thirdPartyBLL.GetServerInfoBySiteName(siteName);
                if (serverInfo is not { SiteId: > 0 })
                {
                    throw new Exception("HashCheck SiteName empty");
                }
                config = _siteConfigDic[serverInfo.SiteId];
                secureKey = config.SecureKey_Stream;
            }

            // 檢查Hash
            var hashKey = req["hash"];
            var dicP = req.Where(x => x.Key != "hash").OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}{secureKey}";

            return hashKey.Equals(_tools.ToMD5(p), StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSiteNameBySuffixAgentId(string agentId)
        {
            if (string.IsNullOrWhiteSpace(agentId))
            {
                return "";
            }

            var suffix = agentId.Split("_")[0];
            if (suffix.StartsWith("nova", StringComparison.OrdinalIgnoreCase))
            {
                suffix = "nova88";
            }
            return suffix;
        }

    }
}