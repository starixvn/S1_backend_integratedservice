using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ILogger<GPIController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;

        public JokerController(ILogger<GPIController> logger, AuthBLL authBLL, ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, BettingBLL bettingBLL, SettledBLL settledBLL, Log2File log2File)
        {
            this.authBLL = authBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
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
            Rep balanceRep = new Rep();
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

            string P = $"{string.Join("&", dicP.Where(x => x.Key != "hash").Select(kp => $"{kp.Key}={kp.Value}").ToArray())}{secret}";
            if (GetMD5Signature(P) == balanceReq.hash)
            {
                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.Joker, balanceReq.username);
                balanceRep.Status = 0;
                balanceRep.Message = "Success";
                balanceRep.Balance = tpCustBalance.Balance;
            }

            log2File.Info(Newtonsoft.Json.JsonConvert.SerializeObject(balanceRep), "Joker_Balance");
            return balanceRep;
        }

        public class BetReq
        {
            public decimal amount { get; set; }
            public string appid { get; set; }
            public string gamecode { get; set; }
            public string hash { get; set; }
            public string id { get; set; }
            public long timestamp { get; set; }
            public string username { get; set; }
        }

        //[Route("[controller]/bet")]
        //public Rep Bet([FromForm]BetReq betReq)
        //{
        //    log2File.Info(Newtonsoft.Json.JsonConvert.SerializeObject(betReq), "Joker_Bet");
        //    Rep balanceRep = new Rep();
        //    var dicP = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(betReq)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
        //    var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.Joker);
        //    string secret = "";
        //    foreach (var setting in thirdPartySetting.TpSetting)
        //    {
        //        if (setting.Value == balanceReq.appid)
        //        {
        //            secret = thirdPartySetting.TpSetting[$"Secret_{setting.Key.Substring(6)}"];
        //            break;
        //        }
        //    }

        //    string P = $"{string.Join("&", dicP.Where(x => x.Key != "hash").Select(kp => $"{kp.Key}={kp.Value}").ToArray())}{secret}";
        //    if (GetMD5Signature(P) == balanceReq.hash)
        //    {
        //        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.Joker, balanceReq.username);
        //        balanceRep.Status = 0;
        //        balanceRep.Message = "Success";
        //        balanceRep.Balance = tpCustBalance.Balance;
        //    }

        //    log2File.Info(Newtonsoft.Json.JsonConvert.SerializeObject(balanceRep), "Joker_Balance");
        //    return balanceRep;
        //}
    }
}