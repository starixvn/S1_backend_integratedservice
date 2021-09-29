using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]

    public class EVOController : ControllerBase
    {
        private readonly ILogger<EVOController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Log2File _log2File;
        private readonly Tools _tools;
        private readonly Tp thisTp;

        public EVOController(
            IServiceProvider provider,
            ILogger<EVOController> logger,
            IConfiguration configuration,
            IHostEnvironment environment
        )
        {
            _logger = logger;
            _log2File = provider.GetRequiredService<Log2File>();
            _configuration = configuration;
            _environment = environment;
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tools = provider.GetRequiredService<Tools>();
            thisTp = Tp.EVO;
        }

        #region HttpStatusCode

        public enum Error
        {
            OK = 200,
            INVALID_DATA = 400,
            UAUTHORIZED = 401,
            NotFound = 404,
            ERROR = 500,
        }

        #endregion

        public class TokenInfo
        {
            public string shortTimeToken { get; set; }
            public string longTimeToken { get; set; }
            public string tpUserName { get; set; }
            public int gameType { get; set; }
            public int tpId { get; set; }
        }

        #region Request


        public class AuthReq
        {
            public Channel channel { get; set; }
            public string sid { get; set; }
            public string userId { get; set; }
            public string uuid { get; set; }
        }
        public class Channel
        {
            public string type { get; set; }
        }


        #endregion

        #region Response

        public class CommonResp
        {
            public string status { get; set; }
        }

        public class AuthResp : CommonResp
        {
        }

        public class BalanceResp : CommonResp
        {
            public decimal balance { get; set; }
            public string uuid { get; set; }
        }

        public class BetResp : CommonResp
        {
            public decimal balance { get; set; }
            public string uuid { get; set; }
            public bool retransmission { get; set; }
        }

        #endregion

        [HttpPost]
        [Route("[controller]/check")]
        public async Task<dynamic> checkAsync([FromQuery] Dictionary<string, string> getQuery)
        {
            string logName = $"{thisTp}_common_check";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            _log2File.Info(Content, logName);

            AuthReq getReq = JsonConvert.DeserializeObject<AuthReq>(Content);
            AuthResp getApiResponse = new AuthResp();

            if (string.IsNullOrEmpty(getReq.sid))
            {
                getApiResponse.status = "INVALID_SID";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tokenInfo = getTokenInfo(getReq.sid);
            int tpId = tokenInfo.tpId;
            if (tpId == 0)
            {
                getApiResponse.status = "INVALID_SID";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            logName = $"{(Tp)tpId}_check";

            var authToken = getQuery["authToken"];
            var check = checkAuthorization(authToken);
            if (check.status != "OK")
            {
                getApiResponse.status = check.status;
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n", logName);
                return getApiResponse;
            }

            var verify = _authBLL.Authenticate(tokenInfo.shortTimeToken, out TpCustInfoAuth tpCustInfoAuth);
            //if (!verify)
            //{
            //    getApiResponse.status = "INVALID_TOKEN_ID";
            //    _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            //    return getApiResponse;
            //}

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(tpId, getReq.userId);
            if (tpCheck == null)
            {
                getApiResponse.status = "INVALID_TOKEN_ID";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            else if (tpCheck.Locked)
            {
                getApiResponse.status = "ACCOUNT_LOCKED";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            getApiResponse.status = "OK";
            _log2File.Info($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpPost]
        [Route("[controller]/balance")]
        public async Task<dynamic> balanceAsync([FromQuery] Dictionary<string, string> getQuery)
        {
            string logName = $"{thisTp}_common_balance";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            _log2File.Info(Content, logName);

            AuthReq getReq = JsonConvert.DeserializeObject<AuthReq>(Content);
            BalanceResp getApiResponse = new BalanceResp();

            if (string.IsNullOrEmpty(getReq.sid))
            {
                getApiResponse.status = "INVALID_SID";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tokenInfo = getTokenInfo(getReq.sid);
            int tpId = tokenInfo.tpId;
            int gameType = tokenInfo.gameType;
            if (tpId == 0 || gameType == 0)
            {
                getApiResponse.status = "INVALID_SID";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            logName = $"{(Tp)tpId}_balance";

            var authToken = getQuery["authToken"];
            var check = checkAuthorization(authToken);
            if (check.status != "OK")
            {
                getApiResponse.status = check.status;
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n", logName);
                return getApiResponse;
            }

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(tpId, getReq.userId);
            if (tpCheck == null)
            {
                getApiResponse.status = "INVALID_TOKEN_ID";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            else if (tpCheck.Locked)
            {
                getApiResponse.status = "ACCOUNT_LOCKED";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(tpId, getReq.userId, gameType);
            if (tpCustBalance != null)
            {
                getApiResponse.status = "OK";
                getApiResponse.balance = tpCustBalance.Balance;
                getApiResponse.uuid = Guid.NewGuid().ToString();
                _log2File.Info($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            getApiResponse.status = "UNKNOWN_ERROR";
            _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpPost]
        [Route("[controller]/debit")]
        public async Task<dynamic> debitAsync([FromQuery] Dictionary<string, string> getQuery)
        {
            string logName = $"{thisTp}_common_debit";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            _log2File.Info(Content, logName);

            BettingData_EVO getReq = JsonConvert.DeserializeObject<BettingData_EVO>(Content);
            BetResp getApiResponse = new BetResp();
            getApiResponse.status = "UNKNOWN_ERROR";

            //if (_environment.IsDevelopment() && getReq.transaction.amount >= 300)
            //{
            //    Response.StatusCode = (int)Error.ERROR;
            //    //System.Threading.Thread.Sleep(60000);
            //    return getApiResponse;
            //}

            if (string.IsNullOrEmpty(getReq.sid))
            {
                getApiResponse.status = "INVALID_SID";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tokenInfo = getTokenInfo(getReq.sid);
            int tpId = tokenInfo.tpId;
            int gameType = tokenInfo.gameType;
            if (tpId == 0 || gameType == 0)
            {
                getApiResponse.status = "INVALID_SID";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            logName = $"{(Tp)tpId}_debit";

            var authToken = getQuery["authToken"];
            var check = checkAuthorization(authToken);
            if (check.status != "OK")
            {
                getApiResponse.status = check.status;
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n", logName);
                return getApiResponse;
            }

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(tpId, getReq.userId);
            if (tpCheck == null)
            {
                getApiResponse.status = "INVALID_TOKEN_ID";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            else if (tpCheck.Locked)
            {
                getApiResponse.status = "ACCOUNT_LOCKED";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            decimal tpAffterAmount = 0;
            try
            {
                var result = 0;
                
                switch (gameType)
                {
                    case (int)GameType.Netent_RNG:
                        result = _bettingBLL.PlaceBetRNG_Netent(getReq, out tpAffterAmount);
                        break;
                    case (int)GameType.RedTiger_RNG:
                        result = _bettingBLL.PlaceBetRNG_RedTiger(getReq, out tpAffterAmount);
                        break;
                    default:
                        result = _bettingBLL.PlaceBetLC_EVO(getReq, out tpAffterAmount);
                        break;
                }
                
                if (result > 0)
                {
                    if(tpAffterAmount == 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance(tpId, getReq.userId, gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    getApiResponse.status = "OK";
                    getApiResponse.balance = tpAffterAmount;
                    getApiResponse.retransmission = false;
                    getApiResponse.uuid = getReq.uuid;
                    _log2File.Info($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n", logName);
                    return getApiResponse;
                }
                else if (result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(tpId, getReq.userId, gameType);
                    getApiResponse.status = "BET_ALREADY_EXIST";
                    getApiResponse.balance = tpCustBalance.Balance;
                    getApiResponse.retransmission = false;
                    getApiResponse.uuid = getReq.uuid;
                }
                else if (tpAffterAmount < 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(tpId, getReq.userId, gameType);
                    getApiResponse.status = "INSUFFICIENT_FUNDS";
                    getApiResponse.balance = tpCustBalance.Balance;
                    getApiResponse.retransmission = false;
                    getApiResponse.uuid = getReq.uuid;
                }

                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n", logName);
                return getApiResponse;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex.Message}", logName, ex);
                return getApiResponse;
            }
        }

        [HttpPost]
        [Route("[controller]/cancel")]
        public async Task<dynamic> cancelAsync([FromQuery] Dictionary<string, string> getQuery)
        {
            string logName = $"{thisTp}_common_cancel";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            _log2File.Info(Content, logName);

            BettingData_EVO getReq = JsonConvert.DeserializeObject<BettingData_EVO>(Content);
            BetResp getApiResponse = new BetResp();
            getApiResponse.status = "UNKNOWN_ERROR";

            if (string.IsNullOrEmpty(getReq.sid))
            {
                getApiResponse.status = "INVALID_SID";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tokenInfo = getTokenInfo(getReq.sid);
            int tpId = tokenInfo.tpId;
            int gameType = tokenInfo.gameType;
            if (tpId == 0 || gameType == 0)
            {
                getApiResponse.status = "INVALID_SID";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            logName = $"{(Tp)tpId}_cancel";

            var authToken = getQuery["authToken"];
            var check = checkAuthorization(authToken);
            if (check.status != "OK")
            {
                getApiResponse.status = check.status;
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n", logName);
                return getApiResponse;
            }

            decimal tpAffterAmount = 0;
            try
            {
                var result = 0;

                switch (gameType)
                {
                    case (int)GameType.Netent_RNG:
                        result = _settledBLL.CancelBetRNG_Netent(getReq, out tpAffterAmount);
                        break;
                    case (int)GameType.RedTiger_RNG:
                        result = _settledBLL.CancelBetRNG_RedTiger(getReq, out tpAffterAmount);
                        break;
                    default:
                        result = _settledBLL.CancelBetLC_EVO(getReq, out tpAffterAmount);
                        break;
                }


                if (result > 0)
                {
                    if (tpAffterAmount == 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance(tpId, getReq.userId, gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    getApiResponse.status = "OK";
                    getApiResponse.balance = tpAffterAmount;
                    getApiResponse.retransmission = false;
                    getApiResponse.uuid = getReq.uuid;
                    _log2File.Info($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n", logName);
                    return getApiResponse;
                }
                else if (result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(tpId, getReq.userId, gameType);
                    getApiResponse.status = "OK";
                    getApiResponse.balance = tpCustBalance.Balance;
                    getApiResponse.retransmission = false;
                    getApiResponse.uuid = getReq.uuid;
                }

                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n", logName);
                return getApiResponse;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex.Message}", logName, ex);
                return getApiResponse;
            }
        }

        [HttpPost]
        [Route("[controller]/credit")]
        public async Task<dynamic> creditAsync([FromQuery] Dictionary<string, string> getQuery)
        {
            string logName = $"{thisTp}_common_credit";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            _log2File.Info(Content, logName);

            BettingData_EVO getReq = JsonConvert.DeserializeObject<BettingData_EVO>(Content);
            BetResp getApiResponse = new BetResp();
            getApiResponse.status = "UNKNOWN_ERROR";

            //if (_environment.IsDevelopment() && getReq.transaction.amount >= 300)
            //{
            //    Response.StatusCode = (int)Error.ERROR;
            //    //System.Threading.Thread.Sleep(60000);
            //    return getApiResponse;
            //}

            if (string.IsNullOrEmpty(getReq.sid))
            {
                getApiResponse.status = "INVALID_SID";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tokenInfo = getTokenInfo(getReq.sid);
            int tpId = tokenInfo.tpId;
            int gameType = tokenInfo.gameType;
            if (tpId == 0 || gameType == 0)
            {
                getApiResponse.status = "INVALID_SID";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            logName = $"{(Tp)tpId}_credit";

            var authToken = getQuery["authToken"];
            var check = checkAuthorization(authToken);
            if (check.status != "OK")
            {
                getApiResponse.status = check.status;
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n", logName);
                return getApiResponse;
            }

            decimal tpAffterAmount = 0;
            try
            {
                var result = 0;

                switch (gameType)
                {
                    case (int)GameType.Netent_RNG:
                        result = _settledBLL.PayoutRNG_Netent(getReq, out tpAffterAmount);
                        break;
                    case (int)GameType.RedTiger_RNG:
                        result = _settledBLL.PayoutRNG_RedTiger(getReq, out tpAffterAmount);
                        break;
                    default:
                        result = _settledBLL.PayoutLC_EVO(getReq, out tpAffterAmount);
                        break;
                }

                if (result > 0)
                {
                    if (tpAffterAmount == 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance(tpId, getReq.userId, gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    getApiResponse.status = "OK";
                    getApiResponse.balance = tpAffterAmount;
                    getApiResponse.retransmission = false;
                    getApiResponse.uuid = getReq.uuid;
                    _log2File.Info($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n", logName);
                    return getApiResponse;
                }
                else if (result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(tpId, getReq.userId, gameType);
                    getApiResponse.status = "BET_ALREADY_SETTLED";
                    getApiResponse.balance = tpCustBalance.Balance;
                    getApiResponse.retransmission = false;
                    getApiResponse.uuid = getReq.uuid;
                }

                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n", logName);
                return getApiResponse;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex.Message}", logName, ex);
                return getApiResponse;
            }
        }

        [HttpPost]
        [HttpGet]
        [Route("[controller]/{path1?}")]
        public async Task<dynamic> NotFoundAsync()
        {
            string logName = $"{thisTp}_common_NotFound";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            _log2File.Error(Content, logName);
            Response.StatusCode = (int)Error.NotFound;
            return 404;
        }

        private CommonResp checkAuthorization(string authorization)
        {
            CommonResp checkResponse = new CommonResp();

            if (string.IsNullOrEmpty(authorization))
            {
                checkResponse.status = "INVALID_PARAMETER";
                return checkResponse;
            }

            var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (HttpUtility.UrlDecode(authorization) != thirdPartySetting.TpSetting["AuthToken"])
            {
                checkResponse.status = "UNKNOWN_ERROR";
                return checkResponse;
            }

            checkResponse.status = "OK";
            return checkResponse;
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
                gameType = Convert.ToInt32(sessionKeyArr[3]),
                tpId = Convert.ToInt32(sessionKeyArr[4])
            };
        }
    }
}
