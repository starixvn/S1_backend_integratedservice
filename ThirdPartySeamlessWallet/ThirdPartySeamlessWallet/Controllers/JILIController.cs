using System.Text;

using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class JILIController : ControllerBase
    {
        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly ILogger<JILIController> _logger;
        private readonly Log2File _log2File;
        private readonly Tools _tools;
        private readonly Tp _thisTp;
        private readonly IHostEnvironment _environment;

        public JILIController(
            IServiceProvider provider,
            ILogger<JILIController> logger,
            IHostEnvironment environment
        )
        {
            _logger = logger;
            _log2File = provider.GetRequiredService<Log2File>();
            _environment = environment;
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _thisTp = Tp.JILI;
        }

        public class TokenInfo
        {
            public string shortTimeToken { get; set; }
            public string longTimeToken { get; set; }
            public string tpUserName { get; set; }
            public string gameId { get; set; }
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
            //public long txId { get; set; }
            public string token { get; set; }
        }

        #endregion

        [HttpPost]
        [Route("[controller]/auth")]
        [Route("[controller]/goal123/auth")]
        [Route("[controller]/bwing/auth")]
        [Route("[controller]/nova_stream/auth")]
        public async Task<dynamic> authAsync()
        {
            string logName = $"{_thisTp}_auth";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            AuthReq getReq = JsonConvert.DeserializeObject<AuthReq>(Content);
            ErrorResp getErrorResponse = new ErrorResp();
            getErrorResponse.errorCode = 5;
            getErrorResponse.message = "Error";

            // check authorization
            Request.Headers.TryGetValue("Authorization", out var authorization);
            string sAuthorization = authorization.ToString();

            #region GetToken

            var tokenInfo = getTokenInfo(getReq.token);
            var verify = _authBLL.Authenticate(tokenInfo.shortTimeToken, out TpCustInfoAuth tpCustInfoAuth);
            if (!verify)
            {
                getErrorResponse.errorCode = 4;
                getErrorResponse.message = "Token expired";
                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                return getErrorResponse;
            }

            #endregion

            var check = checkAuthorization(tpCustInfoAuth?.MemberType == 3, sAuthorization);
            if (check.errorCode != 0)
            {
                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(check)}", logName);
                return check;
            }

            try
            {
                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, tokenInfo.tpUserName);
                if (tpCheck == null)
                {
                    getErrorResponse.errorCode = 4;
                    getErrorResponse.message = "Token expired.";
                    _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }
                else if (tpCheck.Locked)
                {
                    getErrorResponse.errorCode = 5;
                    getErrorResponse.message = "Account is Locked";
                    _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }

                var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, tokenInfo.gameId);

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, tpCustInfoAuth.TpUserName, (int)gameType);
                if (tpCustBalance != null)
                {
                    AuthResp getApiResponse = new AuthResp();
                    getApiResponse.errorCode = 0;
                    getApiResponse.message = "success";
                    getApiResponse.username = tokenInfo.tpUserName;
                    getApiResponse.currency = tpCustBalance.TpCurrencyId;
                    getApiResponse.balance = tpCustBalance.Balance;
                    getApiResponse.token = getReq.token;
                    _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

            }
            catch (Exception ex)
            {
                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}\r\n{ex}", logName, ex);
                return getErrorResponse;
            }

            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
            return getErrorResponse;


        }

        [HttpPost]
        [Route("[controller]/bet")]
        [Route("[controller]/goal123/bet")]
        [Route("[controller]/bwing/bet")]
        [Route("[controller]/nova_stream/bet")]
        public async Task<dynamic> betAsync([FromQuery] Dictionary<string, string> getQuery)
        {
            string logName = $"{_thisTp}_bet";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            BettingData_JILI getReq = JsonConvert.DeserializeObject<BettingData_JILI>(Content);
            ErrorResp getErrorResponse = new ErrorResp();
            getErrorResponse.errorCode = 5;
            getErrorResponse.message = "Error";

            // check authorization
            Request.Headers.TryGetValue("Authorization", out var authorization);
            string sAuthorization = authorization.ToString();

            var custInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, getReq.userId);
            if (custInfo == null)
            {
                getErrorResponse.errorCode = 3;
                getErrorResponse.message = "userId Not Exist.";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                return getErrorResponse;
            }

            var check = checkAuthorization(custInfo.MemberType == 3, sAuthorization);
            if (check.errorCode != 0)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(check)}", logName);
                return check;
            }
            try
            {
                var tokenInfo = getTokenInfo(getReq.token);
                // offline payment no need to check online
                var logToken = "";
                if (!getReq.isFreeRound)
                {
                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, getReq.userId);
                    if (tpCheck == null)
                    {
                        getErrorResponse.errorCode = 4;
                        getErrorResponse.message = "Token expired.";
                        _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                        return getErrorResponse;
                    }
                    else if (tpCheck.Token != tokenInfo.longTimeToken)
                    {
                        getErrorResponse.errorCode = 4;
                        getErrorResponse.message = "Wrong token";
                        _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}\r\n{JsonConvert.SerializeObject(tokenInfo)}\r\n{JsonConvert.SerializeObject(tpCheck)}", logName);
                        return getErrorResponse;
                    }
                    else if (tpCheck.Locked)
                    {
                        getErrorResponse.errorCode = 5;
                        getErrorResponse.message = "Account is Locked";
                        _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                        return getErrorResponse;
                    }

                    logToken = $"\r\n{JsonConvert.SerializeObject(tokenInfo)}\r\n{JsonConvert.SerializeObject(tpCheck)}";
                }

                if (_environment.IsDevelopment() && getReq.betAmount == 90)
                {
                    Thread.Sleep(3000);
                    _log2File.Error($"TEST CANCEL\r\n{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }

                var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, getReq.game.ToString());

                var result = _settledBLL.BetAndPayoutRNG_JILI(getReq, (int)gameType, out decimal tpAffterAmount);

                if (_environment.IsDevelopment() && getReq.betAmount == 210)
                {
                    Thread.Sleep(3000);
                    _log2File.Error($"TEST CANCEL\r\n{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getReq.userId, (int)gameType);
                if (result > 0)
                {
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.userId, (int)gameType).Balance;
                    }
                    BetResp getApiResponse = new BetResp();
                    getApiResponse.errorCode = 0;
                    getApiResponse.message = "success";
                    getApiResponse.username = getReq.userId;
                    getApiResponse.currency = tpCustBalance.TpCurrencyId;
                    getApiResponse.balance = tpAffterAmount;
                    getApiResponse.token = getReq.token;
                    _log2File.Info($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}{logToken}", logName);
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
                    getApiResponse.token = getReq.token;
                    _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}{logToken}", logName);
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
                    getApiResponse.token = getReq.token;
                    _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}{logToken}", logName);
                    return getApiResponse;
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName, ex);
                return getErrorResponse;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
            return getErrorResponse;
        }

        [HttpPost]
        [Route("[controller]/cancelBet")]
        [Route("[controller]/cancelSessionBet")]
        [Route("[controller]/goal123/cancelBet")]
        [Route("[controller]/goal123/cancelSessionBet")]
        [Route("[controller]/bwing/cancelBet")]
        [Route("[controller]/bwing/cancelSessionBet")]
        [Route("[controller]/nova_stream/cancelBet")]
        [Route("[controller]/nova_stream/cancelSessionBet")]
        public async Task<dynamic> cancelBetAsync([FromQuery] Dictionary<string, string> getQuery)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            CancelBetData_JILI getReq = JsonConvert.DeserializeObject<CancelBetData_JILI>(Content);
            string logName = (getReq.sessionId > 0) ? $"{_thisTp}_cancelSessionBet" : $"{_thisTp}_cancelBet";

            ErrorResp getErrorResponse = new ErrorResp();
            getErrorResponse.errorCode = 5;
            getErrorResponse.message = "Error";

            // check authorization
            Request.Headers.TryGetValue("Authorization", out var authorization);
            string sAuthorization = authorization.ToString();

            var custInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, getReq.userId);
            if (custInfo == null)
            {
                getErrorResponse.errorCode = 3;
                getErrorResponse.message = "userId Not Exist.";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                return getErrorResponse;
            }

            var check = checkAuthorization(custInfo.MemberType == 3, sAuthorization);
            if (check.errorCode != 0)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(check)}", logName);
                return check;
            }

            var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, getReq.game.ToString());

            try
            {
                var result = _settledBLL.CancelBetRNG_JILI(getReq, (int)gameType, out decimal tpAffterAmount);

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getReq.userId, (int)gameType);
                if (result > 0)
                {
                    BetResp getApiResponse = new BetResp();
                    getApiResponse.errorCode = 0;
                    getApiResponse.message = "success";
                    getApiResponse.username = getReq.userId;
                    getApiResponse.currency = tpCustBalance.TpCurrencyId;
                    getApiResponse.balance = tpAffterAmount;
                    _log2File.Info($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
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
                    _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (result == -2)
                {
                    getErrorResponse.errorCode = 2;
                    getErrorResponse.message = "Round not found";
                    _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}\r\n{ex}", logName, ex);
                return getErrorResponse;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
            return getErrorResponse;
        }

        [HttpPost]
        [Route("[controller]/sessionBet")]
        [Route("[controller]/goal123/sessionBet")]
        [Route("[controller]/bwing/sessionBet")]
        [Route("[controller]/nova_stream/sessionBet")]
        public async Task<dynamic> sessionBetAsync([FromQuery] Dictionary<string, string> getQuery)
        {
            string logName = $"{_thisTp}_sessionBet";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            BettingData_JILI getReq = JsonConvert.DeserializeObject<BettingData_JILI>(Content);
            ErrorResp getErrorResponse = new ErrorResp();
            getErrorResponse.errorCode = 5;
            getErrorResponse.message = "Error";

            // check authorization
            Request.Headers.TryGetValue("Authorization", out var authorization);
            string sAuthorization = authorization.ToString();

            var custInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, getReq.userId);
            if (custInfo == null)
            {
                getErrorResponse.errorCode = 3;
                getErrorResponse.message = "userId Not Exist.";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                return getErrorResponse;
            }

            var check = checkAuthorization(custInfo.MemberType == 3, sAuthorization);
            if (check.errorCode != 0)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(check)}", logName);
                return check;
            }
            if (getReq.type != 1 && getReq.type != 2)
            {
                getErrorResponse.errorCode = 3;
                getErrorResponse.message = "Invalid parameter (type)";
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                return getErrorResponse;
            }

            try
            {
                var logToken = "";
                var tokenInfo = getTokenInfo(getReq.token);
                var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, getReq.game.ToString());
                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getReq.userId, (int)gameType);
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, getReq.userId);

                if (getReq.type == 1)
                {
                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, getReq.userId);
                    if (tpCheck == null)
                    {
                        getErrorResponse.errorCode = 4;
                        getErrorResponse.message = "Token expired.";
                        _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                        return getErrorResponse;
                    }
                    else if (tpCheck.Token != tokenInfo.longTimeToken)
                    {
                        getErrorResponse.errorCode = 4;
                        getErrorResponse.message = "Wrong token";
                        _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}\r\n{JsonConvert.SerializeObject(tokenInfo)}\r\n{JsonConvert.SerializeObject(tpCheck)}", logName);
                        return getErrorResponse;
                    }
                    else if (tpCheck.Locked)
                    {
                        getErrorResponse.errorCode = 5;
                        getErrorResponse.message = "Account is Locked";
                        _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                        return getErrorResponse;
                    }
                    logToken = $"\r\n{JsonConvert.SerializeObject(tokenInfo)}\r\n{JsonConvert.SerializeObject(tpCheck)}";
                }

                decimal tpAffterAmount = 0;
                var result = (getReq.type == 1) ? _bettingBLL.PlaceBetRNG_JILI(tpCustInfo, getReq, out tpAffterAmount) : _settledBLL.PayoutRNG_JILI(tpCustInfo, getReq, out tpAffterAmount);
                if (result > 0)
                {
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.userId, (int)gameType).Balance;
                    }
                    BetResp getApiResponse = new BetResp();
                    getApiResponse.errorCode = 0;
                    getApiResponse.message = "success";
                    getApiResponse.username = getReq.userId;
                    getApiResponse.currency = tpCustBalance.TpCurrencyId;
                    getApiResponse.balance = tpAffterAmount;
                    getApiResponse.token = getReq.token;
                    _log2File.Info($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}{logToken}", logName);
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
                    getApiResponse.token = getReq.token;
                    _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}{logToken}", logName);
                    return getApiResponse;
                }
                else if (tpAffterAmount < 0)
                {
                    BetResp getApiResponse = new BetResp();
                    getApiResponse.errorCode = 2;
                    getApiResponse.message = "Not enough balance";
                    getApiResponse.username = getReq.userId;
                    getApiResponse.currency = tpCustBalance.TpCurrencyId;
                    getApiResponse.balance = tpAffterAmount;
                    getApiResponse.token = getReq.token;
                    _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}{logToken}", logName);
                    return getApiResponse;
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName, ex);
                return getErrorResponse;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(getQuery)}\r\n{Content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
            return getErrorResponse;
        }

        private ErrorResp checkAuthorization(bool isLiveStreaming, string authorization = null)
        {
            ErrorResp getErrorResponse = new ErrorResp();
            byte[] authTmp = null;

            if (string.IsNullOrEmpty(authorization))
            {
                getErrorResponse.errorCode = 5;
                getErrorResponse.message = "authorization is null";
                return getErrorResponse;
            }
            // all site use same username and password
            var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)_thisTp);

            if (isLiveStreaming)
                authTmp = Encoding.ASCII.GetBytes($"{thirdPartySetting.TpSetting["Username_Stream"]}:{thirdPartySetting.TpSetting["Password_Stream"]}");
            else
                authTmp = Encoding.ASCII.GetBytes($"{thirdPartySetting.TpSetting["Username"]}:{thirdPartySetting.TpSetting["Password"]}");

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
                gameId = sessionKeyArr[3]
            };
        }
    }
}
