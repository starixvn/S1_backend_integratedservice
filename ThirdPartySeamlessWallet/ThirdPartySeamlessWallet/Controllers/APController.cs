using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Web;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class APController : ControllerBase
    {

        #region Fields

        private readonly AuthBLL authBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Tp thisTp;
        private readonly int gameType = 0;
        private readonly Tools tools;
        private readonly ILogger<APController> logger;
        private readonly IHostEnvironment environment;
        private readonly Log2File log2File;
        private readonly string _tpName = Tp.AP.ToString();
        private readonly List<string> _gameOfMiniList = new List<string> { "crash", "cardracing" };

        #endregion

        #region Constructors
        public APController(IServiceProvider provider)
        {
            authBLL = provider.GetRequiredService<AuthBLL>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingBLL = provider.GetRequiredService<BettingBLL>();
            settledBLL = provider.GetRequiredService<SettledBLL>();
            tools = provider.GetRequiredService<Tools>();
            logger = provider.GetRequiredService<ILogger<APController>>();
            thisTp = Tp.AP;
            gameType = (int)GameType.AP_RNG;
            environment = provider.GetRequiredService<IHostEnvironment>();
            log2File = provider.GetRequiredService<Log2File>();
        }
        #endregion

        #region model

        #region Resp
        public class Resp
        {
            public string Timestamp { get; set; } = "";
            public string Seq { get; set; } = "";
            public int ErrorCode { get; set; } = 0;
            public string ErrorDescription { get; set; } = "";
        }
        public class GetPlayerInfoResp : Resp
        {
            public string SiteCode { get; set; } = "";
            public string OPToken { get; set; } = "";
            public string GameCode { get; set; } = "";
            public string PlayerId { get; set; } = "";
            public string PlayerName { get; set; } = "";
            public string PlayerCountry { get; set; } = "";
            public string Currency { get; set; } = "";
            public decimal Balance { get; set; } = 0;
            public int TestAccount { get; set; }
        }
        public class PlaceBetResp : Resp
        {
            public string OPTransId { get; set; } = "";
            public decimal Balance { get; set; } = 0;
        }
        public class RefundBetResp : Resp
        {
            public string GameCode { get; set; } = "";
            public string GameRoundId { get; set; } = "";
            public decimal Balance { get; set; } = 0;
        }
        public class SettleResp : Resp
        {
            public decimal Balance { get; set; } = 0;
        }
        public class JPSettleResp : Resp
        {
            public decimal Balance { get; set; } = 0;
        }
        public class CPSettleResp : Resp
        {
            public decimal Balance { get; set; } = 0;
        }
        public class GetBalanceResp : Resp
        {
            public decimal Balance { get; set; } = 0;
        }
        public class CommonMiniResp
        {
            public decimal balance { get; set; }
            public long timestamp { get; set; }
        }
        public class ErrorResponse
        {
            public string errorMessage { get; set; }
        }
        public class PlaceBetMiniResp : CommonMiniResp
        {
            public string transactionReferenceId { get; set; }
        }

        public class ResultMiniResp
        {
            public string processId { get; set; }
            public long timestamp { get; set; }
        }

        public class RefundMiniResp
        {
            public string refundId { get; set; }
            public long timestamp { get; set; }
        }
        #endregion

        #region Req
        public class Req
        {
            public string Timestamp { get; set; }
            public string Seq { get; set; }
            public string SiteCode { get; set; }
        }
        public class GetPlayerInfoReq : Req
        {
            public string GameCode { get; set; }
            public string OPToken { get; set; }
        }
        public class PlaceBetReq : Req
        {
            public string GameCode { get; set; }
            public string GameRoundId { get; set; }
            public decimal Stake { get; set; }
            public string BetTime { get; set; }
            public string Ip { get; set; }
            public string OPToken { get; set; }
            public string PlayerId { get; set; }
        }
        public class RefundBetReq : Req
        {
            public string GameCode { get; set; }
            public string GameRoundId { get; set; }
            public string PlayerId { get; set; }
            public decimal TotalStake { get; set; }
        }
        public class SettleReq : Req
        {
            public string GameCode { get; set; }
            public string GameRoundId { get; set; }
            public string PlayerId { get; set; }
            public decimal TotalStake { get; set; }
            public decimal TotalWin { get; set; }
            public string SettleTime { get; set; }
            public int HitJackpot { get; set; }
        }
        public class JPSettleReq : Req
        {
            public string GameRoundId { get; set; }
            public string PlayerId { get; set; }
            public decimal JPWin { get; set; }
            public string JPSettleTime { get; set; }
        }
        public class GetBalanceReq : Req
        {
            public string OPToken { get; set; }
            public string PlayerId { get; set; }
        }
        public class CPSettleReq : Req
        {
            public string PlayerId { get; set; }
            public string CPId { get; set; }
            public string CPTransId { get; set; }
            public decimal CPWin { get; set; }
            public string BrandCode { get; set; } = "";
            public string CPType { get; set; } = "";
        }
        public class GetBalanceMiniReq
        {
            public string UserReferenceId { get; set; }
        }
        public class CommonMiniReq
        {
            public string payload { get; set; }
            public string signature { get; set; }
            public long timestamp { get; set; }
        }

        public class SplitTransactionReferenceId
        {
            public string GameId { get; set; }
            public string SessionId { get; set; }
        }
        #endregion

        #endregion

        #region enum
        public enum ErrorCode
        {
            Success = 0,
            UnspecifiedError = 5000,
            GameNotFound = 5011,
            UnderMaintenance = 5050,
            APIError = 5100,
            HashInvalid = 5101,
            MandatoryParamMissing = 5102,
            ParameterIncorrect = 5103,
            TokenInvalid = 5112,
            TokenExpired = 5113,
            DuplicateRequest = 5121,
            IpRestricted = 5201,
            AccountNotExist = 5212,
            AccountLocked = 5213,
            AccountIsBlacklisted = 5214,
            PlayerNoPermitted = 5311,
            StakeIllegal = 5312,
            PlayerHasInsufficientFunds = 5321,
        }

        public enum HttpStatusCode
        {
            OK = 200,
            INVALID_DATA = 400,
            UAUTHORIZED = 401,
            INVALID_SESSION = 404,
            UNPROCESSABLE = 422,
            INSUFFICIENT_FUNDS = 601,
        }

        #endregion

        #region Slot

        [HttpPost]
        [Route("[controller]/GetPlayerInfo")]
        public async Task<GetPlayerInfoResp> GetPlayerInfoAsync()
        {
            #region Initial
            GetPlayerInfoResp result = new GetPlayerInfoResp();
            GetPlayerInfoReq getReq = new GetPlayerInfoReq();
            #endregion
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            try
            {
                string authToken = string.Empty;
                string sessionToken = string.Empty;
                getReq = JsonConvert.DeserializeObject<GetPlayerInfoReq>(Content);
                result.Timestamp = getReq.Timestamp;
                result.Seq = getReq.Seq;
                result.TestAccount = environment.IsDevelopment() ? 1 : 0;

                #region Check Hash
                Request.Headers.TryGetValue("ap-hash", out var ApHash);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
                string Hash = tools.ToMD5($"{SecretKey}{Content}").ToLower();
                if (ApHash != Hash)
                {
                    result.ErrorCode = (int)ErrorCode.HashInvalid;
                    result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n ApHash:{ApHash}\r\n Hash:{Hash}");
                    return result;
                }
                #endregion

                try
                {
                    string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(getReq.OPToken)));
                    var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                    authToken = sessionKeyArr[0];
                    sessionToken = sessionKeyArr[1];
                }
                catch (Exception ex)
                {
                    result.ErrorCode = (int)ErrorCode.TokenInvalid;
                    result.ErrorDescription = ErrorCode.TokenInvalid.ToString();
                    logger.LogError(ex, $"{Content}\r\n{JsonConvert.SerializeObject(result)}");
                    return result;
                }

                if (!authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                {
                    result.ErrorCode = (int)ErrorCode.TokenInvalid;
                    result.ErrorDescription = ErrorCode.TokenInvalid.ToString();
                    logger.LogError($"{Content}");
                    return result;
                }
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
                if (tpCheck == null)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n authToken:{authToken}");
                    return result;
                }
                else if (tpCheck.Token != sessionToken)
                {
                    result.ErrorCode = (int)ErrorCode.TokenInvalid;
                    result.ErrorDescription = ErrorCode.TokenInvalid.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n Token:{tpCheck.Token}\r\n sessionToken:{sessionToken}");
                    return result;
                }
                else if (tpCheck.Locked)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n Locked");
                    return result;
                }
                else
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpCustInfoAuth.TpUserName, gameType);
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.OPToken = getReq.OPToken;
                    result.SiteCode = getReq.SiteCode;
                    result.PlayerId = tpCustInfoAuth.TpUserName;
                    result.PlayerName = tpCustInfoAuth.TpUserName;
                    result.Currency = tpCustInfoAuth.TpCurrencyId;
                    result.Balance = tpCustBalance.Balance;
                    logger.LogInformation($"{Content}\r\n{JsonConvert.SerializeObject(result)}");
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                logger.LogError(ex, $"{Content}\r\n{JsonConvert.SerializeObject(result)}");
            }
            return result;
        }

        [HttpPost]
        [Route("[controller]/PlaceBet")]
        public async Task<PlaceBetResp> PlaceBetAsync()
        {
            #region Initial
            PlaceBetResp result = new PlaceBetResp();
            PlaceBetReq getReq = new PlaceBetReq();
            #endregion
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            try
            {

                getReq = JsonConvert.DeserializeObject<PlaceBetReq>(Content);
                string authToken = string.Empty;
                string sessionToken = string.Empty;
                string TpUserName = string.Empty;
                result.Timestamp = getReq.Timestamp;
                result.Seq = getReq.Seq;

                #region Check Hash
                Request.Headers.TryGetValue("ap-hash", out var ApHash);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
                string Hash = tools.ToMD5($"{SecretKey}{Content}").ToLower();
                if (ApHash != Hash)
                {
                    result.ErrorCode = (int)ErrorCode.HashInvalid;
                    result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n{ApHash}\r\n{Hash}");
                    return result;
                }
                #endregion

                try
                {
                    string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(getReq.OPToken)));
                    var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                    authToken = sessionKeyArr[0];
                    sessionToken = sessionKeyArr[1];
                    TpUserName = sessionKeyArr[2];
                }
                catch (Exception ex)
                {
                    result.ErrorCode = (int)ErrorCode.TokenInvalid;
                    result.ErrorDescription = ErrorCode.TokenInvalid.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n{ex.ToString()}");
                }

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, TpUserName);
                if (tpCheck == null)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n authToken:{authToken}");
                    return result;
                }
                else if (tpCheck.Token != sessionToken)
                {
                    result.ErrorCode = (int)ErrorCode.TokenInvalid;
                    result.ErrorDescription = ErrorCode.TokenInvalid.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n Token:{tpCheck.Token}\r\n sessionToken:{sessionToken}");
                    return result;
                }
                else if (tpCheck.Locked)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n Locked");
                    return result;
                }
                else if (getReq.Stake < 0)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, TpUserName, gameType);
                    result.Balance = tpCustBalance.Balance;
                    result.ErrorCode = (int)ErrorCode.ParameterIncorrect;
                    result.ErrorDescription = ErrorCode.ParameterIncorrect.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n Stake < 0");
                    return result;
                }
                else
                {
                    #region CancelBet測試用
                    //if (environment.IsDevelopment() && getReq.Stake >= 6000)
                    //{
                    //    System.Threading.Thread.Sleep(60000);
                    //    result.ErrorCode = (int)ErrorCode.APIError;
                    //    result.ErrorDescription = ErrorCode.APIError.ToString();
                    //    logger.LogError($"CancelBet Test\r\n{Content}\r\n{JsonConvert.SerializeObject(result)}");
                    //    return result;
                    //}
                    #endregion

                    BettingDataRNG_AP betReq = new BettingDataRNG_AP()
                    {
                        seq = getReq.Seq,
                        sitecode = getReq.SiteCode,
                        gamecode = getReq.GameCode,
                        gameroundid = getReq.GameRoundId,
                        stake = getReq.Stake,
                        bettime = getReq.BetTime,
                        ip = getReq.Ip,
                        timestamp = getReq.Timestamp,
                        playerid = getReq.PlayerId
                    };

                    if (bettingBLL.PlaceBet_AP(betReq, out decimal tpAffterAmount, out long seq) > 0)
                    {
                        result.Balance = tpAffterAmount;
                        result.ErrorCode = (int)ErrorCode.Success;
                        result.ErrorDescription = "";
                        result.OPTransId = seq.ToString();
                        logger.LogInformation($"{Content}\r\n{JsonConvert.SerializeObject(result)}");
                    }
                    else
                    {
                        result.ErrorCode = (int)ErrorCode.APIError;
                        result.ErrorDescription = ErrorCode.APIError.ToString();
                        result.OPTransId = "";
                        result.Balance = 0;
                        logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                logger.LogError(ex, $"{Content}\r\n{JsonConvert.SerializeObject(result)}");
            }

            return result;
        }

        [HttpPost]
        [Route("[controller]/RefundBet")]
        public async Task<RefundBetResp> RefundBetAsync()
        {
            #region Initial
            RefundBetResp result = new RefundBetResp();
            RefundBetReq getReq = new RefundBetReq();
            #endregion
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            try
            {
                getReq = JsonConvert.DeserializeObject<RefundBetReq>(Content);
                result.Timestamp = getReq.Timestamp;
                result.Seq = getReq.Seq;

                #region Check Hash
                Request.Headers.TryGetValue("ap-hash", out var ApHash);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
                string Hash = tools.ToMD5($"{SecretKey}{Content}").ToLower();
                if (ApHash != Hash)
                {
                    result.ErrorCode = (int)ErrorCode.HashInvalid;
                    result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n{ApHash}\r\n{Hash}");
                    return result;
                }
                #endregion

                CancelBetDataRNG_AP cancel = new CancelBetDataRNG_AP()
                {
                    seq = getReq.Seq,
                    sitecode = getReq.SiteCode,
                    gamecode = getReq.GameCode,
                    gameroundid = getReq.GameRoundId,
                    timestamp = getReq.Timestamp,
                    playerid = getReq.PlayerId,
                    totalstake = getReq.TotalStake
                };
                var cancelresult = settledBLL.CancelBetRNG_AP(cancel, out decimal tpAffterAmount);

                if (cancelresult > 0)
                {
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.Balance = tpAffterAmount;
                    logger.LogInformation($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n cancelresult:{cancelresult}");
                    return result;
                }
                else if (cancelresult == -1)
                {
                    result.ErrorCode = (int)ErrorCode.DuplicateRequest;
                    result.ErrorDescription = ErrorCode.DuplicateRequest.ToString();
                    result.Balance = 0;
                    logger.LogInformation($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n cancelresult:{cancelresult}");
                    return result;
                }
                else if (cancelresult == -2)
                {
                    result.ErrorCode = (int)ErrorCode.GameNotFound;
                    result.ErrorDescription = ErrorCode.GameNotFound.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n cancelresult:{cancelresult}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                logger.LogError(ex, $"{Content}\r\n{JsonConvert.SerializeObject(result)}");
            }

            return result;
        }

        [HttpPost]
        [Route("[controller]/Settle")]
        public async Task<SettleResp> SettleAsync()
        {
            #region InitResult
            SettleResp result = new SettleResp();
            SettleReq getReq = new SettleReq();
            #endregion
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            try
            {
                getReq = JsonConvert.DeserializeObject<SettleReq>(Content);
                result.Timestamp = getReq.Timestamp;
                result.Seq = getReq.Seq;

                #region Check Hash
                Request.Headers.TryGetValue("ap-hash", out var ApHash);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
                string Hash = tools.ToMD5($"{SecretKey}{Content}").ToLower();
                if (ApHash != Hash)
                {
                    result.ErrorCode = (int)ErrorCode.HashInvalid;
                    result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n{ApHash}\r\n{Hash}");
                    return result;
                }
                #endregion

                if (getReq.TotalWin < 0 || getReq.TotalStake < 0)
                {
                    result.ErrorCode = (int)ErrorCode.ParameterIncorrect;
                    result.ErrorDescription = ErrorCode.ParameterIncorrect.ToString();
                    result.Balance = 0;
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}");
                    return result;
                }

                BettingDataRNG_AP bettingData = new BettingDataRNG_AP()
                {
                    timestamp = getReq.Timestamp,
                    seq = getReq.Seq,
                    sitecode = getReq.SiteCode,
                    gamecode = getReq.GameCode,
                    gameroundid = getReq.GameRoundId,
                    playerid = getReq.PlayerId,
                    totalstake = getReq.TotalStake,
                    totalwin = getReq.TotalWin,
                    settletime = getReq.SettleTime,
                    hitjackpot = getReq.HitJackpot,
                    TicketType = (int)BettransType.EndRound
                };

                int SettleResult = settledBLL.SettledRNG_AP(bettingData, getReq.PlayerId, out decimal tpAffterAmount);

                if (SettleResult > 0)
                {
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.Balance = tpAffterAmount;
                    logger.LogInformation($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}");
                }
                else if (SettleResult == -1)
                {
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.Balance = tpAffterAmount;
                    logger.LogInformation($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}");
                }
                else
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    result.Balance = 0;
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}");
                }

            }
            catch (Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                result.Balance = 0;
                logger.LogError(ex, $"{Content}\r\n{JsonConvert.SerializeObject(result)}");
            }
            return result;
        }

        [HttpPost]
        [Route("[controller]/JPSettle")]
        public async Task<JPSettleResp> JPSettleAsync()
        {
            #region InitResult
            JPSettleResp result = new JPSettleResp();
            JPSettleReq getReq = new JPSettleReq();
            #endregion
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            try
            {
                getReq = JsonConvert.DeserializeObject<JPSettleReq>(Content);
                result.Timestamp = getReq.Timestamp;
                result.Seq = getReq.Seq;

                #region Check Hash
                Request.Headers.TryGetValue("ap-hash", out var ApHash);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
                string Hash = tools.ToMD5($"{SecretKey}{Content}").ToLower();

                if (ApHash != Hash)
                {
                    result.ErrorCode = (int)ErrorCode.HashInvalid;
                    result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n{ApHash}\r\n{Hash}");
                    return result;
                }
                #endregion

                if (getReq.JPWin < 0)
                {
                    result.ErrorCode = (int)ErrorCode.ParameterIncorrect;
                    result.ErrorDescription = ErrorCode.ParameterIncorrect.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}");
                    return result;
                }

                BettingDataRNG_AP bettingData = new BettingDataRNG_AP()
                {
                    timestamp = getReq.Timestamp,
                    seq = getReq.Seq,
                    sitecode = getReq.SiteCode,
                    gameroundid = getReq.GameRoundId,
                    playerid = getReq.PlayerId,
                    jpsettletime = getReq.JPSettleTime,
                    jpwin = getReq.JPWin,
                    TicketType = (int)BettransType.Jackpot
                };

                int SettleResult = settledBLL.SettledRNG_AP(bettingData, getReq.PlayerId, out decimal tpAffterAmount);

                if (SettleResult > 0)
                {
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.Balance = tpAffterAmount;
                    logger.LogInformation($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}");
                }
                else if (SettleResult == -1)
                {
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.Balance = tpAffterAmount;
                    logger.LogInformation($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}");
                }
                else
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    result.Balance = 0;
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}");
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                result.Balance = 0;
                logger.LogError(ex, $"{Content}\r\n{JsonConvert.SerializeObject(result)}");
            }

            return result;
        }

        [HttpPost]
        [Route("[controller]/GetBalance")]
        public async Task<GetBalanceResp> GetBalanceAsync()
        {
            #region InitResult
            GetBalanceResp result = new GetBalanceResp();
            GetBalanceReq getReq = new GetBalanceReq();
            #endregion
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            try
            {
                getReq = JsonConvert.DeserializeObject<GetBalanceReq>(Content);
                result.Timestamp = getReq.Timestamp;
                result.Seq = getReq.Seq;
                string authToken = string.Empty;
                string sessionToken = string.Empty;
                string TpUserName = string.Empty;

                #region Check Hash
                Request.Headers.TryGetValue("ap-hash", out var ApHash);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
                string Hash = tools.ToMD5($"{SecretKey}{Content}").ToLower();
                if (ApHash != Hash)
                {
                    result.ErrorCode = (int)ErrorCode.HashInvalid;
                    result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n{ApHash}\r\n{Hash}");
                    return result;
                }
                #endregion

                try
                {
                    string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(getReq.OPToken)));
                    var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                    authToken = sessionKeyArr[0];
                    sessionToken = sessionKeyArr[1];
                    TpUserName = sessionKeyArr[2];
                }
                catch (Exception ex)
                {
                    result.ErrorCode = (int)ErrorCode.TokenInvalid;
                    result.ErrorDescription = ErrorCode.TokenInvalid.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n{ex.ToString()}");
                    return result;
                }
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, TpUserName);
                if (tpCheck == null)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n TpUserName:{TpUserName}");
                    return result;
                }
                else if (tpCheck.Token != sessionToken)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n Token:{tpCheck.Token}\r\n sessionToken:{sessionToken}");
                    return result;
                }
                else if (tpCheck.Locked)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n Locked");
                    return result;
                }
                else
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, TpUserName, gameType);
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.Balance = tpCustBalance.Balance;
                    logger.LogInformation($"{Content}\r\n{JsonConvert.SerializeObject(result)}");
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                logger.LogError(ex, $"{Content}\r\n{JsonConvert.SerializeObject(result)}");
            }

            return result;
        }

        [HttpPost]
        [Route("[controller]/CPSettle")]
        public async Task<CPSettleResp> CPSettleAsync()
        {
            #region InitResult
            CPSettleResp result = new CPSettleResp();
            #endregion

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            try
            {
                CPSettleReq getReq = JsonConvert.DeserializeObject<CPSettleReq>(Content);
                result.Timestamp = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToString("o");
                result.Seq = getReq.Seq;

                #region Check Hash
                Request.Headers.TryGetValue("ap-hash", out var ApHash);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
                string Hash = tools.ToMD5($"{SecretKey}{Content}").ToLower();

                if (ApHash != Hash)
                {
                    result.ErrorCode = (int)ErrorCode.HashInvalid;
                    result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\n{ApHash}\r\n{Hash}");
                    return result;
                }
                #endregion

                if (getReq.CPWin < 0)
                {
                    result.ErrorCode = (int)ErrorCode.ParameterIncorrect;
                    result.ErrorDescription = ErrorCode.ParameterIncorrect.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}");
                    return result;
                }

                CPSettleDataRNG_AP settleData = new CPSettleDataRNG_AP()
                {
                    timestamp = DateTime.Parse(getReq.Timestamp),
                    seq = getReq.Seq,
                    brandcode = getReq.BrandCode,
                    sitecode = getReq.SiteCode,
                    playerid = getReq.PlayerId,
                    cpid = getReq.CPId,
                    cptype = getReq.CPType,
                    cptransid = getReq.CPTransId,
                    cpwin = getReq.CPWin
                };

                int SettleResult = settledBLL.RewardRNG_AP(settleData, getReq.PlayerId, out decimal tpAffterAmount);

                if (SettleResult > 0 || SettleResult == -1)
                {
                    if(tpAffterAmount == 0)
                    {
                        tpAffterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getReq.PlayerId, gameType).Balance;
                    }
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.Balance = tpAffterAmount;
                    logger.LogInformation($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}");
                }
                else
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    logger.LogError($"{Content}\r\n{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}");
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                logger.LogError(ex, $"{Content}\r\n{JsonConvert.SerializeObject(result)}");
            }

            return result;
        }

        #endregion

        #region Mini Game

        [HttpGet]
        [Route("[controller]/{sitename}/api/v1/{game}/wallet/balance")]
        public async Task<dynamic> GetBalanceOfMiniAsync(string sitename, string game, string payload, string signature)
        {
            string logName = $"{_tpName}_GetBalanceOfMini";
            Response.StatusCode = (int)HttpStatusCode.INVALID_DATA;
            ErrorResponse errResponse = new ErrorResponse();
            errResponse.errorMessage = "Server error";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            try
            {
                if (string.IsNullOrEmpty(game) || !CheckGameOfMini(game))
                {
                    Response.StatusCode = (int)HttpStatusCode.INVALID_DATA;
                    errResponse.errorMessage = "Game error";
                    log2File.Error($"{errResponse.errorMessage} - {game}", logName);
                    return errResponse;
                }

                var Decrypt = DecrypeRequest(payload, sitename);
                log2File.Info($"{Decrypt}", logName);
                var getReq = JsonConvert.DeserializeObject<GetBalanceMiniReq>(Decrypt);

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getReq.UserReferenceId);
                if (tpCheck == null)
                {
                    Response.StatusCode = (int)HttpStatusCode.INVALID_SESSION;
                    errResponse.errorMessage = "User not found";
                    return errResponse;
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getReq.UserReferenceId, gameType);
                Response.StatusCode = (int)HttpStatusCode.OK;
                CommonMiniResp response = new CommonMiniResp();
                response.balance = tpCustBalance.Balance;
                response.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return response;
            }
            catch (Exception ex)
            {
                log2File.Error($"{ex.Message}", logName, ex);
            }

            return errResponse;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/api/v1/{game}/wallet/buy")]
        public async Task<dynamic> PlaceBetOfMiniAsync(string sitename, string game)
        {
            string logName = $"{_tpName}_PlaceBetOfMini";
            Response.StatusCode = (int)HttpStatusCode.INVALID_DATA;
            ErrorResponse errResponse = new ErrorResponse();
            errResponse.errorMessage = "Server error";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            try
            {
                if (string.IsNullOrEmpty(game) || !CheckGameOfMini(game))
                {
                    Response.StatusCode = (int)HttpStatusCode.INVALID_DATA;
                    errResponse.errorMessage = "game error";
                    log2File.Error($"{errResponse.errorMessage} - {game}", logName);
                    return errResponse;
                }

                var getCommReq = JsonConvert.DeserializeObject<CommonMiniReq>(Content);

                var Decrypt = DecrypeRequest(getCommReq.payload, sitename);
                log2File.Info($"{Decrypt}", logName);

                var getReq = JsonConvert.DeserializeObject<BettingData_AdvantPlayMini>(Decrypt);
                getReq.MiniGameCode = game;

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getReq.UserReferenceId);
                if (tpCheck == null)
                {
                    Response.StatusCode = (int)HttpStatusCode.INVALID_SESSION;
                    errResponse.errorMessage = "User not found";
                    return errResponse;
                }
                else if (tpCheck.Locked)
                {
                    Response.StatusCode = (int)HttpStatusCode.UNPROCESSABLE;
                    errResponse.errorMessage = "Account is Locked";
                    return errResponse;
                }

                if (getReq.Amount == 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.UNPROCESSABLE;
                    errResponse.errorMessage = "Invalid request input";
                    return errResponse;
                }

                PlaceBetMiniResp response = new PlaceBetMiniResp();
                var result = bettingBLL.PlaceBet_AdvantPlayMini(getReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    response.transactionReferenceId = getReq.TransId;
                    response.balance = tpAffterAmount;
                    response.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    return response;
                }
                else if (result == -1)
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    response.transactionReferenceId = getReq.TransId;
                    response.balance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getReq.UserReferenceId, gameType).Balance;
                    response.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    return response;
                }
                else if (tpAffterAmount < 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.INSUFFICIENT_FUNDS;
                    errResponse.errorMessage = "Insufficient funds";
                    log2File.Error($"insufficient amount {tpAffterAmount}", logName);
                }
                else
                {
                    Response.StatusCode = (int)HttpStatusCode.UNPROCESSABLE;
                    errResponse.errorMessage = "Unknow error";
                    log2File.Error($"Unknow error {result}", logName);
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{ex.Message}", logName, ex);
            }

            return errResponse;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/api/v1/crash/wallet/won")]
        public async Task<dynamic> PayoutOfMiniAsync(string sitename)
        {
            string logName = $"{_tpName}_PayoutOfMini";
            Response.StatusCode = (int)HttpStatusCode.INVALID_DATA;
            ErrorResponse errResponse = new ErrorResponse();
            errResponse.errorMessage = "Server error";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            try
            {
                var getCommReq = JsonConvert.DeserializeObject<CommonMiniReq>(Content);

                var Decrypt = DecrypeRequest(getCommReq.payload, sitename);
                log2File.Info($"{Decrypt}", logName);
                var getReq = JsonConvert.DeserializeObject<PayoutData_AdvantPlayMini>(Decrypt);

                var betting = bettingBLL.GetAdvantPlayMiniBet(getReq.TransId, getReq.SettlementTime, "crash", out _);
                if (betting == null || !betting.Any(x => x.TicketType == (int)BettransType.Bet))
                {
                    log2File.Error($"PlaceBet data not found", logName);
                    return errResponse;
                }

                var split = splitTransactionReferenceId(getReq.TransactionReferenceId);
                getReq.GameId = split.GameId;
                getReq.MiniGameCode = betting.First().MiniGameCode;
                getReq.UserReferenceId = betting.First().UserReferenceId;

                CommonMiniResp response = new CommonMiniResp();

                var result = settledBLL.Payout_AdvantPlayMini(getReq, out decimal tpAffterAmount);

                if (result > 0)
                {
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = tpCustInfoBLL.GetBalance((int)thisTp, getReq.UserReferenceId, gameType).Balance;
                    }
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    response.balance = tpAffterAmount;
                    response.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    return response;
                }
                else if (result == -1)
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    response.balance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getReq.UserReferenceId, gameType).Balance;
                    response.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    return response;
                }
                else
                {
                    Response.StatusCode = (int)HttpStatusCode.INVALID_DATA;
                    errResponse.errorMessage = "Unknow error";
                    log2File.Error($"Unknow error {result}", logName);
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{ex.Message}", logName, ex);
            }

            return errResponse;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/api/v1/crash/wallet/result")]
        public async Task<dynamic> ResultOfMiniAsync(string sitename)
        {
            string logName = $"{_tpName}_ResultOfMini";
            Response.StatusCode = (int)HttpStatusCode.INVALID_DATA;
            ErrorResponse errResponse = new ErrorResponse();
            errResponse.errorMessage = "Server error";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            try
            {
                var getCommReq = JsonConvert.DeserializeObject<CommonMiniReq>(Content);

                var Decrypt = DecrypeRequest(getCommReq.payload, sitename);
                log2File.Info($"{Decrypt}", logName);
                var getReq = JsonConvert.DeserializeObject<ResultData_AdvantPlayMini>(Decrypt);

                var betting = bettingBLL.GetAdvantPlayMiniBet(getReq.TransId, getReq.SettlementTime, "crash", out _);
                if (betting == null || !betting.Any(x => x.TicketType == (int)BettransType.Bet))
                {
                    log2File.Error($"PlaceBet data not found", logName);
                    return errResponse;
                }

                getReq.MiniGameCode = betting.First().MiniGameCode;
                getReq.UserReferenceId = betting.First().UserReferenceId;

                ResultMiniResp response = new ResultMiniResp();

                var result = settledBLL.Result_AdvantPlayMini(getReq, out long SQ);
                if (result > 0 || result == -1)
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    response.processId = SQ.ToString();
                    response.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    return response;
                }
                else
                {
                    errResponse.errorMessage = "Unknow error";
                    log2File.Error($"Unknow error {result}", logName);
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{ex.Message}", logName, ex);
            }

            return errResponse;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/api/v1/cardracing/wallet/endResult")]
        public async Task<dynamic> PayoutEndResultOfMiniAsync(string sitename)
        {
            string logName = $"{_tpName}_PayoutEndResultOfMini";
            Response.StatusCode = (int)HttpStatusCode.INVALID_DATA;
            ErrorResponse errResponse = new ErrorResponse();
            errResponse.errorMessage = "Server error";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            try
            {
                var getCommReq = JsonConvert.DeserializeObject<CommonMiniReq>(Content);

                var Decrypt = DecrypeRequest(getCommReq.payload, sitename);
                log2File.Info($"{Decrypt}", logName);
                var TransId = "";
                var getReq = JsonConvert.DeserializeObject<PayoutData_AdvantPlayMini>(Decrypt);
                if (getReq.WinResults.Any() && getReq.WinResults.Count() > 0)
                {
                    TransId = getReq.WinResults.First().TransactionReferenceId;
                }
                if (getReq.LostResults.Any() && getReq.LostResults.Count() > 0)
                {
                    TransId = getReq.LostResults.First().TransactionReferenceId;
                }
                if (TransId == "")
                {
                    log2File.Error($"get TransactionReferenceId error", logName);
                    return errResponse;
                }

                var betting = bettingBLL.GetAdvantPlayMiniBet(TransId, getReq.SettlementTime, "cardracing", out _);
                if (betting == null || !betting.Any(x => x.TicketType == (int)BettransType.Bet))
                {
                    log2File.Error($"PlaceBet data not found", logName);
                    return errResponse;
                }
                getReq.MiniGameCode = betting.First().MiniGameCode;
                getReq.UserReferenceId = betting.First().UserReferenceId;

                CommonMiniResp response = new CommonMiniResp();
                var result = settledBLL.Payout_AdvantPlayMini(getReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = tpCustInfoBLL.GetBalance((int)thisTp, getReq.UserReferenceId, gameType).Balance;
                    }
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    response.balance = tpAffterAmount;
                    response.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    return response;
                }
                else if (result == -1)
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    response.balance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getReq.UserReferenceId, gameType).Balance; ;
                    response.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    return response;
                }
                else
                {
                    Response.StatusCode = (int)HttpStatusCode.INVALID_DATA;
                    errResponse.errorMessage = "Unknow error";
                    log2File.Error($"Unknow error {result}", logName);
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{ex.Message}", logName, ex);
            }

            return errResponse;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/api/v1/{game}/wallet/refund")]
        public async Task<dynamic> RefundOfMiniAsync(string sitename, string game)
        {
            string logName = $"{_tpName}_RefundOfMini";
            Response.StatusCode = (int)HttpStatusCode.INVALID_DATA;
            ErrorResponse errResponse = new ErrorResponse();
            errResponse.errorMessage = "Server error";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            try
            {
                if (string.IsNullOrEmpty(game) || !CheckGameOfMini(game))
                {
                    Response.StatusCode = (int)HttpStatusCode.INVALID_DATA;
                    errResponse.errorMessage = "game error";
                    log2File.Error($"{errResponse.errorMessage} - {game}", logName);
                    return errResponse;
                }

                RefundMiniResp response = new RefundMiniResp();
                var getCommReq = JsonConvert.DeserializeObject<CommonMiniReq>(Content);

                var Decrypt = DecrypeRequest(getCommReq.payload, sitename);
                log2File.Info($"{Decrypt}", logName);
                var getReq = JsonConvert.DeserializeObject<CancelBetData_AdvantPlayMini>(Decrypt);

                var betting = bettingBLL.GetAdvantPlayMiniBet(getReq.TransId, getReq.refundTime, game, out _);
                if (betting == null || betting.Any(x => x.TicketType == (int)BettransType.CancelBet))
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    response.refundId = getReq.TransId;
                    response.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    return response;
                }

                getReq.MiniGameCode = betting.First().MiniGameCode;
                getReq.UserReferenceId = betting.First().UserReferenceId;

                var result = settledBLL.CancelBet_AdvantPlayMini(getReq);
                if (result > 0 || result == -1)
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;
                    response.refundId = getReq.TransId;
                    response.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    return response;
                }
                else
                {
                    errResponse.errorMessage = "Unknow error";
                    log2File.Error($"Unknow error {result}", logName);
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{ex.Message}", logName, ex);
            }

            return errResponse;
        }

        private bool CheckGameOfMini(string game)
        {
            if (_gameOfMiniList.Contains(game, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private string DecrypeRequest(string payload, string sitename)
        {
            var serverInfo = thirdPartyBLL.GetServerInfoBySiteName(sitename);
            var siteId = (serverInfo != null) ? serverInfo.SiteId : 1;
            var thirdPartySetting = thirdPartyBLL.GetThirdPartyBySite((int)thisTp, siteId);
            string SecretKey = thirdPartySetting.TpSetting["SecretKey_MiniGame"];
            return tools.AesDecryptV2(payload, SecretKey);
        }

        private SplitTransactionReferenceId splitTransactionReferenceId(string transactionReferenceId)
        {
            var split = transactionReferenceId.Split("_");
            if (split.Count() != 2)
            {
                throw new Exception("splitTransactionReferenceId failed");
            }

            var p = new SplitTransactionReferenceId();
            p.GameId = split[0];
            p.SessionId = split[1];
            return p;
        }

        #endregion
    }
}
