using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
        private readonly ILogger<JokerController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly IHostEnvironment environment;

        public JokerController(IHostEnvironment environment, ILogger<JokerController> logger, AuthBLL authBLL, ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, BettingBLL bettingBLL, SettledBLL settledBLL, Log2File log2File)
        {
            this.authBLL = authBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            this.environment = environment;
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
        public AuthenticateTokenRep AuthenticateToken([FromForm]AuthenticateTokenReq authenticateTokenReq)
        {
            log2File.Info(Newtonsoft.Json.JsonConvert.SerializeObject(authenticateTokenReq), "Joker_AuthenticateToken");
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
                if (authBLL.Authenticate(authenticateTokenReq.token, out TpCustInfoAuth tpCustInfoAuth))
                {
                    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.Joker, tpCustInfoAuth.TpUserName);
                    if (tpCheck == null)
                    {
                        authenticateTokenRep.Status = 3;
                        authenticateTokenRep.Message = "Invalid token";
                    }
                    else if (tpCheck.Locked)
                    {
                        authenticateTokenRep.Status = 3;
                        authenticateTokenRep.Message = "Invalid token";
                    }
                    else
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.Joker, tpCustInfoAuth.TpUserName);
                        authenticateTokenRep.Status = 0;
                        authenticateTokenRep.Message = "Success";
                        authenticateTokenRep.Balance = tpCustBalance.Balance;
                        authenticateTokenRep.Username = tpCustInfoAuth.TpUserName;
                    }
                }
            }
            else
            {
                authenticateTokenRep.Status = 5;
                authenticateTokenRep.Message = "Invalid signature";
            }

            log2File.Info(Newtonsoft.Json.JsonConvert.SerializeObject(authenticateTokenRep), "Joker_AuthenticateToken");
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
        public Rep Balance([FromForm]BalanceReq balanceReq)
        {
            log2File.Info(Newtonsoft.Json.JsonConvert.SerializeObject(balanceReq), "Joker_Balance");
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
                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.Joker, balanceReq.username);
                rep.Status = 0;
                rep.Message = "Success";
                rep.Balance = tpCustBalance.Balance;
            }

            log2File.Info(Newtonsoft.Json.JsonConvert.SerializeObject(rep), "Joker_Balance");
            return rep;
        }

        public class BetReq : BettingDataRNG_Joker
        {
            public string hash { get; set; }
        }

        [Route("[controller]/bet")]
        public Rep Bet([FromForm]Dictionary<string, string> betReqq)
        {
            if (environment.IsDevelopment())
            {
                //string BB = @"{""amount"":""1.20"",""appid"":""TFEB"",""gamecode"":""69xaiyrbo4dae"",""hash"":""7e3a05a92e9b1a4f8c5d655443440ac1"",""id"":""B-qc3ogwwgx3wzg"",""roundid"":""qc3ogwwgx3wzg"",""timestamp"":""1585853879309"",""username"":""DNOVA88_235""}";
                //log2File.Info(BB, "Joker_Bet");
                //betReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(BB);
            }
            log2File.Info(JsonConvert.SerializeObject(betReqq), "Joker_Bet");
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
                    if (bettingBLL.PlaceBetRNG_Joker(betReq, out decimal tpAffterAmount) > 0)
                    {
                        rep.Status = 0;
                        rep.Message = "Success";
                        rep.Balance = tpAffterAmount;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.Joker, betReq.username);
                        rep.Status = 100;
                        rep.Balance = tpCustBalance.Balance;
                        rep.Message = "Insufficient fund";
                    }
                    else
                    {
                        rep.Status = 1000;
                        rep.Message = "Other";
                    }
                }
            }

            log2File.Info(Newtonsoft.Json.JsonConvert.SerializeObject(rep), "Joker_Bet");
            return rep;
        }

        public class SettleBetReq : BettingDataRNG_Joker
        {
            public string hash { get; set; }
        }

        [Route("[controller]/settle-bet")]
        public Rep SettleBet([FromForm]Dictionary<string, string> settleBetReqq)
        {
            if (environment.IsDevelopment())
            {
                //var s = @"{""amount"":""2.50"",""appid"":""TFEB"",""description"":""69xaiyrbo4dae"",""gamecode"":""69xaiyrbo4dae"",""hash"":""97bb93dc5720365602a9eff110fa29cc"",""id"":""S-qc3hmim8ema8g"",""roundid"":""qc3hmim8ema8g"",""timestamp"":""1585908888716"",""type"":""69xaiyrbo4dae"",""username"":""DNOVA88_235""}";
                //settleBetReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }

            log2File.Info(JsonConvert.SerializeObject(settleBetReqq), "Joker_SettleBet");
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

            string P = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}{secret}";
            if (GetMD5Signature(P) == settleBetReq.hash)
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
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username);
                        rep.Balance = tpCustBalance.Balance;
                        rep.Status = 0;
                        rep.Message = "Success";
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username);
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

            log2File.Info(Newtonsoft.Json.JsonConvert.SerializeObject(rep), "Joker_SettleBet");
            return rep;
        }

        public class CancelBetReq : CancelBetDataRNG_Joker
        {
            public string hash { get; set; }
        }

        [Route("[controller]/cancel-bet")]
        public Rep CancelBet([FromForm]Dictionary<string, string> cancelBetReqq)
        {
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
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, cancelBetReq.username);
                        rep.Balance = tpCustBalance.Balance;
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, cancelBetReq.username);
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

            log2File.Info(Newtonsoft.Json.JsonConvert.SerializeObject(rep), "Joker_CancelBet");
            return rep;
        }

        [Route("[controller]/bonus-win")]
        public Rep Bonus([FromForm]Dictionary<string, string> bonusReqq)
        {
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
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username);
                        rep.Status = 0;
                        rep.Message = "Success";
                        rep.Balance = tpAffterAmount;
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username);
                    rep.Status = 0;
                    rep.Message = "Success";
                }
            }
            else
            {
                rep.Status = 5;
                rep.Message = "Invalid signature";
            }

            log2File.Info(Newtonsoft.Json.JsonConvert.SerializeObject(rep), "Joker_Bonus");
            return rep;
        }

        [Route("[controller]/jackpot-win")]
        public Rep Jackpot([FromForm]Dictionary<string, string> jackpotReqq)
        {
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
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username);
                        rep.Status = 0;
                        rep.Message = "Success";
                        rep.Balance = tpAffterAmount;
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.Joker, settleBetReq.username);
                    rep.Status = 0;
                    rep.Message = "Success";
                }
            }
            else
            {
                rep.Status = 5;
                rep.Message = "Invalid signature";
            }

            log2File.Info(Newtonsoft.Json.JsonConvert.SerializeObject(rep), "Joker_Jackpot");
            return rep;
        }
    }
}