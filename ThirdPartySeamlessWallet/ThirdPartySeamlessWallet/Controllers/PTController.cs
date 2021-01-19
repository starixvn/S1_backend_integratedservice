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
    public class PTController : ControllerBase
    {
        private readonly AuthBLL authBLL;
        private readonly ILogger<PTController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly Tools tools;
        private readonly Tp thisTp;
        private readonly IHostEnvironment environment;

        public PTController(
            IHostEnvironment environment,
            ILogger<PTController> logger,
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
            this.thisTp = Tp.PT;
            this.environment = environment;
        }

        public class GetCommonParam
        {
            public string requestId { get; set; }
        }

        public class TokenInfo
        {
            public string shortTimeToken { get; set; }
            public string longTimeToken { get; set; }
        }

        #region Request

        public class GetAuthenticateReq : GetCommonParam
        {
            public string username { get; set; }
            public string externalToken { get; set; }
        }

        #endregion

        #region Response

        public class GetErrorResp : GetCommonParam
        {
            public Error error { get; set; }
        }

        public class Error
        {
            public string qualifier { get; set; }
            public string code { get; set; }

        }

        public class GetAuthenticateResp : GetCommonParam
        {
            public string username { get; set; }
            public string currencyCode { get; set; }
            public string countryCode { get; set; }
            public string permanentExternalToken { get; set; }
        }

        public class GetBalanceResp : GetCommonParam
        {
            public Balance balance { get; set; }
        }

        public class Balance
        {
            public decimal real { get; set; }
            public string timestamp { get; set; }
        }

        #endregion

        Dictionary<string, string> country = new Dictionary<string, string>(comparer: StringComparer.OrdinalIgnoreCase)
        {
            {"MYR","MY"},
            {"THB","TH"},
            {"CNY","CN"},
            {"IDR","ID"},
            {"VND","VN"},
            {"USD","US"},
        };

        [Route("[controller]")]
        [HttpGet]
        [HttpPost]
        public dynamic SeamlessWallet()
        {
            return true;
        }

        [HttpPost]
        [Route("[controller]/gameslink-adapter/logout")]
        public async Task<dynamic> logoutAsync()
        {
            string logName = $"{thisTp}";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info(Content, logName);
            logName = $"{logName}_logout";
            var getLogoutReq = JsonConvert.DeserializeObject<GetAuthenticateReq>(Content);

            GetCommonParam getLogoutResp = new GetCommonParam();
            getLogoutResp.requestId = getLogoutReq.requestId;

            log2File.Info($"{getLogoutReq}\r\n{JsonConvert.SerializeObject(getLogoutResp)}", logName);
            return getLogoutResp;
        }

        [HttpPost]
        [Route("[controller]/gameslink-adapter/authenticate")]
        public async Task<dynamic> authenticateAsync()
        {
            string logName = $"{thisTp}";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info(Content, logName);
            logName = $"{logName}_authenticate";
            var authenticateReq = JsonConvert.DeserializeObject<GetAuthenticateReq>(Content);

            GetErrorResp getErrorResp = new GetErrorResp();
            getErrorResp.requestId = authenticateReq.requestId;
            getErrorResp.error.qualifier = "GeneralError";
            getErrorResp.error.code = "INTERNAL_ERROR";

            // check param
            if (authenticateReq.requestId == null || authenticateReq.username == null || authenticateReq.externalToken == null)
            {
                //CONSTRAINT_VIOLATION
                getErrorResp.requestId = authenticateReq.requestId;
                getErrorResp.error.qualifier = "GeneralError";
                getErrorResp.error.code = "CONSTRAINT_VIOLATION";
                log2File.Error($"Request:\r\n{authenticateReq}\r\nResponse:\r\n{JsonConvert.SerializeObject(getErrorResp)}", logName);
                return getErrorResp;
            }

            var tokenInfo = getTokenInfo(authenticateReq.externalToken);

            if (authBLL.Authenticate(tokenInfo.shortTimeToken, out TpCustInfoAuth tpCustInfoAuth))
            {
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
                if (tpCheck == null)
                {
                    getErrorResp.error.qualifier = "AuthenticateError";
                    getErrorResp.error.code = "ERR_PLAYER_NOT_FOUND";
                    log2File.Error($"Request:\r\n{authenticateReq}\r\nResponse:\r\n{JsonConvert.SerializeObject(getErrorResp)}", logName);
                    return getErrorResp;
                }
                else if (tpCheck.Locked)
                {
                    getErrorResp.error.qualifier = "AuthenticateError";
                    getErrorResp.error.code = "ERR_PLAYER_NOT_FOUND";
                    log2File.Error($"Request:\r\n{authenticateReq}\r\nResponse:\r\n{JsonConvert.SerializeObject(getErrorResp)}", logName);
                    return getErrorResp;
                }

                GetAuthenticateResp getAuthenticateResp = new GetAuthenticateResp();
                getAuthenticateResp.requestId = authenticateReq.requestId;
                getAuthenticateResp.username = authenticateReq.username;
                getAuthenticateResp.currencyCode = tpCustInfoAuth.TpCurrencyId;
                getAuthenticateResp.countryCode = country[tpCustInfoAuth.TpCurrencyId];
                getAuthenticateResp.permanentExternalToken = HttpUtility.UrlEncode(tokenInfo.longTimeToken);
                log2File.Info($"Request:\r\n{authenticateReq}\r\nResponse:\r\n{JsonConvert.SerializeObject(getAuthenticateResp)}", logName);
                return getAuthenticateResp;
            }
            else
            {
                getErrorResp.error.qualifier = "AuthenticateError";
                getErrorResp.error.code = "ERR_AUTHENTICATION_FAILED";
                log2File.Error($"Request:\r\n{authenticateReq}\r\nResponse:\r\n{JsonConvert.SerializeObject(getErrorResp)}", logName);
                return getErrorResp;
            }
        }

        [HttpPost]
        [Route("[controller]/gameslink-adapter/getbalance")]
        public async Task<dynamic> getBalanceAsync()
        {
            string logName = $"{thisTp}";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info(Content, logName);
            logName = $"{logName}_getbalance";
            var getBalanceReq = JsonConvert.DeserializeObject<GetAuthenticateReq>(Content);

            GetErrorResp getErrorResp = new GetErrorResp();
            getErrorResp.requestId = getBalanceReq.requestId;
            getErrorResp.error.qualifier = "GeneralError";
            getErrorResp.error.code = "INTERNAL_ERROR";

            // check param
            if (getBalanceReq.requestId == null || getBalanceReq.username == null || getBalanceReq.externalToken == null)
            {
                //CONSTRAINT_VIOLATION
                getErrorResp.requestId = getBalanceReq.requestId;
                getErrorResp.error.qualifier = "GeneralError";
                getErrorResp.error.code = "CONSTRAINT_VIOLATION";
                log2File.Error($"Request:\r\n{getBalanceReq}\r\nResponse:\r\n{JsonConvert.SerializeObject(getErrorResp)}", logName);
                return getErrorResp;
            }

            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getBalanceReq.username);
            if (tpCheck == null)
            {
                // check player account exist
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, getBalanceReq.username);
                if (tpCustInfo == null)
                {
                    getErrorResp.error.qualifier = "GetBalanceError";
                    getErrorResp.error.code = "ERR_PLAYER_NOT_FOUND";
                    log2File.Error($"Request:\r\n{getBalanceReq}\r\nResponse:\r\n{JsonConvert.SerializeObject(getErrorResp)}", logName);
                    return getErrorResp;
                }
            }
            //else if (tpCheck.Locked)
            //{
            //    getErrorResp.error.qualifier = "GetBalanceError";
            //    getErrorResp.error.code = "ERR_PLAYER_NOT_FOUND";
            //    log2File.Error($"Request:\r\n{getBalanceReq}\r\nResponse:\r\n{JsonConvert.SerializeObject(getErrorResp)}", logName);
            //    return getErrorResp;
            //}

            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBalanceReq.username);
            if (tpCustBalance != null)
            {
                GetBalanceResp balanceResp = new GetBalanceResp();
                balanceResp.requestId = getBalanceReq.requestId;
                balanceResp.balance.real = (tpCheck == null) ? 0 : tpCustBalance.Balance;
                balanceResp.balance.timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                log2File.Info($"Request:\r\n{getBalanceReq}\r\nResponse:\r\n{JsonConvert.SerializeObject(balanceResp)}", logName);
                return getErrorResp;
            }
            log2File.Error($"Request:\r\n{getBalanceReq}\r\nResponse:\r\n{JsonConvert.SerializeObject(getErrorResp)}", logName);
            return getErrorResp;
        }

        [HttpPost]
        [Route("[controller]/gameslink-adapter/gameRoundResult")]
        public async Task<dynamic> gameRoundResultAsync()
        {
            string logName = $"{thisTp}";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info(Content, logName);
            logName = $"{logName}_gameRoundResult";
            var getGameRoundResultReq = JsonConvert.DeserializeObject<GetAuthenticateReq>(Content);

            GetErrorResp getErrorResp = new GetErrorResp();
            getErrorResp.requestId = getGameRoundResultReq.requestId;
            getErrorResp.error.qualifier = "GeneralError";
            getErrorResp.error.code = "INTERNAL_ERROR";

            log2File.Error($"{getGameRoundResultReq}\r\n{JsonConvert.SerializeObject(getErrorResp)}", logName);
            return getErrorResp;
        }

        private TokenInfo getTokenInfo(string token)
        {
            string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(token));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            return new TokenInfo
            {
                shortTimeToken = sessionKeyArr[0], /* 1 min*/
                longTimeToken = sessionKeyArr[1],  /* Long Time*/
            };
        }
    }
}
