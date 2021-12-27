using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class JokerController : ControllerBase
    {
        private readonly AuthBLL authBLL;
        private readonly ILogger<JokerController> _logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly IHostEnvironment environment;
        private readonly Tools tools;

        public JokerController(IHostEnvironment environment, ILogger<JokerController> logger, AuthBLL authBLL, ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, BettingBLL bettingBLL, SettledBLL settledBLL, Log2File log2File, Tools tools)
        {
            this.authBLL = authBLL;
            this._logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            this.environment = environment;
            this.tools = tools;
        }
        private string GetMD5Signature(string rawData)
        {
            using MD5 md5Hash = MD5.Create();
            string md5Content = GetMd5Hash(md5Hash, rawData);
            return md5Content;
        }
        private string GetMd5Hash(MD5 md5Hash, string rawData)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
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

        [Route("[controller]/authenticate-token")]
        public AuthenticateTokenRep AuthenticateToken([FromForm] AuthenticateTokenReq authenticateTokenReq)
        {
            log2File.Info(JsonConvert.SerializeObject(authenticateTokenReq), "Joker_AuthenticateToken");
            AuthenticateTokenRep authenticateTokenRep = new AuthenticateTokenRep();
            authenticateTokenRep.Status = 1000;
            authenticateTokenRep.Message = "Other";
            var dicP = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(authenticateTokenReq)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.Joker);
            string secret = "";
            foreach (var setting in thirdPartySetting.TpSetting)
            {
                if (setting.Value == authenticateTokenReq.appid)
                {
                    secret = thirdPartySetting.TpSetting[$"Secret_{setting.Key.Substring(6)}"];
                    break;
                }
            }

            string P = $"{string.Join("&", dicP.Where(x => x.Key != "hash").Select(kp => $"{kp.Key}={kp.Value}").ToArray())}{secret}";
            if (GetMD5Signature(P) == authenticateTokenReq.hash)
            {
                var (token, gameCode) = GetToken(authenticateTokenReq.token);
                if (authBLL.Authenticate(token, out TpCustInfoAuth tpCustInfoAuth))
                {
                    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.Joker, tpCustInfoAuth.TpUserName);
                    if (tpCheck == null || tpCheck.Locked)
                    {
                        authenticateTokenRep.Status = 3;
                        authenticateTokenRep.Message = "Invalid token";
                    }
                    else
                    {
                        var gameType = (int)thirdPartyBLL.GetGameType((int)Tp.Joker, gameCode);
                        tpCustInfoBLL.SetCustGotoGame(tpCustInfoAuth.CustId, (int)Tp.Joker, gameType);
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.Joker, tpCustInfoAuth.TpUserName, gameType);
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
            }
            else
            {
                authenticateTokenRep.Status = 5;
                authenticateTokenRep.Message = "Invalid signature";
            }

            log2File.Info($"secret={secret}\r\n{JsonConvert.SerializeObject(authenticateTokenReq)}\r\n{JsonConvert.SerializeObject(authenticateTokenRep)}", "Joker_AuthenticateToken");
            return authenticateTokenRep;
        }

        public class BalanceReq
        {
            public string appid { get; set; }
            [Newtonsoft.Json.JsonIgnore]
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

        [Route("[controller]/balance")]
        public Rep Balance([FromForm] BalanceReq balanceReq)
        {
            Rep rep = new Rep();
            rep.Status = 1000;
            rep.Message = "Other";
            var dicP = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(balanceReq)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.Joker);
            string secret = "";
            foreach (var setting in thirdPartySetting.TpSetting)
            {
                if (setting.Value == balanceReq.appid)
                {
                    secret = thirdPartySetting.TpSetting[$"Secret_{setting.Key.Substring(6)}"];
                    break;
                }
            }

            string P = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}{secret}";
            if (GetMD5Signature(P) == balanceReq.hash)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Joker, balanceReq.username);
                var gameType = tpCustInfoBLL.GetCustGotoGameType(tpCustInfo.CustId, (int)Tp.Joker);
                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.Joker, balanceReq.username, gameType);
                rep.Status = 0;
                rep.Message = "Success";
                rep.Balance = tpCustBalance.Balance;
            }
            else
            {
                rep.Status = 5;
                rep.Message = "Invalid signature";
            }

            log2File.Info($"secret={secret}\r\n{JsonConvert.SerializeObject(balanceReq)}\r\n{JsonConvert.SerializeObject(rep)}", "Joker_Balance");
            return rep;
        }

        public class BetReq : BettingDataRNG_Joker
        {
            public string hash { get; set; }
        }

        [Route("[controller]/bet")]
        public Rep Bet([FromForm] Dictionary<string, string> betReqq)
        {
            var gameType = (int)GameType.Joker_RNG;
            if (environment.IsDevelopment())
            {
                //string BB = @"{""amount"":""1.20"",""appid"":""TFEB"",""gamecode"":""69xaiyrbo4dae"",""hash"":""7e3a05a92e9b1a4f8c5d655443440ac1"",""id"":""B-qc3ogwwgx3wzg"",""roundid"":""qc3ogwwgx3wzg"",""timestamp"":""1585853879309"",""username"":""DNOVA88_235""}";
                //log2File.Info(BB, "Joker_Bet");
                //betReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(BB);
            }
            var betReq = JsonConvert.DeserializeObject<BetReq>(JsonConvert.SerializeObject(betReqq));
            Rep rep = new Rep();
            rep.Status = 1000;
            rep.Message = "Other";
            betReqq.Remove("hash");
            var dicP = betReqq.OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.Joker);
            string secret = "";
            foreach (var setting in thirdPartySetting.TpSetting)
            {
                if (setting.Value == betReq.appid)
                {
                    secret = thirdPartySetting.TpSetting[$"Secret_{setting.Key.Substring(6)}"];
                    break;
                }
            }

            if (betReq.amount < 0)
            {
                var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, betReq.username, gameType);
                rep.Balance = tpCustBalance.Balance;
                rep.Status = 4;
                rep.Message = "InvalidParameters";
                return rep;
            }

            betReq.amount = tools.Floor(betReq.amount, 2);
            dicP["amount"] = betReq.amount.ToString("0.00");

            string P = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}{secret}";
            if (GetMD5Signature(P) == betReq.hash)
            {
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.Joker, betReq.username);
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
                        if (bettingBLL.PlaceBetRNG_Joker(betReq, out decimal tpAffterAmount) > 0)
                        {
                            rep.Status = 0;
                            rep.Message = "Success";
                            if (betReq.amount == 0)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, betReq.username, gameType);
                                rep.Balance = tpCustBalance.Balance;
                            }
                            else
                            {
                                rep.Balance = tpAffterAmount;
                            }
                        }
                        else if (tpAffterAmount < 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.Joker, betReq.username, gameType);
                            rep.Status = 100;
                            rep.Balance = tpCustBalance.Balance;
                            rep.Message = "Insufficient fund";
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, betReq.username, gameType);
                            rep.Balance = tpCustBalance.Balance;
                            rep.Status = 0;
                            rep.Message = "The Bet already existed";
                        }
                    }
                    catch (Exception ex)
                    {
                        rep.Status = 1000;
                        rep.Message = "Other";
                        log2File.Error($"{JsonConvert.SerializeObject(betReqq)}\r\n{JsonConvert.SerializeObject(rep)}\r\n{ex}", "Joker_Bet", ex);
                    }
                }
            }
            else
            {
                rep.Status = 5;
                rep.Message = "Invalid signature";
            }

            log2File.Info($"{JsonConvert.SerializeObject(betReqq)}\r\n{JsonConvert.SerializeObject(rep)}", "Joker_Bet");
            return rep;
        }

        public class SettleBetReq : BettingDataRNG_Joker
        {
            public string hash { get; set; }
        }

        [Route("[controller]/settle-bet")]
        public Rep SettleBet([FromForm] Dictionary<string, string> settleBetReqq)
        {
            var gameType = (int)GameType.Joker_RNG;
            if (environment.IsDevelopment())
            {
                /*var s = @"{""amount"":""0.00"",""appid"":""TFEE"",""description"":""G2a4153aeb436"",""gamecode"":""tocki7xk7xwq1"",""id"":""S_Ta8c164a170c5"",""roundid"":""8yhaa13k78ouc"",""timestamp"":""1586840626331"",""type"":""8yhaa13k78ouc"",""username"":""Nova88_14""}";
                settleBetReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);*/
            }

            var settleBetReq = JsonConvert.DeserializeObject<SettleBetReq>(JsonConvert.SerializeObject(settleBetReqq));
            Rep rep = new Rep();
            rep.Status = 1000;
            rep.Message = "Other";
            settleBetReqq.Remove("hash");
            var dicP = settleBetReqq.OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.Joker);
            string secret = "";
            foreach (var setting in thirdPartySetting.TpSetting)
            {
                if (setting.Value == settleBetReq.appid)
                {
                    secret = thirdPartySetting.TpSetting[$"Secret_{setting.Key.Substring(6)}"];
                    break;
                }
            }

            if (settleBetReq.amount < 0)
            {
                var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username, gameType);
                rep.Balance = tpCustBalance.Balance;
                rep.Status = 4;
                rep.Message = "InvalidParameters";
                return rep;
            }

            settleBetReq.amount = tools.Floor(settleBetReq.amount, 2);
            dicP["amount"] = settleBetReq.amount.ToString("0.00");

            string P = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}{secret}";
            if (GetMD5Signature(P) == settleBetReq.hash)
            {
                try
                {
                    settleBetReq.type = "settled";
                    int result = settledBLL.SettledRNG_Joker(settleBetReq, out decimal tpAffterAmount);

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
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username, gameType);
                            rep.Balance = tpCustBalance.Balance;
                            rep.Status = 0;
                            rep.Message = "Success";
                        }
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username, gameType);
                        rep.Balance = tpCustBalance.Balance;
                        rep.Status = 0;
                        rep.Message = "Transaction can not be found";
                    }
                    else if (result == -2)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username, gameType);
                        rep.Balance = tpCustBalance.Balance;
                        rep.Status = 0;
                        rep.Message = "Success";
                    }
                }
                catch (Exception ex)
                {
                    rep.Status = 1000;
                    rep.Message = "Other";
                    log2File.Error($"{JsonConvert.SerializeObject(settleBetReqq)}\r\n{JsonConvert.SerializeObject(rep)}\r\n{ex}", "Joker_SettleBet", ex);
                }
            }
            else
            {
                rep.Status = 5;
                rep.Message = "Invalid signature";
            }
            log2File.Info($"{JsonConvert.SerializeObject(settleBetReqq)}\r\n{JsonConvert.SerializeObject(rep)}", "Joker_SettleBet");
            return rep;
        }

        public class CancelBetReq : CancelBetDataRNG_Joker
        {
            public string hash { get; set; }
        }

        [Route("[controller]/cancel-bet")]
        public Rep CancelBet([FromForm] Dictionary<string, string> cancelBetReqq)
        {
            var gameType = (int)GameType.Joker_RNG;
            log2File.Info(JsonConvert.SerializeObject(cancelBetReqq), "Joker_CancelBet");
            var cancelBetReq = JsonConvert.DeserializeObject<CancelBetReq>(JsonConvert.SerializeObject(cancelBetReqq));
            Rep rep = new Rep();
            rep.Status = 1000;
            rep.Message = "Other";
            cancelBetReqq.Remove("hash");
            var dicP = cancelBetReqq.OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.Joker);
            string secret = "";
            foreach (var setting in thirdPartySetting.TpSetting)
            {
                if (setting.Value == cancelBetReq.appid)
                {
                    secret = thirdPartySetting.TpSetting[$"Secret_{setting.Key.Substring(6)}"];
                    break;
                }
            }

            string P = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}{secret}";
            if (GetMD5Signature(P) == cancelBetReq.hash)
            {
                int result = settledBLL.CancelBetRNG_Joker(cancelBetReq, out decimal tpAffterAmount);

                if (result > 0)
                {
                    rep.Status = 0;
                    rep.Message = "Success";
                    rep.Balance = tpAffterAmount;

                    if (tpAffterAmount == 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, cancelBetReq.username, gameType);
                        rep.Balance = tpCustBalance.Balance;
                    }
                }
                else if (result == -2)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, cancelBetReq.username, gameType);
                    rep.Balance = tpCustBalance.Balance;
                    rep.Status = 1000;
                    rep.Message = "Bet was settled";
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, cancelBetReq.username, gameType);
                    rep.Balance = tpCustBalance.Balance;
                    rep.Status = 0;
                    rep.Message = "Success";
                }
            }
            else
            {
                rep.Status = 5;
                rep.Message = "Invalid signature";
            }

            log2File.Info($"secret={secret}\r\n{JsonConvert.SerializeObject(cancelBetReqq)}\r\n{JsonConvert.SerializeObject(rep)}", "Joker_SettleBet");
            return rep;
        }

        [Route("[controller]/bonus-win")]
        public Rep Bonus([FromForm] Dictionary<string, string> bonusReqq)
        {
            var gameType = (int)GameType.Joker_RNG;
            log2File.Info(JsonConvert.SerializeObject(bonusReqq), "Joker_Bonus");
            var settleBetReq = JsonConvert.DeserializeObject<SettleBetReq>(JsonConvert.SerializeObject(bonusReqq));
            Rep rep = new Rep();
            rep.Status = 1000;
            rep.Message = "Other";
            bonusReqq.Remove("hash");
            var dicP = bonusReqq.OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.Joker);
            string secret = "";
            foreach (var setting in thirdPartySetting.TpSetting)
            {
                if (setting.Value == settleBetReq.appid)
                {
                    secret = thirdPartySetting.TpSetting[$"Secret_{setting.Key.Substring(6)}"];
                    break;
                }
            }

            if (settleBetReq.amount < 0)
            {
                var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username, gameType);
                rep.Balance = tpCustBalance.Balance;
                rep.Status = 4;
                rep.Message = "InvalidParameters";
                return rep;
            }

            settleBetReq.amount = tools.Floor(settleBetReq.amount, 2);
            dicP["amount"] = settleBetReq.amount.ToString("0.00");

            string P = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}{secret}";
            if (GetMD5Signature(P) == settleBetReq.hash)
            {
                settleBetReq.type = "bonus";
                int result = settledBLL.SettledRNG_Joker(settleBetReq, out decimal tpAffterAmount);

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
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username, gameType);
                        rep.Status = 0;
                        rep.Message = "Success";
                        rep.Balance = tpAffterAmount;
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username, gameType);
                    rep.Balance = tpCustBalance.Balance;
                    rep.Status = 0;
                    rep.Message = "Success";
                }
                else if (result == -2)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username, gameType);
                    rep.Balance = tpCustBalance.Balance;
                    rep.Status = 0;
                    rep.Message = "Success";
                }
            }
            else
            {
                rep.Status = 5;
                rep.Message = "Invalid signature";
            }

            log2File.Info(JsonConvert.SerializeObject(rep), "Joker_Bonus");
            return rep;
        }

        [Route("[controller]/jackpot-win")]
        public Rep Jackpot([FromForm] Dictionary<string, string> jackpotReqq)
        {
            var gameType = (int)GameType.Joker_RNG;
            log2File.Info(JsonConvert.SerializeObject(jackpotReqq), "Joker_Jackpot");
            var settleBetReq = JsonConvert.DeserializeObject<SettleBetReq>(JsonConvert.SerializeObject(jackpotReqq));
            Rep rep = new Rep();
            rep.Status = 1000;
            rep.Message = "Other";
            jackpotReqq.Remove("hash");
            var dicP = jackpotReqq.OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.Joker);
            string secret = "";
            foreach (var setting in thirdPartySetting.TpSetting)
            {
                if (setting.Value == settleBetReq.appid)
                {
                    secret = thirdPartySetting.TpSetting[$"Secret_{setting.Key.Substring(6)}"];
                    break;
                }
            }

            if (settleBetReq.amount < 0)
            {
                var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username, gameType);
                rep.Balance = tpCustBalance.Balance;
                rep.Status = 4;
                rep.Message = "InvalidParameters";
                return rep;
            }

            settleBetReq.amount = tools.Floor(settleBetReq.amount, 2);
            dicP["amount"] = settleBetReq.amount.ToString("0.00");

            string P = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}{secret}";
            if (GetMD5Signature(P) == settleBetReq.hash)
            {
                settleBetReq.type = "jackpot";
                int result = settledBLL.SettledRNG_Joker(settleBetReq, out decimal tpAffterAmount);

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
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username, gameType);
                        rep.Status = 0;
                        rep.Message = "Success";
                        rep.Balance = tpAffterAmount;
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username, gameType);
                    rep.Balance = tpCustBalance.Balance;
                    rep.Status = 0;
                    rep.Message = "Success";
                }
                else if (result == -2)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username, gameType);
                    rep.Balance = tpCustBalance.Balance;
                    rep.Status = 0;
                    rep.Message = "Success";
                }
            }
            else
            {
                rep.Status = 5;
                rep.Message = "Invalid signature";
            }

            log2File.Info(JsonConvert.SerializeObject(rep), "Joker_Jackpot");
            return rep;
        }

        [Route("[controller]/transaction")]
        public Rep Transaction([FromForm] Dictionary<string, string> parameter)
        {
            return FishBet(JokerFishTransType.Transaction, parameter);
        }

        [Route("[controller]/withdraw")]
        public Rep Withdraw([FromForm] Dictionary<string, string> parameter)
        {
            return FishBet(JokerFishTransType.Withdraw, parameter);
        }

        [Route("[controller]/deposit")]
        public Rep Deposit([FromForm] Dictionary<string, string> parameter)
        {
            return FishBet(JokerFishTransType.Deposit, parameter);
        }

        private Rep FishBet(JokerFishTransType transType, Dictionary<string, string> parameter)
        {
            try
            {
                log2File.Info(JsonConvert.SerializeObject(parameter), $"Joker_{transType}");
                var request = JsonConvert.DeserializeObject<BettingDataFish_Joker>(JsonConvert.SerializeObject(parameter));

                if (request.amount < 0)
                {
                    return new Rep
                    {
                        Balance = tpCustInfoBLL.GetBalance((int)Tp.Joker, request.username, (int)GameType.Joker_Fish).Balance,
                        Status = 4,
                        Message = "InvalidParameters"
                    };
                }

                request.amount = tools.Floor(request.amount, 2);
                parameter["amount"] = request.amount.ToString("0.00");

                if (!CheckSign(request.appid, request.hash, parameter))
                {
                    return new Rep
                    {
                        Status = 5,
                        Message = "Invalid signature"
                    };
                }

                if (transType == JokerFishTransType.Withdraw)
                {
                    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.Joker, request.username);
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
                    JokerFishTransType.Withdraw => bettingBLL.WithdrawFish_Joker(request, out tpAffterAmount),
                    JokerFishTransType.Deposit => settledBLL.DepositFish_Joker(request, out tpAffterAmount),
                    JokerFishTransType.Transaction => settledBLL.TransactionFish_Joker(request, out tpAffterAmount),
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
                        Balance = tpCustInfoBLL.GetBalance((int)Tp.Joker, request.username, (int)GameType.Joker_Fish).Balance,
                        Status = 100,
                        Message = "InsufficientFund"
                    };
                }
                // Duplicated
                if (result == -1)
                {
                    return new Rep
                    {
                        Balance = tpCustInfoBLL.GetBalance((int)Tp.Joker, request.username, (int)GameType.Joker_Fish).Balance,
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

        private bool CheckSign(string appid, string hash, Dictionary<string, string> data)
        {
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.Joker);
            var secret = "";
            foreach (var setting in thirdPartySetting.TpSetting)
            {
                if (setting.Value == appid)
                {
                    secret = thirdPartySetting.TpSetting[$"Secret_{setting.Key.Substring(6)}"];
                    break;
                }
            }

            data.Remove("hash");
            var rawData = $"{string.Join("&", data.OrderBy(kp => kp.Key).Select(kp => $"{kp.Key}={kp.Value}"))}{secret}";

            return GetMD5Signature(rawData) == hash;
        }

        private (string Token, string gameCode) GetToken(string token)
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
    }
}