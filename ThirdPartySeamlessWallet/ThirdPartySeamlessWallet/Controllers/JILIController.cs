using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class JILIController : ControllerBase
    {
        private readonly AuthBLL authBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly ILogger<JILIController> logger;
        private readonly Log2File log2File;
        private readonly Tools tools;
        private readonly Tp thisTp;
        private readonly IHostEnvironment environment;

        public JILIController(
            AuthBLL authBLL,
            ThirdPartyBLL thirdPartyBLL,
            TpCustInfoBLL tpCustInfoBLL,
            BettingBLL bettingBLL,
            SettledBLL settledBLL,
            ILogger<JILIController> logger,
            Log2File log2File,
            Tools tools,
            IHostEnvironment environment
        )
        {
            this.authBLL = authBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            this.tools = tools;
            this.thisTp = Tp.JILI;
            this.environment = environment;
        }

        public class TokenInfo
        {
            public string shortTimeToken { get; set; }
            public string longTimeToken { get; set; }
            public string tpUserName { get; set; }
            //public string tpCurrency { get; set; }
            public string gameType { get; set; }
        }

        #region Request

        public class AuthReq
        {
            public string reqId { get; set; }
            public string token { get; set; }
        }

        #endregion

        #region Response

        public class ErrorResp
        {
            public int errorCode { get; set; }
            public string message { get; set; }
        }

        public class AuthResp : ErrorResp
        {
            public string username { get; set; }
            public string currency { get; set; }
            public decimal balance { get; set; }
            public string token { get; set; }
        }

        public class BetResp : ErrorResp
        {
            public string username { get; set; }
            public string currency { get; set; }
            public decimal balance { get; set; }
            public long txId { get; set; }
            public string token { get; set; }
        }

        public class CancelBetResp : ErrorResp
        {
            public string username { get; set; }
            public string currency { get; set; }
            public int balance { get; set; }
            public int txId { get; set; }
        }

        #endregion

        [HttpPost]
        [Route("[controller]/auth")]
        public async Task<dynamic> authAsync()
        {
            string logName = $"{thisTp}_auth";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info(Content, logName);

            AuthReq getReq = JsonConvert.DeserializeObject<AuthReq>(Content);
            ErrorResp getErrorResponse = new ErrorResp();
            getErrorResponse.errorCode = 5;
            getErrorResponse.message = "Error";

            // check authorization
            Request.Headers.TryGetValue("Authorization", out var authorization);
            string sAuthorization = authorization.ToString();

            var check = checkAuthorization(sAuthorization);
            if (check.errorCode != 0)
            {
                log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(check)}", logName);
                return check;
            }

            try
            {
                var tokenInfo = getTokenInfo(getReq.token);
                var verify = authBLL.Authenticate(tokenInfo.shortTimeToken, out TpCustInfoAuth tpCustInfoAuth);
                if (!verify)
                {
                    getErrorResponse.errorCode = 4;
                    getErrorResponse.message = "Token expired";
                    log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tokenInfo.tpUserName);
                if (tpCheck == null)
                {
                    getErrorResponse.errorCode = 4;
                    getErrorResponse.message = "Token expired.";
                    log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }
                else if (tpCheck.Locked)
                {
                    getErrorResponse.errorCode = 5;
                    getErrorResponse.message = "Account is Locked";
                    log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }

                var gameType = thirdPartyBLL.GetGameType((int)thisTp, tokenInfo.gameType);

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpCustInfoAuth.TpUserName, (int)gameType);
                if (tpCustBalance != null)
                {
                    AuthResp getApiResponse = new AuthResp();
                    getApiResponse.errorCode = 0;
                    getApiResponse.message = "success";
                    getApiResponse.username = tokenInfo.tpUserName;
                    getApiResponse.currency = tpCustBalance.TpCurrencyId;
                    getApiResponse.balance = tpCustBalance.Balance;
                    getApiResponse.token = getReq.token;
                    log2File.Info($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getErrorResponse)}\r\n{ex}", logName);
                return getErrorResponse;
            }

            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
            return getErrorResponse;


        }

        [HttpPost]
        [Route("[controller]/bet")]
        public async Task<dynamic> betAsync()
        {
            string logName = $"{thisTp}_bet";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info(Content, logName);

            BettingData_JILI getReq = JsonConvert.DeserializeObject<BettingData_JILI>(Content);
            ErrorResp getErrorResponse = new ErrorResp();
            getErrorResponse.errorCode = 5;
            getErrorResponse.message = "Error";

            // check authorization
            Request.Headers.TryGetValue("Authorization", out var authorization);
            string sAuthorization = authorization.ToString();

            var check = checkAuthorization(sAuthorization);
            if (check.errorCode != 0)
            {
                log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(check)}", logName);
                return check;
            }
            try
            {
                var tokenInfo = getTokenInfo(getReq.token);

                // offline payment no need to check online
                if (!getReq.isFreeRound)
                {
                    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getReq.userId);
                    if (tpCheck == null)
                    {
                        getErrorResponse.errorCode = 4;
                        getErrorResponse.message = "Token expired.";
                        log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                        return getErrorResponse;
                    }
                    else if (tpCheck.Token != tokenInfo.longTimeToken)
                    {
                        getErrorResponse.errorCode = 4;
                        getErrorResponse.message = "Wrong token";
                        log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                        return getErrorResponse;
                    }
                    else if (tpCheck.Locked)
                    {
                        getErrorResponse.errorCode = 5;
                        getErrorResponse.message = "Account is Locked";
                        log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                        return getErrorResponse;
                    }
                }

                var gameType = thirdPartyBLL.GetGameType((int)thisTp, getReq.game.ToString());
                //var result = 0;
                //decimal tpAffterAmount = 0;
                //if (gameType == GameType.JILI_RNG)
                //{
                //    // slot
                //    result = settledBLL.BetAndPayoutRNG_JILI(getReq, out tpAffterAmount);
                //}
                //else
                //{
                //    // fish
                //    result = settledBLL.BetAndPayoutFish_JILI(getReq, out tpAffterAmount);
                //}

                //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tokenInfo.tpUserName, (int)gameType);
                //BetResp getApiResponse = new BetResp();
                //getApiResponse.errorCode = 0;
                //getApiResponse.message = "success";
                //getApiResponse.username = tokenInfo.tpUserName;
                //getApiResponse.currency = tokenInfo.tpCurrency;
                //getApiResponse.balance = tpCustBalance.Balance;
                //getApiResponse.txId = DateTimeOffset.Now.ToUnixTimeSeconds();
                //getApiResponse.token = getReq.token;
                //log2File.Info($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                //return getApiResponse;

                var result = settledBLL.BetAndPayoutRNG_JILI(getReq, (int)gameType, out decimal tpAffterAmount);

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getReq.userId, (int)gameType);
                //var result = (gameType == GameType.JILI_RNG) ? bettingBLL.PlaceBetRNG_JILI(getReq, out long txId, out decimal tpAffterAmount) : bettingBLL.PlaceBetFish_JILI(getReq, out txId, out tpAffterAmount);
                if (result > 0)
                {
                    BetResp getApiResponse = new BetResp();
                    getApiResponse.errorCode = 0;
                    getApiResponse.message = "success";
                    getApiResponse.username = getReq.userId;
                    getApiResponse.currency = tpCustBalance.TpCurrencyId;
                    getApiResponse.balance = tpAffterAmount;
                    //getApiResponse.txId = txId;
                    getApiResponse.token = getReq.token;
                    log2File.Info($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (result == -1)
                {
                    BetResp getApiResponse = new BetResp();
                    getApiResponse.errorCode = 1;
                    getApiResponse.message = "Already accepted";
                    getApiResponse.username = getReq.userId;
                    getApiResponse.currency = tpCustBalance.TpCurrencyId;
                    getApiResponse.balance = tpCustBalance.Balance;
                    //getApiResponse.txId = txId;
                    getApiResponse.token = getReq.token;
                    log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (tpAffterAmount < 0)
                {
                    BetResp getApiResponse = new BetResp();
                    getApiResponse.errorCode = 2;
                    getApiResponse.message = "Not enough balance";
                    getApiResponse.username = getReq.userId;
                    getApiResponse.currency = tpCustBalance.TpCurrencyId;
                    getApiResponse.balance = tpCustBalance.Balance;
                    //getApiResponse.txId = txId;
                    getApiResponse.token = getReq.token;
                    log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getErrorResponse)}\r\n{ex}", logName);
                return getErrorResponse;
            }

            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
            return getErrorResponse;
        }

        [HttpPost]
        [Route("[controller]/cancelBet")]
        public async Task<dynamic> cancelBetAsync()
        {
            string logName = $"{thisTp}_cancelBet";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info(Content, logName);

            CancelBetData_JILI getReq = JsonConvert.DeserializeObject<CancelBetData_JILI>(Content);
            ErrorResp getErrorResponse = new ErrorResp();
            getErrorResponse.errorCode = 5;
            getErrorResponse.message = "Error";

            // check authorization
            Request.Headers.TryGetValue("Authorization", out var authorization);
            string sAuthorization = authorization.ToString();

            var check = checkAuthorization(sAuthorization);
            if (check.errorCode != 0)
            {
                log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(check)}", logName);
                return check;
            }

            var gameType = thirdPartyBLL.GetGameType((int)thisTp, getReq.game.ToString());

            try
            {
                var result = settledBLL.CancelBetRNG_JILI(getReq, (int)gameType, out decimal tpAffterAmount);

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getReq.userId, (int)gameType);
                if (result > 0)
                {
                    BetResp getApiResponse = new BetResp();
                    getApiResponse.errorCode = 0;
                    getApiResponse.message = "success";
                    getApiResponse.username = getReq.userId;
                    getApiResponse.currency = tpCustBalance.TpCurrencyId;
                    getApiResponse.balance = tpAffterAmount;
                    //getApiResponse.txId = txId;
                    log2File.Info($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (result == -1)
                {

                    BetResp getApiResponse = new BetResp();
                    getApiResponse.errorCode = 1;
                    getApiResponse.message = "Already accepted";
                    getApiResponse.username = getReq.userId;
                    getApiResponse.currency = tpCustBalance.TpCurrencyId;
                    getApiResponse.balance = tpCustBalance.Balance;
                    //getApiResponse.txId = txId;
                    log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (result == -2)
                {
                    getErrorResponse.errorCode = 2;
                    getErrorResponse.message = "Round not found";
                    log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(Content)}\r\n{JsonConvert.SerializeObject(getErrorResponse)}\r\n{ex}", logName);
                return getErrorResponse;
            }

            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
            return getErrorResponse;
        }

        private ErrorResp checkAuthorization(string authorization = null)
        {
            ErrorResp getErrorResponse = new ErrorResp();

            if (string.IsNullOrEmpty(authorization))
            {
                getErrorResponse.errorCode = 5;
                getErrorResponse.message = "authorization is null";
                return getErrorResponse;
            }

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            byte[] authTmp = Encoding.ASCII.GetBytes($"{thirdPartySetting.TpSetting["Username"]}:{thirdPartySetting.TpSetting["Password"]}");
            string authToken = Convert.ToBase64String(authTmp);
            string[] aAuthorization = authorization.Split(' ');
            if (aAuthorization.Count() != 2 && aAuthorization[0] != "Basic")
            {
                getErrorResponse.errorCode = 5;
                getErrorResponse.message = $"authorization parameter error ({authorization})";
                return getErrorResponse;
            }
            if (authToken != aAuthorization[1])
            {
                getErrorResponse.errorCode = 5;
                getErrorResponse.message = $"authorization check error ({authorization})";
                return getErrorResponse;
            }

            getErrorResponse.errorCode = 0;
            getErrorResponse.message = "authorization check success";
            return getErrorResponse;
        }

        private TokenInfo getTokenInfo(string token)
        {
            string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(token));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            return new TokenInfo
            {
                shortTimeToken = sessionKeyArr[0], /* 1 min*/
                longTimeToken = sessionKeyArr[1],  /* Long Time*/
                tpUserName = sessionKeyArr[2],
                //tpCurrency = sessionKeyArr[3],
                gameType = sessionKeyArr[3]
            };
        }
    }
}
