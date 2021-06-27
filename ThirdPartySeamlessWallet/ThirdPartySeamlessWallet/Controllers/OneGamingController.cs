using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
    public class OneGamingController : ControllerBase
    {
        private readonly AuthBLL authBLL;
        private readonly ILogger<OneGamingController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly Tools tools;
        private readonly Tp thisTp;
        private readonly IHostEnvironment environment;

        public OneGamingController(
            IHostEnvironment environment,
            ILogger<OneGamingController> logger,
            AuthBLL authBLL,
            ThirdPartyBLL thirdPartyBLL,
            TpCustInfoBLL tpCustInfoBLL,
            BettingBLL bettingBLL,
            SettledBLL settledBLL,
            Log2File log2File,
            Tools tools
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
            this.thisTp = Tp.OneGaming;
            this.environment = environment;
        }

        #region Request

        public class VerifyReq
        {
            public string l_token { get; set; }
        }

        public class BalanceReq
        {
            public string l_token { get; set; }
            public string bcode { get; set; }
        }

        public class MobileReq
        {
            public string agent_code { get; set; }
            public string username { get; set; }
            public string password { get; set; }
            public string bcode { get; set; }
        }

        #endregion

        #region Response

        public class ApiResp
        {
            public string status_code { get; set; }
            public string message { get; set; }
            public string error_code { get; set; }
            public string datetime { get; set; }
            public string agent_code { get; set; }
        }

        public class VerifyResp : ApiResp
        {
            public string user_name { get; set; }
        }

        public class BalanceResp : ApiResp
        {
            public decimal balance { get; set; }
            public string player_id { get; set; }
        }

        public class TransferResp : ApiResp
        {
            public string transaction_id { get; set; }
            public string player_id { get; set; }
            public decimal balance { get; set; }
        }

        public class MobileResp : ApiResp
        {
            public string l_token { get; set; }
            public string player_id { get; set; }
            public decimal balance { get; set; }
        }

        public class TokenInfo
        {
            public string shortTimeToken { get; set; }
            public string longTimeToken { get; set; }
            public string tpUserName { get; set; }
            public string tpCurrency { get; set; }
        }
        #endregion

        [HttpGet]
        [Route("[controller]/igPokerValidation")]
        public dynamic igPokerValidation([FromQuery] Dictionary<string, string> getReq)
        {
            string logName = $"{thisTp}_igPokerValidation";
            var json = JsonConvert.SerializeObject(getReq);
            var verifyReq = JsonConvert.DeserializeObject<VerifyReq>(json);

            VerifyResp getApiResponse = new VerifyResp();
            getApiResponse.status_code = "1";
            getApiResponse.message = "Error";
            getApiResponse.datetime = getCurrentGameTime();

            if (string.IsNullOrEmpty(verifyReq.l_token))
            {
                getApiResponse.status_code = "1";
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tokenInfo = getTokenInfo(verifyReq.l_token);

            if (authBLL.Authenticate(tokenInfo.shortTimeToken, out TpCustInfoAuth tpCustInfoAuth))
            {
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
                if (tpCheck == null)
                {
                    getApiResponse.status_code = "1";
                    getApiResponse.message = "Invalid user Id";
                    log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (tpCheck.Token != tokenInfo.longTimeToken)
                {
                    getApiResponse.status_code = "1";
                    getApiResponse.error_code = "320";
                    getApiResponse.message = $"Validation not verify";
                    log2File.Error($"{JsonConvert.SerializeObject(tokenInfo)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (tpCheck.Locked)
                {
                    getApiResponse.status_code = "1";
                    getApiResponse.message = "Account is Locked";
                    log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);

                getApiResponse.status_code = "0";
                getApiResponse.message = "";
                getApiResponse.user_name = tpCustInfoAuth.TpUserName;
                getApiResponse.agent_code = thirdPartySetting.TpSetting[$"agent_code_{tpCustInfoAuth.TpCurrencyId}"];
            }

            log2File.Info($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpGet]
        [Route("[controller]/IG_getBalance")]
        public dynamic IG_getBalance([FromQuery] Dictionary<string, string> getReq)
        {
            string logName = $"{thisTp}_IG_getBalance";
            var json = JsonConvert.SerializeObject(getReq);
            var balanceReq = JsonConvert.DeserializeObject<BalanceReq>(json);

            BalanceResp getApiResponse = new BalanceResp();
            getApiResponse.status_code = "1";
            getApiResponse.message = "Error";
            getApiResponse.datetime = getCurrentGameTime();

            if (string.IsNullOrEmpty(balanceReq.l_token))
            {
                getApiResponse.status_code = "1";
                getApiResponse.message = "Token is null";
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tokenInfo = getTokenInfo(balanceReq.l_token);

            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tokenInfo.tpUserName);
            if (tpCheck == null)
            {
                getApiResponse.status_code = "1";
                getApiResponse.message = "Invalid user Id";
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            else if (tpCheck.Token != tokenInfo.longTimeToken)
            {
                getApiResponse.status_code = "1";
                getApiResponse.error_code = "320";
                getApiResponse.message = "Validation not verify";
                log2File.Error($"{JsonConvert.SerializeObject(tokenInfo)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            else if (tpCheck.Locked)
            {
                getApiResponse.status_code = "1";
                getApiResponse.message = "Account is Locked";
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);

            var verifyBcode = HMACSHA256($"{thirdPartySetting.TpSetting[$"agent_code_{tokenInfo.tpCurrency}"] + tokenInfo.tpUserName}", thirdPartySetting.TpSetting[$"secret_key_{tokenInfo.tpCurrency}"]);
            if (balanceReq.bcode != verifyBcode)
            {
                getApiResponse.status_code = "1";
                getApiResponse.error_code = "315";
                getApiResponse.message = "Invalid bcode";
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tokenInfo.tpUserName);
            if (tpCustBalance != null)
            {
                getApiResponse.status_code = "0";
                getApiResponse.message = "";
                getApiResponse.balance = tpCustBalance.Balance;
                getApiResponse.player_id = tokenInfo.tpUserName;
                getApiResponse.agent_code = thirdPartySetting.TpSetting[$"agent_code_{tokenInfo.tpCurrency}"];
                log2File.Info($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            log2File.Info($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpGet]
        [Route("[controller]/IG_playerMoneyOut")]
        public dynamic IG_playerMoneyOut([FromQuery] Dictionary<string, string> getReq)
        {
            string logName = $"{thisTp}_IG_playerMoneyOut";
            var json = JsonConvert.SerializeObject(getReq);
            var transferReq = JsonConvert.DeserializeObject<BettingDataPK_OneGaming>(json);

            TransferResp getApiResponse = new TransferResp();
            getApiResponse.status_code = "1";
            getApiResponse.message = "Error";
            getApiResponse.datetime = getCurrentGameTime();

            //if (environment.IsDevelopment())
            //{
            //    System.Threading.Thread.Sleep(15000);
            //    log2File.Info($"SLEEP:\r\n{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            //    //return getApiResponse;
            //}

            if (string.IsNullOrEmpty(transferReq.l_token))
            {
                getApiResponse.status_code = "1";
                getApiResponse.message = "Token is null";
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tokenInfo = getTokenInfo(transferReq.l_token);

            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tokenInfo.tpUserName);
            if (tpCheck == null)
            {
                getApiResponse.status_code = "1";
                getApiResponse.message = "Invalid user Id";
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            else if (tpCheck.Token != tokenInfo.longTimeToken)
            {
                getApiResponse.status_code = "1";
                getApiResponse.error_code = "320";
                getApiResponse.message = $"Validation not verify";
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            else if (tpCheck.Locked)
            {
                getApiResponse.status_code = "1";
                getApiResponse.message = "Account is Locked";
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            var verifyBcode = HMACSHA256($"{thirdPartySetting.TpSetting[$"agent_code_{tokenInfo.tpCurrency}"]}{tokenInfo.tpUserName}{transferReq.amount}", thirdPartySetting.TpSetting[$"secret_key_{tokenInfo.tpCurrency}"]);
            if (transferReq.bcode != verifyBcode)
            {
                getApiResponse.status_code = "1";
                getApiResponse.error_code = "315";
                getApiResponse.message = "Invalid bcode";
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            try
            {
                transferReq.player_id = tokenInfo.tpUserName;
                var result = bettingBLL.MoneyOutPK_OneGaming(transferReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    getApiResponse.status_code = "0";
                    getApiResponse.message = "";
                    getApiResponse.transaction_id = transferReq.transaction_id;
                    getApiResponse.agent_code = thirdPartySetting.TpSetting[$"agent_code_{tokenInfo.tpCurrency}"];
                    getApiResponse.player_id = tokenInfo.tpUserName;

                    if (tpAffterAmount == 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tokenInfo.tpUserName);
                        tpAffterAmount = tpCustBalance.Balance;
                    }

                    getApiResponse.balance = tpAffterAmount;
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tokenInfo.tpUserName);
                    getApiResponse.transaction_id = transferReq.transaction_id;
                    getApiResponse.agent_code = thirdPartySetting.TpSetting[$"agent_code_{tokenInfo.tpCurrency}"];
                    getApiResponse.player_id = tokenInfo.tpUserName;
                    getApiResponse.balance = tpCustBalance.Balance;
                }
                else if (tpAffterAmount < 0)
                {
                    getApiResponse.status_code = "1";
                    getApiResponse.error_code = "302";
                    getApiResponse.message = "Insufficient Balance";
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex}", logName);
                logger.LogError(ex, $"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}");
                throw;
            }

            log2File.Info($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpGet]
        [Route("[controller]/IG_playerMoneyIn")]
        public dynamic IG_playerMoneyIn([FromQuery] Dictionary<string, string> getReq)
        {
            string logName = $"{thisTp}_IG_playerMoneyIn";
            var json = JsonConvert.SerializeObject(getReq);
            var transferReq = JsonConvert.DeserializeObject<BettingDataPK_OneGaming>(json);

            TransferResp getApiResponse = new TransferResp();
            getApiResponse.status_code = "1";
            getApiResponse.message = "Error";
            getApiResponse.datetime = getCurrentGameTime();

            if (environment.IsDevelopment())
            {
                //System.Threading.Thread.Sleep(15000);
                //log2File.Info($"SLEEP:\r\n{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                //return getApiResponse;
            }

            if (string.IsNullOrEmpty(transferReq.l_token))
            {
                getApiResponse.status_code = "1";
                getApiResponse.message = "Token is null";
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tokenInfo = getTokenInfo(transferReq.l_token);

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);

            var verifyBcode = HMACSHA256($"{thirdPartySetting.TpSetting[$"agent_code_{tokenInfo.tpCurrency}"]}{tokenInfo.tpUserName}{transferReq.amount}", thirdPartySetting.TpSetting[$"secret_key_{tokenInfo.tpCurrency}"]);
            if (transferReq.bcode != verifyBcode)
            {
                getApiResponse.status_code = "1";
                getApiResponse.error_code = "315";
                getApiResponse.message = "Invalid bcode";
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            transferReq.player_id = tokenInfo.tpUserName;
            try
            {
                var result = settledBLL.MoneyInPK_OneGaming(transferReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    getApiResponse.status_code = "0";
                    getApiResponse.message = "";
                    getApiResponse.transaction_id = transferReq.transaction_id;
                    getApiResponse.agent_code = thirdPartySetting.TpSetting[$"agent_code_{tokenInfo.tpCurrency}"];
                    getApiResponse.player_id = tokenInfo.tpUserName;

                    if (tpAffterAmount == 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tokenInfo.tpUserName);
                        tpAffterAmount = tpCustBalance.Balance;
                    }

                    getApiResponse.balance = tpAffterAmount;

                    if (environment.IsDevelopment() && !transferReq.game_key.Equals("Refund", comparisonType: StringComparison.OrdinalIgnoreCase))
                    {
                        //getApiResponse.status_code = "1";
                        //getApiResponse.message = "Error";
                        //return getApiResponse;
                        //System.Threading.Thread.Sleep(15000);
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tokenInfo.tpUserName);
                    getApiResponse.status_code = "0";
                    getApiResponse.message = "";
                    getApiResponse.transaction_id = transferReq.transaction_id;
                    getApiResponse.agent_code = thirdPartySetting.TpSetting[$"agent_code_{tokenInfo.tpCurrency}"];
                    getApiResponse.player_id = tokenInfo.tpUserName;
                    getApiResponse.balance = tpCustBalance.Balance;
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex}", logName);
                logger.LogError(ex, $"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}");
                throw;
            }

            log2File.Info($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpGet]
        [Route("[controller]/IG_playerFailedTransaction")]
        public dynamic IG_playerFailedTransaction([FromQuery] Dictionary<string, string> getReq)
        {
            string logName = $"{thisTp}_IG_playerFailedTransaction";
            var json = JsonConvert.SerializeObject(getReq);
            var rollbackReq = JsonConvert.DeserializeObject<RollbackDataPK_OneGaming>(json);

            TransferResp getApiResponse = new TransferResp();
            getApiResponse.status_code = "1";
            getApiResponse.message = "Error";
            getApiResponse.datetime = getCurrentGameTime();

            if (string.IsNullOrEmpty(rollbackReq.l_token))
            {
                getApiResponse.status_code = "1";
                getApiResponse.message = "Token is null";
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tokenInfo = getTokenInfo(rollbackReq.l_token);

            rollbackReq.player_id = tokenInfo.tpUserName;
            try
            {
                var result = settledBLL.RollbackPK_OneGaming(rollbackReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tokenInfo.tpUserName);
                    getApiResponse.status_code = "0";
                    getApiResponse.message = "";
                    getApiResponse.player_id = tokenInfo.tpUserName;
                    getApiResponse.balance = tpCustBalance.Balance;
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex}", logName);
                logger.LogError(ex, $"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}");
                throw;
            }

            log2File.Info($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpGet]
        [Route("[controller]/IG_mobileAccess")]
        public dynamic IG_mobileAccess([FromQuery] Dictionary<string, string> getReq)
        {
            string logName = $"{thisTp}_IG_mobileAccess";
            var json = JsonConvert.SerializeObject(getReq);
            log2File.Info(json, logName);
            var mobileReq = JsonConvert.DeserializeObject<MobileReq>(json);

            MobileResp getApiResponse = new MobileResp();
            getApiResponse.status_code = "1";
            getApiResponse.message = "Error";
            getApiResponse.datetime = getCurrentGameTime();
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)thisTp, mobileReq.username);

            if (!string.IsNullOrEmpty(mobileReq.password) && tpCustInfo?.TpPwd == mobileReq.password)
            {
                string token = null;
                var tpCheck = thirdPartyBLL.ThirdPartyCheck((int)thisTp, mobileReq.username);

                if (tpCheck == null)
                {
                    string clientIp = "";
                    tpCustInfoBLL.AddLoginRecord(tpCustInfo.CustId, tpCustInfo.SiteId, clientIp, out token);
                    tpCustInfoBLL.AddUserLoginLog(tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, clientIp, "App", "App");
                    tpCheck = thirdPartyBLL.ThirdPartyCheck((int)thisTp, mobileReq.username);
                }
                else
                {
                    token = tpCheck.Token;
                }

                if (tpCheck.Locked)
                {
                    getApiResponse.status_code = "1";
                    getApiResponse.message = "Account is Locked";
                    log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, mobileReq.username);
                getApiResponse.status_code = "0";
                getApiResponse.message = "";
                getApiResponse.player_id = mobileReq.username;
                getApiResponse.balance = tpCustBalance.Balance;
                getApiResponse.agent_code = mobileReq.agent_code;

                var key = authBLL.ApplyToken(tpCustInfo);
                var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName, tpCustBalance.TpCurrencyId };
                byte[] bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sessionKeyArr));
                string sessionKey = Convert.ToBase64String(bytes);

                getApiResponse.l_token = HttpUtility.UrlEncode(sessionKey);
            }
            else
            {
                getApiResponse.status_code = "1";
                getApiResponse.error_code = "309";
                getApiResponse.message = "Invalid password";
            }
            log2File.Info($"{json}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        private string getCurrentGameTime()
        {
            return new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd HH:mm:ss");
        }

        private string HMACSHA256(string message, string key)
        {
            var encoding = new UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(key);
            byte[] messageBytes = encoding.GetBytes(message);
            using var hmacSHA256 = new HMACSHA256(keyByte);
            byte[] hashMessage = hmacSHA256.ComputeHash(messageBytes);
            return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
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
                tpCurrency = sessionKeyArr[3]
            };
        }
    }
}
