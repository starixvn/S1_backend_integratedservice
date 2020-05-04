using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
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
        private readonly Tp thisTp;
        private readonly Log2File log2File;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly AuthBLL authBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly IHostEnvironment environment;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Tools tools;

        public PPController(IHostEnvironment environment, Log2File log2File, ThirdPartyBLL thirdPartyBLL, AuthBLL authBLL, TpCustInfoBLL tpCustInfoBLL, BettingBLL bettingBLL, SettledBLL settledBLL, Tools tools)
        {
            thisTp = Tp.PP;
            this.log2File = log2File;
            this.thirdPartyBLL = thirdPartyBLL;
            this.authBLL = authBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.environment = environment;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.tools = tools;
        }
        public class AuthenticateResp
        {
            public string userId { get; set; }
            public string currency { get; set; }
            public decimal cash { get; set; }
            public decimal bonus { get; set; }
            public int error { get; set; }
            public string description { get; set; }
        }

        public class AuthenticateReq
        {
            public string gameId { get; set; }
            public string providerId { get; set; }
            public string ipAddress { get; set; }
            public string token { get; set; }
        }

        [Route("[controller]/authenticate")]
        public AuthenticateResp Authenticate([FromForm]Dictionary<string, string> authenticateReqq)
        {
            AuthenticateResp authenticateResp = new AuthenticateResp();
            authenticateResp.error = 100;
            authenticateResp.description = "Internal server error.";

            try
            {
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var secureKey = thirdPartySetting.TpSetting["SecureKey"];

                if (hashCheck(authenticateReqq, secureKey))
                {
                    AuthenticateReq authenticateReq = JsonConvert.DeserializeObject<AuthenticateReq>(JsonConvert.SerializeObject(authenticateReqq));

                    if (authBLL.Authenticate(authenticateReq.token, out TpCustInfoAuth tpCustInfoAuth))
                    {
                        var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
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
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpCustInfoAuth.TpUserName);
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
                log2File.Error($"{JsonConvert.SerializeObject(authenticateReqq)}\r\n{JsonConvert.SerializeObject(authenticateResp)}\r\n{ex}", $"{thisTp}_Authenticate");
            }

            log2File.Info($"{JsonConvert.SerializeObject(authenticateReqq)}\r\n{JsonConvert.SerializeObject(authenticateResp)}", $"{thisTp}_Authenticate");
            return authenticateResp;
        }
        public class BalanceResp
        {
            public string currency { get; set; }
            public decimal cash { get; set; }
            public decimal bonus { get; set; }
            public int error { get; set; }
            public string description { get; set; }
        }

        public class BalanceReq
        {
            public string providerId { get; set; }
            public string userId { get; set; }
        }

        [Route("[controller]/balance")]
        public BalanceResp Balance([FromForm]Dictionary<string, string> balanceReqq)
        {
            //log2File.Info(JsonConvert.SerializeObject(balanceReqq), $"{thisTp}_Balance");
            BalanceResp balanceResp = new BalanceResp();
            balanceResp.error = 100;
            balanceResp.description = "Internal server error.";

            try
            {
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var secureKey = thirdPartySetting.TpSetting["SecureKey"];

                if (hashCheck(balanceReqq, secureKey))
                {
                    BalanceReq balanceReq = JsonConvert.DeserializeObject<BalanceReq>(JsonConvert.SerializeObject(balanceReqq));

                    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, balanceReq.userId);
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
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, balanceReq.userId);

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
                log2File.Error($"{JsonConvert.SerializeObject(balanceReqq)}\r\n{JsonConvert.SerializeObject(balanceResp)}\r\n{ex}", $"{thisTp}_Balance");
            }

            log2File.Info($"{JsonConvert.SerializeObject(balanceReqq)}\r\n{JsonConvert.SerializeObject(balanceResp)}", $"{thisTp}_Balance");
            return balanceResp;
        }


        public class BetReq : BettingDataRNG_PP
        {

        }

        public class BetResp
        {
            public string transactionId { get; set; }
            public string currency { get; set; }
            public decimal cash { get; set; }
            public decimal bonus { get; set; }
            public decimal usedPromo { get; set; }
            public int error { get; set; }
            public string description { get; set; }
        }

        [Route("[controller]/bet")]
        public BetResp Bet([FromForm]Dictionary<string, string> betReqq)
        {
            //log2File.Info(JsonConvert.SerializeObject(betReqq), $"{thisTp}_Bet");
            BetResp betResp = new BetResp();
            betResp.error = 100;
            betResp.description = "Internal server error.";

            try
            {
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var secureKey = thirdPartySetting.TpSetting["SecureKey"];

                if (hashCheck(betReqq, secureKey))
                {
                    BetReq betReq = JsonConvert.DeserializeObject<BetReq>(JsonConvert.SerializeObject(betReqq));
                    betResp.transactionId = betReq.reference;

                    //if (environment.IsDevelopment())
                    //{
                    //    string Content = @"{""amount"":""2.7"",""gameId"":""vs10firestrike"",""hash"":""519262f598f0dda6f877fdbc1450450e"",""ipAddress"":""182.1.200.141"",""platform"":""MOBILE"",""providerId"":""PragmaticPlay"",""reference"":""5eafadcc005c8e0008993879"",""roundDetails"":""spin"",""roundId"":""6020844666402"",""timestamp"":""1588571596318"",""userId"":""DNova88_7""}";
                    //    betReq = JsonConvert.DeserializeObject<BetReq>(Content);
                    //}

                    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, betReq.userId);
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
                        if (bettingBLL.PlaceBetRNG_PP(betReq, out decimal tpAffterAmount) > 0)
                        {
                            if (betReq.amount == 0)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, betReq.userId);
                                betResp.cash = tpCustBalance.Balance;
                                betResp.currency = tpCustBalance.TpCurrencyId;
                            }
                            else
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, betReq.userId);
                                betResp.cash = tpAffterAmount;
                                betResp.currency = tpCustBalance.TpCurrencyId;
                            }
                            betResp.bonus = 0;
                            betResp.error = 0;
                            betResp.description = "Success.";
                        }
                        else if (tpAffterAmount < 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, betReq.userId);
                            betResp.cash = tpCustBalance.Balance;
                            betResp.currency = tpCustBalance.TpCurrencyId;
                            betResp.bonus = 0;
                            betResp.error = 1;
                            betResp.description = "Insufficient balance.";
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, betReq.userId);
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
                log2File.Error($"{JsonConvert.SerializeObject(betReqq)}\r\n{JsonConvert.SerializeObject(betResp)}\r\n{ex}", $"{thisTp}_Bet");
            }

            log2File.Info($"{JsonConvert.SerializeObject(betReqq)}\r\n{JsonConvert.SerializeObject(betResp)}", $"{thisTp}_Bet");
            return betResp;
        }

        public class PayoutResp
        {
            public string transactionId { get; set; }
            public string currency { get; set; }
            public decimal cash { get; set; }
            public decimal bonus { get; set; }
            public int error { get; set; }
            public string description { get; set; }
        }

        [Route("[controller]/result")]
        public PayoutResp Result([FromForm]Dictionary<string, string> resultReqq)
        {
            //log2File.Info(JsonConvert.SerializeObject(resultReqq), $"{thisTp}_Result");
            PayoutResp resultResp = new PayoutResp();
            resultResp.error = 100;
            resultResp.description = "Internal server error.";

            try
            {
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var secureKey = thirdPartySetting.TpSetting["SecureKey"];

                if (hashCheck(resultReqq, secureKey))
                {
                    BetReq betReq = JsonConvert.DeserializeObject<BetReq>(JsonConvert.SerializeObject(resultReqq));

                    //if (environment.IsDevelopment())
                    //{
                    //    string Content = @"{""amount"":""32.2"",""gameId"":""vs25vegas"",""hash"":""a9e24484698072525090d72b354da64c"",""platform"":""WEB"",""providerId"":""PragmaticPlay"",""reference"":""5eaf966624aa9a0008024348"",""roundDetails"":""spin"",""roundId"":""2360806877"",""timestamp"":""1588565606805"",""userId"":""DNova88_196""}";
                    //    betReq = JsonConvert.DeserializeObject<BetReq>(Content);
                    //}

                    resultResp.transactionId = betReq.reference;
                    betReq.type = BettransType.Payout;
                    if (bettingBLL.PayoutRNG_PP(betReq, out decimal tpAffterAmount) > 0)
                    {
                        if (betReq.amount == 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, betReq.userId);
                            resultResp.cash = tpCustBalance.Balance;
                            resultResp.currency = tpCustBalance.TpCurrencyId;
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, betReq.userId);
                            resultResp.cash = tpAffterAmount;
                            resultResp.currency = tpCustBalance.TpCurrencyId;
                        }
                        resultResp.bonus = 0;
                        resultResp.error = 0;
                        resultResp.description = "Success.";
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, betReq.userId);
                        resultResp.cash = tpCustBalance.Balance;
                        resultResp.currency = tpCustBalance.TpCurrencyId;
                        resultResp.bonus = 0;
                        resultResp.error = 1;
                        resultResp.description = "Insufficient balance.";
                    }
                    else
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, betReq.userId);
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
                log2File.Error($"{JsonConvert.SerializeObject(resultReqq)}\r\n{JsonConvert.SerializeObject(resultResp)}\r\n{ex}", $"{thisTp}_Result");
            }

            log2File.Info($"{JsonConvert.SerializeObject(resultReqq)}\r\n{JsonConvert.SerializeObject(resultResp)}", $"{thisTp}_Result");
            return resultResp;
        }

        [Route("[controller]/bonusWin")]
        public PayoutResp BonusWin([FromForm]Dictionary<string, string> bonusWinReqq)
        {
            //log2File.Info(JsonConvert.SerializeObject(bonusWinReqq), $"{thisTp}_BonusWin");
            PayoutResp payoutResp = new PayoutResp();
            payoutResp.error = 100;
            payoutResp.description = "Internal server error.";

            try
            {
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var secureKey = thirdPartySetting.TpSetting["SecureKey"];

                if (hashCheck(bonusWinReqq, secureKey))
                {
                    //string Content = @"{""amount"":""9.0"",""gameId"":""vs7776aztec"",""hash"":""672028d15e2191c3050000f0f8a6b62a"",""ipAddress"":""218.211.34.185"",""language"":""zh"",""platform"":""WEB"",""providerId"":""PragmaticPlay"",""reference"":""5e9ed802005c8e0008a65f62"",""roundDetails"":""spin"",""roundId"":""2360710356"",""timestamp"":""1587468290666"",""userId"":""DNova88_157""}";
                    BetReq betReq = JsonConvert.DeserializeObject<BetReq>(JsonConvert.SerializeObject(bonusWinReqq));
                    payoutResp.transactionId = betReq.reference;
                    betReq.type = BettransType.Bonus;
                    if (bettingBLL.PayoutRNG_PP(betReq, out decimal tpAffterAmount) > 0)
                    {
                        if (betReq.amount == 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, betReq.userId);
                            payoutResp.cash = tpCustBalance.Balance;
                            payoutResp.currency = tpCustBalance.TpCurrencyId;
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, betReq.userId);
                            payoutResp.cash = tpAffterAmount;
                            payoutResp.currency = tpCustBalance.TpCurrencyId;
                        }
                        payoutResp.bonus = 0;
                        payoutResp.error = 0;
                        payoutResp.description = "Success.";
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, betReq.userId);
                        payoutResp.cash = tpCustBalance.Balance;
                        payoutResp.currency = tpCustBalance.TpCurrencyId;
                        payoutResp.bonus = 0;
                        payoutResp.error = 1;
                        payoutResp.description = "Insufficient balance.";
                    }
                    else
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, betReq.userId);
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
                log2File.Error($"{JsonConvert.SerializeObject(bonusWinReqq)}\r\n{JsonConvert.SerializeObject(payoutResp)}\r\n{ex}", $"{thisTp}_BonusWin");
            }

            log2File.Info($"{JsonConvert.SerializeObject(bonusWinReqq)}\r\n{JsonConvert.SerializeObject(payoutResp)}", $"{thisTp}_BonusWin");
            return payoutResp;
        }

        [Route("[controller]/JackpotWin")]
        public PayoutResp JackpotWin([FromForm]Dictionary<string, string> jackpotWinReqq)
        {
            //log2File.Info(JsonConvert.SerializeObject(jackpotWinReqq), $"{thisTp}_JackpotWin");
            PayoutResp payoutResp = new PayoutResp();
            payoutResp.error = 100;
            payoutResp.description = "Internal server error.";

            try
            {
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var secureKey = thirdPartySetting.TpSetting["SecureKey"];

                if (hashCheck(jackpotWinReqq, secureKey))
                {
                    BetReq betReq = JsonConvert.DeserializeObject<BetReq>(JsonConvert.SerializeObject(jackpotWinReqq));
                    payoutResp.transactionId = betReq.reference;
                    betReq.type = BettransType.Jackpot;
                    if (bettingBLL.PayoutRNG_PP(betReq, out decimal tpAffterAmount) > 0)
                    {
                        if (betReq.amount == 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, betReq.userId);
                            payoutResp.cash = tpCustBalance.Balance;
                            payoutResp.currency = tpCustBalance.TpCurrencyId;
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, betReq.userId);
                            payoutResp.cash = tpAffterAmount;
                            payoutResp.currency = tpCustBalance.TpCurrencyId;
                        }
                        payoutResp.bonus = 0;
                        payoutResp.error = 0;
                        payoutResp.description = "Success.";
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, betReq.userId);
                        payoutResp.cash = tpCustBalance.Balance;
                        payoutResp.currency = tpCustBalance.TpCurrencyId;
                        payoutResp.bonus = 0;
                        payoutResp.error = 1;
                        payoutResp.description = "Insufficient balance.";
                    }
                    else
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, betReq.userId);
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
                log2File.Error($"{JsonConvert.SerializeObject(jackpotWinReqq)}\r\n{JsonConvert.SerializeObject(payoutResp)}\r\n{ex}", $"{thisTp}_JackpotWin");
            }

            log2File.Info($"{JsonConvert.SerializeObject(jackpotWinReqq)}\r\n{JsonConvert.SerializeObject(payoutResp)}", $"{thisTp}_JackpotWin");
            return payoutResp;
        }

        public partial class PromoWinReq : PromoBonus
        {

        }

        [Route("[controller]/promoWin")]
        public PayoutResp PromoWin([FromForm]Dictionary<string, string> promoWinReqq)
        {
            //log2File.Info(JsonConvert.SerializeObject(promoWinReqq), $"{thisTp}_PromoWin");
            PayoutResp payoutResp = new PayoutResp();
            payoutResp.error = 100;
            payoutResp.description = "Internal server error.";

            try
            {
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var secureKey = thirdPartySetting.TpSetting["SecureKey"];

                if (hashCheck(promoWinReqq, secureKey))
                {
                    PromoWinReq promoWinReq = JsonConvert.DeserializeObject<PromoWinReq>(JsonConvert.SerializeObject(promoWinReqq));
                    payoutResp.transactionId = promoWinReq.Reference;

                    if (settledBLL.PromoBonus_PP(promoWinReq, out decimal tpAffterAmount) > 0)
                    {
                        if (promoWinReq.Amount == 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, promoWinReq.UserId);
                            payoutResp.cash = tpCustBalance.Balance;
                            payoutResp.currency = tpCustBalance.TpCurrencyId;
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, promoWinReq.UserId);
                            payoutResp.cash = tpAffterAmount;
                            payoutResp.currency = tpCustBalance.TpCurrencyId;
                        }
                        payoutResp.bonus = 0;
                        payoutResp.error = 0;
                        payoutResp.description = "Success.";
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, promoWinReq.UserId);
                        payoutResp.cash = tpCustBalance.Balance;
                        payoutResp.currency = tpCustBalance.TpCurrencyId;
                        payoutResp.bonus = 0;
                        payoutResp.error = 1;
                        payoutResp.description = "Insufficient balance.";
                    }
                    else
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, promoWinReq.UserId);
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
                log2File.Error($"{JsonConvert.SerializeObject(promoWinReqq)}\r\n{JsonConvert.SerializeObject(payoutResp)}\r\n{ex}", $"{thisTp}_PromoWin");
            }

            log2File.Info($"{JsonConvert.SerializeObject(promoWinReqq)}\r\n{JsonConvert.SerializeObject(payoutResp)}", $"{thisTp}_PromoWin");
            return payoutResp;
        }

        public class EndRoundResp
        {
            public decimal cash { get; set; }
            public decimal bonus { get; set; }
            public int error { get; set; }
            public string description { get; set; }
        }

        [Route("[controller]/endRound")]
        public EndRoundResp EndRound([FromForm]Dictionary<string, string> endRoundReqq)
        {
            //log2File.Info(JsonConvert.SerializeObject(endRoundReqq), $"{thisTp}_EndRound");
            EndRoundResp endRoundResp = new EndRoundResp();
            endRoundResp.error = 100;
            endRoundResp.description = "Internal server error.";

            try
            {
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var secureKey = thirdPartySetting.TpSetting["SecureKey"];

                if (hashCheck(endRoundReqq, secureKey))
                {
                    BetReq betReq = JsonConvert.DeserializeObject<BetReq>(JsonConvert.SerializeObject(endRoundReqq));
                    int result = settledBLL.SettledRNG_PP(betReq, out decimal tpAffterAmount);

                    endRoundResp.cash = 0;
                    endRoundResp.bonus = 0;
                    endRoundResp.error = 0;
                    endRoundResp.description = "Success.";
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
                log2File.Error($"{JsonConvert.SerializeObject(endRoundReqq)}\r\n{JsonConvert.SerializeObject(endRoundResp)}\r\n{ex}", $"{thisTp}_EndRound");
            }

            log2File.Info($"{JsonConvert.SerializeObject(endRoundReqq)}\r\n{JsonConvert.SerializeObject(endRoundResp)}", $"{thisTp}_EndRound");
            return endRoundResp;
        }

        public class RefundReq : BettingDataRNG_PP
        {
            public string providerId { get; set; }
        }

        public class RefundResp
        {
            public string transactionId { get; set; }
            public int error { get; set; }
            public string description { get; set; }
        }

        [Route("[controller]/refund")]
        public RefundResp refund([FromForm]Dictionary<string, string> refundReqq)
        {
            RefundResp refundResp = new RefundResp();
            refundResp.error = 100;
            refundResp.description = "Internal server error.";

            try
            {
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var secureKey = thirdPartySetting.TpSetting["SecureKey"];

                if (hashCheck(refundReqq, secureKey))
                {
                    //string Content = @"{""amount"":""9.0"",""gameId"":""vs7776aztec"",""hash"":""672028d15e2191c3050000f0f8a6b62a"",""ipAddress"":""218.211.34.185"",""language"":""zh"",""platform"":""WEB"",""providerId"":""PragmaticPlay"",""reference"":""5e9ed802005c8e0008a65f62"",""roundDetails"":""spin"",""roundId"":""2360710356"",""timestamp"":""1587468290666"",""userId"":""DNova88_157""}";
                    RefundReq refundReq = JsonConvert.DeserializeObject<RefundReq>(JsonConvert.SerializeObject(refundReqq));
                    refundResp.transactionId = $"{refundReq.reference}_refund";
                    //BetReq betReq = JsonConvert.DeserializeObject<BetReq>(Content);
                    if (settledBLL.CancelBetRNG_PP(refundReq, out decimal tpAffterAmount) > 0)
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
                log2File.Error($"{JsonConvert.SerializeObject(refundReqq)}\r\n{JsonConvert.SerializeObject(refundResp)}\r\n{ex}", $"{thisTp}_refund");
            }

            log2File.Info($"{JsonConvert.SerializeObject(refundReqq)}\r\n{JsonConvert.SerializeObject(refundResp)}", $"{thisTp}_refund");
            return refundResp;
        }
        private bool hashCheck(Dictionary<string, string> req, string secureKey)
        {
            var hashKey = req["hash"];
            var dicP = req.Where(x => x.Key != "hash").OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}{secureKey}";

            //log2File.Info($"{p}\r\n{hashKey}\r\n{tools.ToMD5(p)}", $"{thisTp}_hashCheck");

            return hashKey.Equals(tools.ToMD5(p), StringComparison.OrdinalIgnoreCase);
        }
    }
}