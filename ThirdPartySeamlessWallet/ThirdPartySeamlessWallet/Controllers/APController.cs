using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private readonly Log2File log2File;
        private readonly Tp thisTp;
        private readonly int gameType = 0;
        private readonly Tools tools;
        private readonly ILogger<APController> logger;
        private readonly int isTestAccount;
        #endregion

        #region Constructors
        public APController(AuthBLL authBLL, ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, BettingBLL bettingBLL, SettledBLL settledBLL, Log2File log2File, Tools tools, ILogger<APController> logger, IConfiguration configuration)
        {
            this.authBLL = authBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            this.tools = tools;
            this.logger = logger;
            thisTp = Tp.AP;
            gameType = (int)GameType.AP_RNG;
            this.isTestAccount = configuration.GetValue<int>("IsAPTestAccount");
        }
        #endregion

        #region model

        #region Resp
        public class Resp
        {
            public string Timestamp { get; set; } = "";
            public string Seq { get; set; } = "";
            //public string SiteCode { get; set; }
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
            public string GameCode { get; set; }
            public string GameRoundId { get; set; }
            public decimal Balance { get; set; }
        }
        public class SettleResp : Resp
        {
            public decimal Balance { get; set; }
        }
        public class JPSettleResp : Resp
        {
            public decimal Balance { get; set; }
        }
        public class GetBalanceResp : Resp
        {
            public decimal Balance { get; set; }
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
                result.TestAccount = isTestAccount;

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
                    logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ex.ToString()}\r\n AP_GetPlayerInfo");
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
                logger.LogInformation($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ex.ToString()}\r\n AP_GetPlayerInfo");
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


                #region Check Hash
                Request.Headers.TryGetValue("ap-hash", out var ApHash);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
                string reqJson = JsonConvert.SerializeObject(getReq);
                string Hash = tools.ToMD5($"{SecretKey}{Content}");
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
                log2File.Error($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ex}", "AP_PlaceBet", ex);
            }

            return result;
        }

        [HttpPost]
        [Route("[controller]/RefundBet")]
        public async Task<RefundBetResp> RefundBetAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();
            var getReq = JsonConvert.DeserializeObject<RefundBetReq>(Content);
            #region InitResult
            RefundBetResp result = new RefundBetResp()
            {
                Timestamp = getReq.Timestamp,
                Seq = getReq.Seq
            };
            #endregion

            #region Check Hash
            Request.Headers.TryGetValue("ap-hash", out var ApHash);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
            string reqJson = JsonConvert.SerializeObject(getReq);
            string Hash = tools.ToMD5($"{SecretKey}{reqJson}");
            if (ApHash != Hash)
            {
                result.ErrorCode = (int)ErrorCode.HashInvalid;
                result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ApHash}\r\n{Hash}", "RefundBet");
                return result;
            }
            #endregion

            try
            {
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
                    log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}", "RefundBet");
                    return result;
                }
                else if (cancelesult == -1)
                {
                    result.ErrorCode = (int)ErrorCode.DuplicateRequest;
                    result.ErrorDescription = ErrorCode.DuplicateRequest.ToString();
                    result.Balance = 0;
                    log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}", "RefundBet");
                    return result;
                }
                else if (cancelesult == -2)
                {
                    result.ErrorCode = (int)ErrorCode.GameNotFound;
                    result.ErrorDescription = ErrorCode.GameNotFound.ToString();
                    log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}", "RefundBet");
                    return result;
                }
            }
            catch(Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                log2File.Error($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ex}", "RefundBet", ex);
            }


            return result;
        }

        [HttpPost]
        [Route("[controller]/Settle")]
        public async Task<SettleResp> SettleAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();
            var getReq = JsonConvert.DeserializeObject<SettleReq>(Content);
            //string authToken = string.Empty;
            //string sessionToken = string.Empty;
            //string TpUserName = string.Empty;

            #region InitResult
            SettleResp result = new SettleResp()
            {
                Timestamp = getReq.Timestamp,
                Seq = getReq.Seq
            };
            #endregion

            #region Check Hash
            Request.Headers.TryGetValue("ap-hash", out var ApHash);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
            string reqJson = JsonConvert.SerializeObject(getReq);
            string Hash = tools.ToMD5($"{SecretKey}{reqJson}");
            if (ApHash != Hash)
            {
                result.ErrorCode = (int)ErrorCode.HashInvalid;
                result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ApHash}\r\n{Hash}", "Settle");
                return result;
            }
            #endregion

            //try
            //{
            //    string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(getReq.OPToken)));
            //    var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            //    authToken = sessionKeyArr[0];
            //    sessionToken = sessionKeyArr[1];
            //    TpUserName = sessionKeyArr[2];
            //}
            //catch (Exception ex)
            //{
            //    result.ErrorCode = (int)ErrorCode.TokenInvalid;
            //    result.ErrorDescription = ErrorCode.TokenInvalid.ToString();
            //}


            if (getReq.TotalWin < 0 || getReq.TotalStake < 0)
            {
                result.ErrorCode = (int)ErrorCode.ParameterIncorrect;
                result.ErrorDescription = ErrorCode.ParameterIncorrect.ToString();
                result.Balance = 0;
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}", "Settle");
                return result;
            }

            //getReq.TotalWin = tools.Floor(getReq.TotalWin, 2);
            //getReq.TotalStake = tools.Floor(getReq.TotalStake, 2);

            try
            {
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
                }
                else if(SettleResult == -1)
                {
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.Balance = tpAffterAmount;
                }
                else
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    result.Balance = 0;
                }
            }
            catch(Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                result.Balance = 0;
            }

            log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}", "Settle");
            return result;
        }

        [HttpPost]
        [Route("[controller]/JPSettle")]
        public async Task<JPSettleResp> JPSettleAsync()
        {
            JPSettleResp result = new JPSettleResp();

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();
            var getReq = JsonConvert.DeserializeObject<JPSettleReq>(Content);

            #region Check Hash
            Request.Headers.TryGetValue("ap-hash", out var ApHash);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
            string reqJson = JsonConvert.SerializeObject(getReq);
            string Hash = tools.ToMD5($"{SecretKey}{reqJson}");

            if (ApHash != Hash)
            {
                result.ErrorCode = (int)ErrorCode.HashInvalid;
                result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ApHash}\r\n{Hash}", "JPSettle");
                return result;
            }
            #endregion

            if (getReq.JPWin < 0)
            {
                result.ErrorCode = (int)ErrorCode.ParameterIncorrect;
                result.ErrorDescription = ErrorCode.ParameterIncorrect.ToString();
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}", "JPSettle");
                return result;
            }

            try
            {
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
                }
                else if (SettleResult == -1)
                {
                    result.ErrorCode = (int)ErrorCode.Success;
                    result.ErrorDescription = "";
                    result.Balance = tpAffterAmount;
                }
                else
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                    result.Balance = 0;
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = (int)ErrorCode.APIError;
                result.ErrorDescription = ErrorCode.APIError.ToString();
                result.Balance = 0;
            }
            log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}", "JPSettle");
            return result;
        }

        [HttpPost]
        [Route("[controller]/GetBalance")]
        public async Task<GetBalanceResp> GetBalanceAsync()
        {

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();
            var getReq = JsonConvert.DeserializeObject<GetBalanceReq>(Content);
            string authToken = string.Empty;
            string sessionToken = string.Empty;
            string TpUserName = string.Empty;

            GetBalanceResp result = new GetBalanceResp()
            {
                Timestamp = getReq.Timestamp,
                Seq = getReq.Seq,
            };

            #region Check Hash
            Request.Headers.TryGetValue("ap-hash", out var ApHash);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            string SecretKey = thirdPartySetting.TpSetting["SecretKey"];
            string reqJson = JsonConvert.SerializeObject(getReq);
            string Hash = tools.ToMD5($"{SecretKey}{reqJson}");
            if (ApHash != Hash)
            {
                result.ErrorCode = (int)ErrorCode.HashInvalid;
                result.ErrorDescription = ErrorCode.HashInvalid.ToString();
                log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ApHash}\r\n{Hash}", "AP_GetBalance");
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
            }

            try
            {
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, TpUserName);
                if (tpCheck == null)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                }
                else if (tpCheck.Token != sessionToken)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
                }
                else if (tpCheck.Locked)
                {
                    result.ErrorCode = (int)ErrorCode.APIError;
                    result.ErrorDescription = ErrorCode.APIError.ToString();
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
                log2File.Error($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}\r\n{ex}", "AP_GetBalance", ex);
            }

            log2File.Info($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(result)}", "AP_GetBalance");
            return result;
        }
    }
}
