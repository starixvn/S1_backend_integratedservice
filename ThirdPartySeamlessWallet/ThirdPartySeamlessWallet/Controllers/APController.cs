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
        public class GetPlayerInfoResp: Resp
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
        public class PlaceBetResp: Resp
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
        public class GetBalanceResp : Resp
        {
            public decimal Balance { get; set; } = 0;
        }
        #endregion

        #region Req
        public class Req
        {
            public string Timestamp { get; set; }
            public string Seq { get; set; }
            public string SiteCode { get; set; }
        }
        public class GetPlayerInfoReq: Req
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
        public class SettleReq: Req
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
        public class GetBalanceReq: Req 
        {
            public string OPToken { get; set; }
            public string PlayerId { get; set; }
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
            PlayerHasInsufficientFunds = 5321
        }
        #endregion

        [HttpPost]
        [Route("[controller]/GetPlayerInfo")]
        public async Task<GetPlayerInfoResp> GetPlayerInfoAsync()
        {
            #region Initial
            GetPlayerInfoResp result = new GetPlayerInfoResp();
            GetPlayerInfoReq getReq = new GetPlayerInfoReq();
            #endregion

            try
            {
                string authToken = string.Empty;
                string sessionToken = string.Empty;
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string Content = await reader.ReadToEndAsync();
                getReq = JsonConvert.DeserializeObject<GetPlayerInfoReq>(Content);
                result.Timestamp = getReq.Timestamp;
                result.Seq = getReq.Seq;
                result.TestAccount = environment.IsDevelopment() ? 1 : 0; ;

                #region Check Hash
                Request.Headers.TryGetValue("ap-hash", out var ApHash);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
                string reqJson = JsonConvert.SerializeObject(getReq);
                string Hash = tools.ToMD5($"{SecretKey}{Content}").ToLower();
                if (ApHash != Hash)
                {
                    result.ErrorCode = (int)ErrorCode.HashInvalid;
                    result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n ApHash:{ApHash}\r\n Hash:{Hash}\r\n AP_GetPlayerInfo");
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
                    logger.LogError(ex,$"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n AP_GetPlayerInfo");
                    return result;
                }

                if (!authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                {
                    result.ErrorCode = (int)ErrorCode.TokenInvalid;
                    result.ErrorDescription = ErrorCode.TokenInvalid.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n AP_GetPlayerInfo");
                    return result;
                }
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
                if (tpCheck == null)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n authToken:{authToken}\r\n AP_GetPlayerInfo");
                    return result;
                }
                else if (tpCheck.Token != sessionToken)
                {
                    result.ErrorCode = (int)ErrorCode.TokenInvalid;
                    result.ErrorDescription = ErrorCode.TokenInvalid.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n Token:{tpCheck.Token}\r\n sessionToken:{sessionToken} \r\n AP_GetPlayerInfo");
                    return result;
                }
                else if (tpCheck.Locked)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n Locked\r\n AP_GetPlayerInfo");
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
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                logger.LogError(ex,$"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n AP_GetPlayerInfo");
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

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string Content = await reader.ReadToEndAsync();
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
                string reqJson = JsonConvert.SerializeObject(getReq);
                string Hash = tools.ToMD5($"{SecretKey}{Content}").ToLower();
                if (ApHash != Hash)
                {
                    result.ErrorCode = (int)ErrorCode.HashInvalid;
                    result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ApHash}\r\n{Hash}\r\n AP_PlaceBet");
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
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ex.ToString()}\r\n AP_PlaceBet");
                }

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, TpUserName);
                if (tpCheck == null)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n authToken:{authToken}\r\n AP_PlaceBet");
                    return result;
                }
                else if (tpCheck.Token != sessionToken)
                {
                    result.ErrorCode = (int)ErrorCode.TokenInvalid;
                    result.ErrorDescription = ErrorCode.TokenInvalid.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n Token:{tpCheck.Token}\r\n sessionToken:{sessionToken} \r\n AP_PlaceBet");
                    return result;
                }
                else if (tpCheck.Locked)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n Locked\r\n AP_PlaceBet");
                    return result;
                }
                else if(getReq.Stake < 0)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, TpUserName, gameType);
                    result.Balance = tpCustBalance.Balance;
                    result.ErrorCode = (int)ErrorCode.ParameterIncorrect;
                    result.ErrorDescription = ErrorCode.ParameterIncorrect.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n Stake < 0\r\n AP_PlaceBet");
                    return result;
                }
                else
                {
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

                    if (bettingBLL.PlaceBet_AP(betReq, out decimal tpAffterAmount, out int seq) > 0)
                    {
                        result.Balance = tpAffterAmount;
                        result.ErrorCode = (int)ErrorCode.Success;
                        result.ErrorDescription = "";
                        result.OPTransId = seq.ToString();
                    }
                    else
                    {
                        result.ErrorCode = (int)ErrorCode.APIError;
                        result.ErrorDescription = ErrorCode.APIError.ToString();
                        result.OPTransId = "";
                        result.Balance = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                logger.LogError(ex,$"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n AP_PlaceBet");
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

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string Content = await reader.ReadToEndAsync();
                getReq = JsonConvert.DeserializeObject<RefundBetReq>(Content);
                result.Timestamp = getReq.Timestamp;
                result.Seq = getReq.Seq;

                #region Check Hash
                Request.Headers.TryGetValue("ap-hash", out var ApHash);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
                string reqJson = JsonConvert.SerializeObject(getReq);
                string Hash = tools.ToMD5($"{SecretKey}{Content}").ToLower();
                if (ApHash != Hash)
                {
                    result.ErrorCode = (int)ErrorCode.HashInvalid;
                    result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ApHash}\r\n{Hash}\r\n AP_RefundBet");
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
                var cancelesult = settledBLL.CancelBetRNG_AP(cancel, out decimal tpAffterAmount);

                if (cancelesult > 0)
                {
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.Balance = tpAffterAmount;
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n cancelesult:{cancelesult}\r\n AP_RefundBet");
                    return result;
                }
                else if (cancelesult == -1)
                {
                    result.ErrorCode = (int)ErrorCode.DuplicateRequest;
                    result.ErrorDescription = ErrorCode.DuplicateRequest.ToString();
                    result.Balance = 0;
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n cancelesult:{cancelesult}\r\n AP_RefundBet");
                    return result;
                }
                else if (cancelesult == -2)
                {
                    result.ErrorCode = (int)ErrorCode.GameNotFound;
                    result.ErrorDescription = ErrorCode.GameNotFound.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n cancelesult:{cancelesult}\r\n AP_RefundBet");
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ex}\r\n AP_RefundBet");
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

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string Content = await reader.ReadToEndAsync();
                getReq = JsonConvert.DeserializeObject<SettleReq>(Content);
                result.Timestamp = getReq.Timestamp;
                result.Seq = getReq.Seq;

                #region Check Hash
                Request.Headers.TryGetValue("ap-hash", out var ApHash);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
                string reqJson = JsonConvert.SerializeObject(getReq);
                string Hash = tools.ToMD5($"{SecretKey}{Content}").ToLower();
                if (ApHash != Hash)
                {
                    result.ErrorCode = (int)ErrorCode.HashInvalid;
                    result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ApHash}\r\n{Hash}\r\n AP_Settle");
                    return result;
                }
                #endregion

                if (getReq.TotalWin < 0 || getReq.TotalStake < 0)
                {
                    result.ErrorCode = (int)ErrorCode.ParameterIncorrect;
                    result.ErrorDescription = ErrorCode.ParameterIncorrect.ToString();
                    result.Balance = 0;
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n AP_Settle");
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
                    hitjackpot = getReq.HitJackpot
                };

                int SettleResult = settledBLL.SettledRNG_AP(bettingData, getReq.PlayerId, out decimal tpAffterAmount);

                if (SettleResult > 0)
                {
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.Balance = tpAffterAmount;
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}\r\n AP_Settle");
                }
                else if (SettleResult == -1)
                {
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.Balance = tpAffterAmount;
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}\r\n AP_Settle");
                }
                else
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    result.Balance = 0;
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}\r\n AP_Settle");
                }

            }
            catch (Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                result.Balance = 0;
                logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ex}\r\n AP_Settle");
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

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string Content = await reader.ReadToEndAsync();
                getReq = JsonConvert.DeserializeObject<JPSettleReq>(Content);
                result.Timestamp = getReq.Timestamp;
                result.Seq = getReq.Seq;

                #region Check Hash
                Request.Headers.TryGetValue("ap-hash", out var ApHash);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
                string reqJson = JsonConvert.SerializeObject(getReq);
                string Hash = tools.ToMD5($"{SecretKey}{Content}").ToLower();

                if (ApHash != Hash)
                {
                    result.ErrorCode = (int)ErrorCode.HashInvalid;
                    result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ApHash}\r\n{Hash}\r\n AP_JPSettle");
                    return result;
                }
                #endregion

                if (getReq.JPWin < 0)
                {
                    result.ErrorCode = (int)ErrorCode.ParameterIncorrect;
                    result.ErrorDescription = ErrorCode.ParameterIncorrect.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n AP_JPSettle");
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
                    jpwin = getReq.JPWin
                };

                int SettleResult = settledBLL.SettledRNG_AP(bettingData, getReq.PlayerId, out decimal tpAffterAmount);

                if (SettleResult > 0)
                {
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.Balance = tpAffterAmount;
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}\r\n AP_JPSettle");
                }
                else if (SettleResult == -1)
                {
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.Balance = tpAffterAmount;
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}\r\n AP_JPSettle");
                }
                else
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    result.Balance = 0;
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}\r\n AP_JPSettle");
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                result.Balance = 0;
                logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ex}\r\n AP_JPSettle");
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

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string Content = await reader.ReadToEndAsync();
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
                string reqJson = JsonConvert.SerializeObject(getReq);
                string Hash = tools.ToMD5($"{SecretKey}{Content}").ToLower();
                if (ApHash != Hash)
                {
                    result.ErrorCode = (int)ErrorCode.HashInvalid;
                    result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ApHash}\r\n{Hash}\r\n AP_GetBalance");
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
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ex.ToString()}\r\n AP_GetBalance");
                    return result;
                }
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, TpUserName);
                if (tpCheck == null)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n TpUserName:{TpUserName}\r\n AP_GetBalance");
                    return result;
                }
                else if (tpCheck.Token != sessionToken)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n Token:{tpCheck.Token}\r\n sessionToken:{sessionToken} \r\n AP_GetBalance");
                    return result;
                }
                else if (tpCheck.Locked)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n Locked\r\n AP_GetBalance");
                    return result;
                }
                else
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, TpUserName, gameType);
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.Balance = tpCustBalance.Balance;
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ex.ToString()}\r\n AP_GetBalance");
            }
      
            return result;
        }
    }
}
