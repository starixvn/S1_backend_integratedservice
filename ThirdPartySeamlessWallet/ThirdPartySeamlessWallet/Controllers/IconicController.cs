using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public class IconicController : ControllerBase
    {
        private readonly AuthBLL authBLL;
        private readonly ILogger<IconicController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly Tools tools;
        private readonly Tp thisTp;
        private readonly IHostEnvironment environment;

        public IconicController(
            IHostEnvironment environment,
            ILogger<IconicController> logger,
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
            this.thisTp = Tp.Iconic;
            this.environment = environment;
        }

        public enum Error
        {
            OK = 200,
            INVALID_DATA = 400,
            ERROR = 500,
        }

        #region Response

        public class GetErrorResp
        {
            public GetErrorResp()
            {
                data = new Data();
                error = new ErrorDetail();
            }
            public Data data { get; set; }
            public ErrorDetail error { get; set; }
        }

        public class Data
        {
            public int statusCode { get; set; }

        }

        public class ErrorDetail
        {
            public string title { get; set; }
            public string description { get; set; }

        }

        public class GetApiResp
        {
            public GetApiResp()
            {
                data = new BalanceData();
                error = new ErrorDetail();
            }
            public BalanceData data { get; set; }
            public ErrorDetail error { get; set; }
        }

        public class BalanceData
        {
            public int statusCode { get; set; }
            public string username { get; set; }
            public decimal balance { get; set; }
            public string hash { get; set; }
        }

        #endregion

        [HttpGet]
        [Route("[controller]/callback")]
        public dynamic callback(string token, bool isInfo)
        {
            string logName = $"{thisTp}_verify";
            GetErrorResp getApiResponse = new GetErrorResp();
            Response.StatusCode = (int)Error.INVALID_DATA;
            getApiResponse.data.statusCode = 999;
            getApiResponse.error.title = "Undefined Errors";
            getApiResponse.error.description = "Undefined Errors";

            if (string.IsNullOrEmpty(token))
            {
                getApiResponse.data.statusCode = 1;
                getApiResponse.error.title = "TOKEN_IS_NULL";
                getApiResponse.error.description = "Token is nil";
                log2File.Info($"token={token}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            if (isInfo == true)
            {
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(token));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string authToken = sessionKeyArr[0]; /* 1 Time*/
                string sessionToken = sessionKeyArr[1];/* Long Time*/
                string tpUserName = sessionKeyArr[2];/* User Name*/

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpUserName);
                if (tpCheck == null)
                {
                    getApiResponse.data.statusCode = 3;
                    getApiResponse.error.title = "INVALID_USER";
                    getApiResponse.error.description = "Invalid user Id";
                    log2File.Info($"token={JsonConvert.SerializeObject(sessionKeyArr)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (tpCheck.Token != sessionToken)
                {
                    /*User has Log out*/
                    getApiResponse.data.statusCode = 1;
                    getApiResponse.error.title = "Wrong_Token";
                    getApiResponse.error.description = "Wrong Token";
                    log2File.Info($"{JsonConvert.SerializeObject(sessionKeyArr)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (tpCheck.Locked)
                {
                    getApiResponse.data.statusCode = 3;
                    getApiResponse.error.title = "USER_IS_LOCKED";
                    getApiResponse.error.description = "Account is Locked";
                    log2File.Error($"token={JsonConvert.SerializeObject(sessionKeyArr)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpUserName);
                    if (tpCustBalance != null)
                    {
                        var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);

                        Response.StatusCode = (int)Error.OK;
                        GetApiResp getBalanceResp = new GetApiResp();
                        getBalanceResp.data.statusCode = 0;
                        getBalanceResp.data.username = tpUserName;
                        getBalanceResp.data.balance = tpCustBalance.Balance;
                        getBalanceResp.data.hash = tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{tpUserName}{tpCustBalance.Balance}").ToLower();
                        log2File.Info($"token={JsonConvert.SerializeObject(sessionKeyArr)}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                        return getBalanceResp;
                    }
                }
            }

            log2File.Info($"token={token}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpGet]
        [Route("[controller]/verify")]
        public dynamic verify(string token)
        {
            string logName = $"{thisTp}_verify";
            GetErrorResp getApiResponse = new GetErrorResp();
            Response.StatusCode = (int)Error.INVALID_DATA;
            getApiResponse.data.statusCode = 999;
            getApiResponse.error.title = "Undefined Errors";
            getApiResponse.error.description = "Undefined Errors";

            if (string.IsNullOrEmpty(token))
            {
                getApiResponse.data.statusCode = 1;
                getApiResponse.error.title = "TOKEN_IS_NULL";
                getApiResponse.error.description = "Token is nil";
                log2File.Info($"token={token}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(token));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            string authToken = sessionKeyArr[0]; /* 1 Time*/
            string sessionToken = sessionKeyArr[1];/* Long Time*/
            string tpUserName = sessionKeyArr[2];/* User Name*/
            string sGameType = sessionKeyArr[3];/*Game Type*/
            int.TryParse(sGameType, out int gameType);

            if (authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
            {
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
                if (tpCheck == null)
                {
                    getApiResponse.data.statusCode = 3;
                    getApiResponse.error.title = "INVALID_USER";
                    getApiResponse.error.description = "Invalid user Id";
                    log2File.Info($"token={JsonConvert.SerializeObject(sessionKeyArr)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (tpCheck.Locked)
                {
                    getApiResponse.data.statusCode = 3;
                    getApiResponse.error.title = "USER_IS_LOCKED";
                    getApiResponse.error.description = "Account is Locked";
                    log2File.Info($"token={JsonConvert.SerializeObject(sessionKeyArr)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpCustInfoAuth.TpUserName, gameType);
                if (tpCustBalance != null)
                {
                    var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                    if (string.IsNullOrEmpty(thirdPartySetting.TpSetting["secure_code"]))
                    {
                        log2File.Info($"token={JsonConvert.SerializeObject(sessionKeyArr)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }

                    Response.StatusCode = (int)Error.OK;
                    GetApiResp getBalanceResp = new GetApiResp();
                    getBalanceResp.data.statusCode = 0;
                    getBalanceResp.data.username = tpCustInfoAuth.TpUserName;
                    getBalanceResp.data.balance = Math.Truncate(tpCustBalance.Balance);
                    getBalanceResp.data.hash = tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{tpCustInfoAuth.TpUserName}{Math.Truncate(tpCustBalance.Balance)}").ToLower();
                    log2File.Info($"token={JsonConvert.SerializeObject(sessionKeyArr)}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                    return getBalanceResp;
                }
            }
            else if (gameType == (int)GameType.Iconic_Fish)
            {
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpUserName);
                if (tpCheck == null)
                {
                    getApiResponse.data.statusCode = 3;
                    getApiResponse.error.title = "INVALID_USER";
                    getApiResponse.error.description = "Invalid user Id";
                    log2File.Info($"token={JsonConvert.SerializeObject(sessionKeyArr)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (tpCheck.Token != sessionToken)
                {
                    /*User has Log out*/
                    getApiResponse.data.statusCode = 1;
                    getApiResponse.error.title = "Wrong_Token";
                    getApiResponse.error.description = "Wrong Token";
                    log2File.Info($"{JsonConvert.SerializeObject(sessionKeyArr)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (tpCheck.Locked)
                {
                    getApiResponse.data.statusCode = 3;
                    getApiResponse.error.title = "USER_IS_LOCKED";
                    getApiResponse.error.description = "Account is Locked";
                    log2File.Info($"token={JsonConvert.SerializeObject(sessionKeyArr)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpUserName, gameType);
                if (tpCustBalance != null)
                {
                    var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                    if (string.IsNullOrEmpty(thirdPartySetting.TpSetting["secure_code"]))
                    {
                        log2File.Info($"token={JsonConvert.SerializeObject(sessionKeyArr)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }

                    Response.StatusCode = (int)Error.OK;
                    GetApiResp getBalanceResp = new GetApiResp();
                    getBalanceResp.data.statusCode = 0;
                    getBalanceResp.data.username = tpUserName;
                    getBalanceResp.data.balance = Math.Truncate(tpCustBalance.Balance);
                    getBalanceResp.data.hash = tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{tpUserName}{Math.Truncate(tpCustBalance.Balance)}").ToLower();
                    log2File.Info($"token={JsonConvert.SerializeObject(sessionKeyArr)}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                    return getBalanceResp;
                }
            }

            log2File.Info($"token={JsonConvert.SerializeObject(sessionKeyArr)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpPost]
        [Route("[controller]/bet")]
        public async Task<dynamic> betAsync()
        {
            string logName = $"{thisTp}_bet";

            GetErrorResp getApiResponse = new GetErrorResp();
            Response.StatusCode = (int)Error.INVALID_DATA;
            getApiResponse.data.statusCode = 999;
            getApiResponse.error.title = "Undefined Errors";
            getApiResponse.error.description = "Undefined Errors";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info($"{Content}", logName);

            BettingDataRNG_Iconic getReq = JsonConvert.DeserializeObject<BettingDataRNG_Iconic>(Content);

            if (string.IsNullOrEmpty(getReq.token))
            {
                getApiResponse.data.statusCode = 1;
                getApiResponse.error.title = "TOKEN_IS_NULL";
                getApiResponse.error.description = "Token is nil";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (string.IsNullOrEmpty(getReq.hash) || !string.Equals(getReq.hash, tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{getReq.transactionId}{getReq.amount}"), StringComparison.OrdinalIgnoreCase))
            {
                getApiResponse.data.statusCode = 4;
                getApiResponse.error.title = "Wrong Hash";
                getApiResponse.error.description = "Wrong Hash";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(getReq.token));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            string authToken = sessionKeyArr[0]; /* 1 Time*/
            string sessionToken = sessionKeyArr[1];/* Long Time*/
            string tpUserName = sessionKeyArr[2];/* User Name*/
            var gameType = thirdPartyBLL.GetGameType((int)thisTp, getReq.productId);
            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpUserName);
            if (tpCheck == null)
            {
                getApiResponse.data.statusCode = 3;
                getApiResponse.error.title = "INVALID_USER";
                getApiResponse.error.description = "Invalid user Id";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            else if (tpCheck.Token != sessionToken)
            {
                /*User has Log out*/
                getApiResponse.data.statusCode = 1;
                getApiResponse.error.title = "Wrong_Token";
                getApiResponse.error.description = "Wrong Token";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            else if (tpCheck.Locked)
            {
                getApiResponse.data.statusCode = 3;
                getApiResponse.error.title = "USER_IS_LOCKED";
                getApiResponse.error.description = "Account is Locked";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            try
            {
                GetApiResp getBetResp = new GetApiResp();
                getReq.username = tpUserName;

                var result = bettingBLL.PlaceBetRNG_Iconic(getReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    Response.StatusCode = (int)Error.OK;
                    getBetResp.data.statusCode = 0;
                    getBetResp.data.username = tpUserName;
                    getBetResp.data.balance = Math.Truncate(tpAffterAmount);
                    getBetResp.data.hash = tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{tpUserName}{Math.Truncate(tpAffterAmount)}").ToLower();
                    log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                    return getBetResp;
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpUserName, (int)gameType);
                    Response.StatusCode = (int)Error.OK;
                    getBetResp.data.statusCode = 0;
                    getBetResp.data.username = tpUserName;
                    getBetResp.data.balance = Math.Truncate(tpCustBalance.Balance);
                    getBetResp.data.hash = tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{tpUserName}{Math.Truncate(tpCustBalance.Balance)}").ToLower();
                    log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                    return getBetResp;
                }
                else if (tpAffterAmount < 0)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpUserName, (int)gameType);
                    Response.StatusCode = (int)Error.INVALID_DATA;
                    getBetResp.data.statusCode = 2;
                    getBetResp.data.username = tpUserName;
                    getBetResp.data.balance = Math.Truncate(tpAffterAmount);
                    getBetResp.data.hash = tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{tpUserName}{Math.Truncate(tpAffterAmount)}").ToLower();
                    getBetResp.error.title = "Not Enough Balance";
                    getBetResp.error.description = "Not Enough Balance";
                    log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                    return getBetResp;
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)Error.ERROR;
                log2File.Error($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex}", logName);
                return getApiResponse;
            }

            log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpDelete]
        [Route("[controller]/bet")]
        public async Task<dynamic> cancel_betAsync()
        {
            string logName = $"{thisTp}_cancel_bet";

            GetErrorResp getApiResponse = new GetErrorResp();
            Response.StatusCode = (int)Error.INVALID_DATA;
            getApiResponse.data.statusCode = 999;
            getApiResponse.error.title = "Undefined Errors";
            getApiResponse.error.description = "Undefined Errors";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info($"{Content}", logName);
            BettingDataRNG_Iconic getReq = JsonConvert.DeserializeObject<BettingDataRNG_Iconic>(Content);

            if (string.IsNullOrEmpty(getReq.token))
            {
                getApiResponse.data.statusCode = 1;
                getApiResponse.error.title = "TOKEN_IS_NULL";
                getApiResponse.error.description = "Token is nil";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (string.IsNullOrEmpty(getReq.hash) || !string.Equals(getReq.hash, tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{getReq.transactionId}{getReq.amount}"), StringComparison.OrdinalIgnoreCase))
            {
                getApiResponse.data.statusCode = 4;
                getApiResponse.error.title = "Wrong Hash";
                getApiResponse.error.description = "Wrong Hash";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(getReq.token));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            string authToken = sessionKeyArr[0]; /* 1 Time*/
            string sessionToken = sessionKeyArr[1];/* Long Time*/
            string tpUserName = sessionKeyArr[2];/* User Name*/

            var gameType = thirdPartyBLL.GetGameType((int)thisTp, getReq.productId);
            getReq.username = tpUserName;

            var result = settledBLL.CancelBetRNG_Iconic(getReq, out decimal tpAffterAmount);
            if (result > 0 || result == -1)
            {
                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpUserName, (int)gameType);
                Response.StatusCode = (int)Error.OK;
                GetApiResp getCancelBetResp = new GetApiResp();
                getCancelBetResp.data.statusCode = 0;
                getCancelBetResp.data.username = tpUserName;
                getCancelBetResp.data.balance = Math.Truncate(tpCustBalance.Balance);
                getCancelBetResp.data.hash = tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{tpUserName}{Math.Truncate(tpCustBalance.Balance)}").ToLower();
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getCancelBetResp)}", logName);
                return getCancelBetResp;
            }

            log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpPost]
        [Route("[controller]/win")]
        public async Task<dynamic> winAsync()
        {
            string logName = $"{thisTp}_win";

            GetErrorResp getApiResponse = new GetErrorResp();
            Response.StatusCode = (int)Error.INVALID_DATA;
            getApiResponse.data.statusCode = 999;
            getApiResponse.error.title = "Undefined Errors";
            getApiResponse.error.description = "Undefined Errors";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info($"{Content}", logName);
            BettingDataRNG_Iconic getReq = JsonConvert.DeserializeObject<BettingDataRNG_Iconic>(Content);

            if (string.IsNullOrEmpty(getReq.token))
            {
                getApiResponse.data.statusCode = 1;
                getApiResponse.error.title = "TOKEN_IS_NULL";
                getApiResponse.error.description = "Token is nil";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (string.IsNullOrEmpty(getReq.hash) || !string.Equals(getReq.hash, tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{getReq.transactionId}{getReq.amount}"), StringComparison.OrdinalIgnoreCase))
            {
                getApiResponse.data.statusCode = 4;
                getApiResponse.error.title = "Wrong Hash";
                getApiResponse.error.description = "Wrong Hash";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(getReq.token));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            string authToken = sessionKeyArr[0]; /* 1 Time*/
            string sessionToken = sessionKeyArr[1];/* Long Time*/
            string tpUserName = sessionKeyArr[2];/* User Name*/

            GetApiResp getPayoutResp = new GetApiResp();

            var gameType = thirdPartyBLL.GetGameType((int)thisTp, getReq.productId);
            getReq.username = tpUserName;

            var result = settledBLL.PayoutRNG_Iconic(getReq, out decimal tpAffterAmount);
            if (result > 0 || result == -1)
            {
                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpUserName, (int)gameType);
                Response.StatusCode = (int)Error.OK;
                getPayoutResp.data.statusCode = 0;
                getPayoutResp.data.username = tpUserName;
                getPayoutResp.data.balance = Math.Truncate(tpCustBalance.Balance);
                getPayoutResp.data.hash = tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{tpUserName}{Math.Truncate(tpCustBalance.Balance)}").ToLower();
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getPayoutResp)}", logName);
                return getPayoutResp;
            }

            log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpPost]
        [Route("[controller]/deposit")]
        public async Task<dynamic> depositAsync()
        {
            string logName = $"{thisTp}_deposit";

            GetErrorResp getApiResponse = new GetErrorResp();
            Response.StatusCode = (int)Error.INVALID_DATA;
            getApiResponse.data.statusCode = 999;
            getApiResponse.error.title = "Undefined Errors";
            getApiResponse.error.description = "Undefined Errors";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info($"{Content}", logName);

            BettingDataFish_Iconic getReq = JsonConvert.DeserializeObject<BettingDataFish_Iconic>(Content);

            if (string.IsNullOrEmpty(getReq.token))
            {
                getApiResponse.data.statusCode = 1;
                getApiResponse.error.title = "TOKEN_IS_NULL";
                getApiResponse.error.description = "Token is nil";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (string.IsNullOrEmpty(getReq.hash) || !string.Equals(getReq.hash, tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{getReq.transactionId}{getReq.amount}"), StringComparison.OrdinalIgnoreCase))
            {
                getApiResponse.data.statusCode = 4;
                getApiResponse.error.title = "Wrong Hash";
                getApiResponse.error.description = "Wrong Hash";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(getReq.token));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            string authToken = sessionKeyArr[0]; /* 1 Time*/
            string sessionToken = sessionKeyArr[1];/* Long Time*/
            string tpUserName = sessionKeyArr[2];/* User Name*/

            var gameType = GameType.Iconic_Fish;
            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpUserName);
            if (tpCheck == null)
            {
                getApiResponse.data.statusCode = 3;
                getApiResponse.error.title = "INVALID_USER";
                getApiResponse.error.description = "Invalid user Id";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            else if (tpCheck.Token != sessionToken)
            {
                /*User has Log out*/
                getApiResponse.data.statusCode = 1;
                getApiResponse.error.title = "Wrong_Token";
                getApiResponse.error.description = "Wrong Token";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
                return getApiResponse;
            }
            else if (tpCheck.Locked)
            {
                getApiResponse.data.statusCode = 3;
                getApiResponse.error.title = "USER_IS_LOCKED";
                getApiResponse.error.description = "Account is Locked";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            try
            {
                GetApiResp getBetResp = new GetApiResp();

                getReq.username = tpUserName;

                var result = bettingBLL.PlaceBetFish_Iconic(getReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    Response.StatusCode = (int)Error.OK;
                    getBetResp.data.statusCode = 0;
                    getBetResp.data.username = tpUserName;
                    getBetResp.data.balance = Math.Truncate(tpAffterAmount);
                    getBetResp.data.hash = tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{tpUserName}{Math.Truncate(tpAffterAmount)}").ToLower();
                    log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                    return getBetResp;
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpUserName, (int)gameType);
                    Response.StatusCode = (int)Error.OK;
                    getBetResp.data.statusCode = 0;
                    getBetResp.data.username = tpUserName;
                    getBetResp.data.balance = Math.Truncate(tpCustBalance.Balance);
                    getBetResp.data.hash = tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{tpUserName}{Math.Truncate(tpCustBalance.Balance)}").ToLower();
                    log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                    return getBetResp;
                }
                else if (tpAffterAmount < 0)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpUserName, (int)gameType);
                    Response.StatusCode = (int)Error.INVALID_DATA;
                    getBetResp.data.statusCode = 2;
                    getBetResp.data.username = tpUserName;
                    getBetResp.data.balance = Math.Truncate(tpAffterAmount);
                    getBetResp.data.hash = tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{tpUserName}{Math.Truncate(tpAffterAmount)}").ToLower();
                    getBetResp.error.title = "Not Enough Balance";
                    getBetResp.error.description = "Not Enough Balance";
                    log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                    return getBetResp;
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)Error.ERROR;
                log2File.Error($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex}", logName);
                return getApiResponse;
            }

            log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpDelete]
        [Route("[controller]/deposit")]
        public async Task<dynamic> cancel_depositAsync()
        {
            string logName = $"{thisTp}_cancel_deposit";

            GetErrorResp getApiResponse = new GetErrorResp();
            Response.StatusCode = (int)Error.INVALID_DATA;
            getApiResponse.data.statusCode = 999;
            getApiResponse.error.title = "Undefined Errors";
            getApiResponse.error.description = "Undefined Errors";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info($"{Content}", logName);
            BettingDataFish_Iconic getReq = JsonConvert.DeserializeObject<BettingDataFish_Iconic>(Content);

            if (string.IsNullOrEmpty(getReq.token))
            {
                getApiResponse.data.statusCode = 1;
                getApiResponse.error.title = "TOKEN_IS_NULL";
                getApiResponse.error.description = "Token is nil";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (string.IsNullOrEmpty(getReq.hash) || !string.Equals(getReq.hash, tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{getReq.transactionId}{getReq.amount}"), StringComparison.OrdinalIgnoreCase))
            {
                getApiResponse.data.statusCode = 4;
                getApiResponse.error.title = "Wrong Hash";
                getApiResponse.error.description = "Wrong Hash";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(getReq.token));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            string authToken = sessionKeyArr[0]; /* 1 Time*/
            string sessionToken = sessionKeyArr[1];/* Long Time*/
            string tpUserName = sessionKeyArr[2];/* User Name*/

            var gameType = GameType.Iconic_Fish;
            getReq.username = tpUserName;

            var result = settledBLL.CancelBetFish_Iconic(getReq, out decimal tpAffterAmount);
            if (result > 0 || result == -1)
            {
                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpUserName, (int)gameType);
                Response.StatusCode = (int)Error.OK;
                GetApiResp getCancelBetResp = new GetApiResp();
                getCancelBetResp.data.statusCode = 0;
                getCancelBetResp.data.username = tpUserName;
                getCancelBetResp.data.balance = Math.Truncate(tpAffterAmount);
                getCancelBetResp.data.hash = tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{tpUserName}{Math.Truncate(tpAffterAmount)}").ToLower();
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getCancelBetResp)}", logName);
                return getCancelBetResp;
            }

            log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpPost]
        [Route("[controller]/withdraw")]
        public async Task<dynamic> withdrawAsync()
        {
            string logName = $"{thisTp}_withdraw";

            GetErrorResp getApiResponse = new GetErrorResp();
            Response.StatusCode = (int)Error.INVALID_DATA;
            getApiResponse.data.statusCode = 999;
            getApiResponse.error.title = "Undefined Errors";
            getApiResponse.error.description = "Undefined Errors";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info($"{Content}", logName);
            BettingDataFish_Iconic getReq = JsonConvert.DeserializeObject<BettingDataFish_Iconic>(Content);

            if (string.IsNullOrEmpty(getReq.token))
            {
                getApiResponse.data.statusCode = 1;
                getApiResponse.error.title = "TOKEN_IS_NULL";
                getApiResponse.error.description = "Token is nil";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (string.IsNullOrEmpty(getReq.hash) || !string.Equals(getReq.hash, tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{getReq.transactionId}{getReq.amount}"), StringComparison.OrdinalIgnoreCase))
            {
                getApiResponse.data.statusCode = 4;
                getApiResponse.error.title = "Wrong Hash";
                getApiResponse.error.description = "Wrong Hash";
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(getReq.token));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            string authToken = sessionKeyArr[0]; /* 1 Time*/
            string sessionToken = sessionKeyArr[1];/* Long Time*/
            string tpUserName = sessionKeyArr[2];/* User Name*/

            GetApiResp getPayoutResp = new GetApiResp();

            var gameType = GameType.Iconic_Fish;
            getReq.username = tpUserName;

            var result = settledBLL.PayoutFish_Iconic(getReq, out decimal tpAffterAmount);
            if (result > 0)
            {
                Response.StatusCode = (int)Error.OK;
                getPayoutResp.data.statusCode = 0;
                getPayoutResp.data.username = tpUserName;
                getPayoutResp.data.balance = Math.Truncate(tpAffterAmount);
                getPayoutResp.data.hash = tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{tpUserName}{Math.Truncate(tpAffterAmount)}").ToLower();
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getPayoutResp)}", logName);
                return getPayoutResp;
            }
            else if (result == -1)
            {
                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpUserName, (int)gameType);
                Response.StatusCode = (int)Error.OK;
                getPayoutResp.data.statusCode = 0;
                getPayoutResp.data.username = tpUserName;
                getPayoutResp.data.balance = Math.Truncate(tpCustBalance.Balance);
                getPayoutResp.data.hash = tools.ToMD5($"{thirdPartySetting.TpSetting["secure_code"]}{tpUserName}{Math.Truncate(tpCustBalance.Balance)}").ToLower();
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getPayoutResp)}", logName);
                return getPayoutResp;
            }

            log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }
    }
}
