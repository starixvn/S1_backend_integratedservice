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
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]

    public class EVOController : ControllerBase
    {
        #region Fields

        private readonly ILogger<EVOController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly TpRequestFailBLL _tpRequestFailBll;
        private readonly Log2File _log2File;
        private readonly Tools _tools;
        private readonly Tp thisTp;
        private readonly IEVOBLL _EVOBLL;
        public string[] promoTransactionTypeList { get; set; }
        #endregion

        #region Constructors

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
            _tpRequestFailBll = provider.GetRequiredService<TpRequestFailBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _EVOBLL = provider.GetService<IEVOBLL>();
            thisTp = Tp.EVO;

            promoTransactionTypeList = new string[] { "JackpotWin",
                "FreeRoundPlayableSpent",
                "RewardGamePlayableSpent", "RewardGameWinCapReached", "RewardGameMinBetLimitReached",
                "FromGame", "RtrMonetaryReward" };
        }

        #endregion

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

        public class SidReq : AuthReq
        {
            public string gameProvider { get; set; }
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

        public class SidResp : CommonResp
        {
            public string sid { get; set; }
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

        /// <summary>
        /// 測試API有通
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/ping")]
        public string PingTest() => "Ping Ok.";


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

        [HttpPost]
        [Route("[controller]/sid")]
        public async Task<dynamic> sidAsync([FromQuery] Dictionary<string, string> getQuery)
        {
            string logName = $"{thisTp}_sid";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            _log2File.Info(Content, logName);

            SidReq getReq = JsonConvert.DeserializeObject<SidReq>(Content);
            SidResp getApiResponse = new SidResp();
            getApiResponse.status = "UNKNOWN_ERROR";

            var authToken = getQuery["authToken"];
            var check = checkAuthorization(authToken);
            if (check.status != "OK")
            {
                getApiResponse.status = check.status;
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n", logName);
                return getApiResponse;
            }
            if (/*string.IsNullOrEmpty(getReq.gameProvider) ||*/ string.IsNullOrEmpty(getReq.userId))
            {
                getApiResponse.status = "INVALID_PARAMETER";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            try
            {
                var tpId = (int)Tp.EVO;
                var gameType = (int)GameType.EVO_LiveCasino;
                if (getReq.gameProvider != null && string.Equals(getReq.gameProvider, "netent", StringComparison.OrdinalIgnoreCase))
                {
                    tpId = (int)Tp.Netent;
                    gameType = (int)GameType.Netent_RNG;
                }
                else if (getReq.gameProvider != null && string.Equals(getReq.gameProvider, "redtiger", StringComparison.OrdinalIgnoreCase))
                {
                    tpId = (int)Tp.RedTiger;
                    gameType = (int)GameType.RedTiger_RNG;
                }

                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, getReq.userId);
                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getReq.userId);
                if (tpCheck == null)
                {
                    string token = null;
                    string clientIp = "";
                    tpCustInfo = _tpCustInfoBLL.GetTpCustInfo((int)thisTp, getReq.userId);
                    _tpCustInfoBLL.AddLoginRecord(tpCustInfo.CustId, tpCustInfo.SiteId, clientIp, out token);
                    _tpCustInfoBLL.AddUserLoginLog(tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, clientIp, "Web", "Web");
                    tpCheck = _thirdPartyBLL.ThirdPartyCheck((int)thisTp, getReq.userId);
                }
                if (tpCheck == null || tpCheck.Locked)
                {
                    getApiResponse.status = "ACCOUNT_LOCKED";
                    _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var key = _authBLL.ApplyToken(tpCustInfo);
                var sessionKeyArr = new string[] { key, tpCheck.Token, tpCustInfo.TpUserName, gameType.ToString(), tpId.ToString() };
                byte[] bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sessionKeyArr));
                string sessionKey = Convert.ToBase64String(bytes);

                getApiResponse.status = "OK";
                getApiResponse.sid = sessionKey;
                _log2File.Info($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex.Message}", logName, ex);
                return getApiResponse;
            }
        }

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

            if (string.IsNullOrEmpty(getReq.sid) || string.IsNullOrEmpty(getReq.userId))
            {
                getApiResponse.status = "INVALID_PARAMETER";
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
            if (tokenInfo.tpUserName != getReq.userId)
            {
                getApiResponse.status = "INVALID_PARAMETER";
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
            if (!verify)
            {
                getApiResponse.status = "INVALID_TOKEN_ID";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
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

            if (string.IsNullOrEmpty(getReq.sid) || string.IsNullOrEmpty(getReq.userId))
            {
                getApiResponse.status = "INVALID_PARAMETER";
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
            if (tokenInfo.tpUserName != getReq.userId)
            {
                getApiResponse.status = "INVALID_PARAMETER";
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

            var tpCustBalance = _tpCustInfoBLL.GetBalance(tpId, getReq.userId, gameType);
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

            if (string.IsNullOrEmpty(getReq.sid) || string.IsNullOrEmpty(getReq.userId))
            {
                getApiResponse.status = "INVALID_PARAMETER";
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
            if (!verifyGameId(gameType, getReq.game.details.table.id))
            {
                getApiResponse.status = "INVALID_TOKEN_ID";
                _log2File.Error($"error gametype\r\n{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            if (tokenInfo.tpUserName != getReq.userId)
            {
                getApiResponse.status = "INVALID_PARAMETER";
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

                result = gameType switch
                {
                    (int)GameType.Netent_RNG => _bettingBLL.PlaceBetRNG_Netent(getReq, out tpAffterAmount),
                    (int)GameType.RedTiger_RNG => _bettingBLL.PlaceBetRNG_RedTiger(getReq, out tpAffterAmount),
                    _ => _bettingBLL.PlaceBetLC_EVO(getReq, out tpAffterAmount),
                };
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

            if (string.IsNullOrEmpty(getReq.sid) || string.IsNullOrEmpty(getReq.userId))
            {
                getApiResponse.status = "INVALID_PARAMETER";
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
            if (tokenInfo.tpUserName != getReq.userId)
            {
                getApiResponse.status = "INVALID_PARAMETER";
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

                result = gameType switch
                {
                    (int)GameType.Netent_RNG => _settledBLL.CancelBetRNG_Netent(getReq, out tpAffterAmount),
                    (int)GameType.RedTiger_RNG => _settledBLL.CancelBetRNG_RedTiger(getReq, out tpAffterAmount),
                    _ => _settledBLL.CancelBetLC_EVO(getReq, out tpAffterAmount),
                };
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
                else if (result == -2)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(tpId, getReq.userId, gameType);
                    getApiResponse.status = "BET_DOES_NOT_EXIST";
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
                _tpRequestFailBll.SetRequestFail(RequestFailureType.Tp, Tp.EVO, TpRequestFailureCategory.Cancel, JsonConvert.SerializeObject(getReq), ex.Message);
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

            if (string.IsNullOrEmpty(getReq.sid) || string.IsNullOrEmpty(getReq.userId))
            {
                getApiResponse.status = "INVALID_PARAMETER";
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
            if (tokenInfo.tpUserName != getReq.userId)
            {
                getApiResponse.status = "INVALID_PARAMETER";
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

                result = gameType switch
                {
                    (int)GameType.Netent_RNG => _settledBLL.PayoutRNG_Netent(getReq, out tpAffterAmount),
                    (int)GameType.RedTiger_RNG => _settledBLL.PayoutRNG_RedTiger(getReq, out tpAffterAmount),
                    _ => _settledBLL.PayoutLC_EVO(getReq, out tpAffterAmount),
                };

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
                else if (result == -2)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(tpId, getReq.userId, gameType);
                    getApiResponse.status = "BET_DOES_NOT_EXIST";
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
                _tpRequestFailBll.SetRequestFail(RequestFailureType.Tp, Tp.EVO, TpRequestFailureCategory.Credit, JsonConvert.SerializeObject(getReq), ex.Message);
                return getApiResponse;
            }
        }

        [HttpPost]
        [Route("[controller]/promo_payout")]
        public async Task<StandardResponse> promoPayoutAsync([FromQuery] Dictionary<string, string> getQuery)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            PromoData_EVO request = JsonConvert.DeserializeObject<PromoData_EVO>(Content);
            var authToken = getQuery["authToken"];

            if (request == null || string.IsNullOrWhiteSpace(authToken))
            {
                _logger.LogError($"Request or AuthToken is Empty.Request.Body={JsonConvert.SerializeObject(request)}.AuthToken={authToken}.");
                return new StandardResponse()
                {
                    status = EVOStatus.INVALID_PARAMETER.ToString(),
                    retransmission = false
                };
            }

            if (!CheckRequest(request, out EVOStatus status))
            {
                return new StandardResponse()
                {
                    status = status.ToString(),
                    uuid = request.uuid,
                    retransmission = false
                };
            }

            var tokenInfo = getTokenInfo(request.sid);
            int tpId = tokenInfo.tpId;
            int gameType = tokenInfo.gameType;
            if (tpId == 0 || gameType == 0)
            {
                _logger.LogError($"CheckSidToken Fail: sid.tpId or sid.gametype is Invalid. sid.tp={tpId}. sid.gametype={gameType}.");
                return new StandardResponse()
                {
                    status = EVOStatus.INVALID_SID.ToString(),
                    uuid = request.uuid,
                    retransmission = false
                };
            }
            if (tokenInfo.tpUserName != request.userId)
            {
                _logger.LogError($"CheckSidToken Fail: sid.tpUserName is equal request.userId.");
                return new StandardResponse()
                {
                    status = EVOStatus.INVALID_PARAMETER.ToString(),
                    uuid = request.uuid,
                    retransmission = false
                };
            }

            var check = checkAuthorization(authToken);
            if (check.status != "OK")
            {
                _logger.LogError($"CheckAuthToken Fail.");
                var result = Enum.TryParse(check.status, out EVOStatus enumStatus);
                return new StandardResponse()
                {
                    status = enumStatus.ToString(),
                    uuid = request.uuid,
                    retransmission = false
                };
            }

            return await _EVOBLL.PromoPayout(request, tokenInfo.tpId, tokenInfo.gameType);
        }

        [HttpPost]
        [Route("[controller]/promo_payoutv1")]
        public async Task<dynamic> promoPayoutv1Async([FromQuery] Dictionary<string, string> getQuery)
        {
            string logName = $"{thisTp}_common_promo_payout";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            _log2File.Info(Content, logName);

            PromoData_EVO getReq = JsonConvert.DeserializeObject<PromoData_EVO>(Content);
            BetResp getApiResponse = new BetResp();
            getApiResponse.status = "UNKNOWN_ERROR";

            if (string.IsNullOrEmpty(getReq.sid))
            {
                getApiResponse.status = "INVALID_SID";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            if (string.IsNullOrEmpty(getReq.userId))
            {
                getApiResponse.status = "INVALID_PARAMETER";
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
            if (tokenInfo.tpUserName != getReq.userId)
            {
                getApiResponse.status = "INVALID_PARAMETER";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            logName = $"{(Tp)tpId}_promo_payout";

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

                result = gameType switch
                {
                    (int)GameType.Netent_RNG => _settledBLL.PromoPayoutRNG_Netent(getReq, out tpAffterAmount),
                    (int)GameType.RedTiger_RNG => _settledBLL.PromoPayoutRNG_RedTiger(getReq, out tpAffterAmount),
                    _ => _settledBLL.PromoPayoutLC_EVO(getReq, out tpAffterAmount),
                };

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
                _tpRequestFailBll.SetRequestFail(RequestFailureType.Tp, Tp.EVO, TpRequestFailureCategory.PromoPayout, JsonConvert.SerializeObject(getReq), ex.Message);
                return getApiResponse;
            }
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

        private bool verifyGameId(int gameType, string gameId)
        {
            var getGameType = _thirdPartyBLL.GetGameType((int)Tp.EVO, gameId);
            if (gameType == (int)getGameType)
            {
                return true;
            }
            return false;
        }

        private bool CheckRequest(PromoData_EVO request, out EVOStatus status)
        {
            status = EVOStatus.OK;

            if (request == null)
            {
                _logger.LogError($"CheckRequest Fail: request is null.");
                status = EVOStatus.INVALID_PARAMETER;
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.sid))
            {
                _logger.LogError($"CheckRequest Fail: request.sid is Invalid.");
                status = EVOStatus.INVALID_SID;
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.userId))
            {
                _logger.LogError($"CheckRequest Fail: request.userId is Invalid.");
                status = EVOStatus.INVALID_PARAMETER;
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.currency))
            {
                _logger.LogError($"CheckRequest Fail: request.currency is Invalid.");
                status = EVOStatus.INVALID_PARAMETER;
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.uuid))
            {
                _logger.LogError($"CheckRequest Fail: request.uuid is Invalid.");
                status = EVOStatus.INVALID_PARAMETER;
                return false;
            }

            if (request.promoTransaction == null)
            {
                _logger.LogError($"CheckRequest Fail: request.promoTransaction is Invalid.");
                status = EVOStatus.INVALID_PARAMETER;
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.promoTransaction.id))
            {
                _logger.LogError($"CheckRequest Fail: request.promoTransaction.id is Invalid.");
                status = EVOStatus.INVALID_PARAMETER;
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.promoTransaction.type))
            {
                _logger.LogError($"CheckRequest Fail: request.promoTransaction.type is Invalid.");
                status = EVOStatus.INVALID_PARAMETER;
                return false;
            }

            if (!promoTransactionTypeList.Contains(request.promoTransaction.type))
            {
                _logger.LogError($"CheckRequest Fail: request.promoTransaction.type can't found.");
                status = EVOStatus.INVALID_PARAMETER;
                return false;
            }

            return true;
        }

    }
}
