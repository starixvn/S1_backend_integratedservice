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
    public class SkywindController : ControllerBase
    {
        #region Fields

        private readonly AuthBLL authBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Tp thisTp;
        //private readonly int gameType = 0;
        private readonly Tools tools;
        private readonly ILogger<SkywindController> logger;
        private readonly IHostEnvironment environment;
        #endregion

        #region Constructors
        public SkywindController(IServiceProvider provider)
        {
            authBLL = provider.GetRequiredService<AuthBLL>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingBLL = provider.GetRequiredService<BettingBLL>();
            settledBLL = provider.GetRequiredService<SettledBLL>();
            tools = provider.GetRequiredService<Tools>();
            logger = provider.GetRequiredService<ILogger<SkywindController>>();
            thisTp = Tp.Skywind;
            //gameType = (int)GameType.Skywind_RNG;
            environment = provider.GetRequiredService<IHostEnvironment>();
        }
        #endregion

        #region model

        #region Resp
        public class GetBalanceResp
        {
            public int error_code { get; set; }
            public decimal balance { get; set; }
            public string currency_code { get; set; }
        }
        public class ValidateTicketResp
        {
            public int error_code { get; set; }
            public string cust_session_id { get; set; }
            public string cust_id { get; set; }
            public string currency_code { get; set; }
            public bool test_cust { get; set; }
            public string country { get; set; }
            public string game_group { get; set; }
            public string rci { get; set; }
            public string rce { get; set; }
        }
        #endregion

        #region Req
        public class ValidateTicketReq
        {
            public string ticket { get; set; }
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
            public string ip { get; set; }
        }
        public class GetBalanceReq 
        {
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
            public string cust_id { get; set; }
            public string cust_session_id { get; set; }
            public string game_code { get; set; }
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

        #region
        [Route("[controller]/api/validate_ticket")]
        [HttpPost]
        public ValidateTicketResp ValidateTicket(ValidateTicketReq req)
        {
            ValidateTicketResp result = new ValidateTicketResp()
            {
                error_code = -1
            };

            try
            {
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(req.ticket));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string authToken = sessionKeyArr[0];
                string sessionToken = sessionKeyArr[1];
                string tpUserName = sessionKeyArr[2];
                var tpCheck = thirdPartyBLL.ThirdPartyCheck((int)thisTp, tpUserName);
                if(tpCheck == null || tpCheck.Token != sessionToken)
                    return result;
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Skywind, tpUserName);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Skywind, tpCustInfo.CurrencyId);
                var tpCountryCode = tpCustInfoBLL.GetTpCurrencyCode((int)Tp.Skywind, tpCustInfo.CurrencyId);

                result.error_code = 0;
                result.cust_session_id = sessionToken;
                result.cust_id = tpUserName;
                result.country = tpCountryCode.CountryCode;
                result.test_cust = true;
                result.currency_code = tpCurrency.TpCurrencyId;
            }
            catch (Exception ex)
            {
                logger.LogError($"TpCountryCodeError\r\n{JsonConvert.SerializeObject(req)}\r\n{ex.ToString()}");
            }
            //ValidateTicketResp result = new ValidateTicketResp()
            //{
            //    error_code = 0,
            //    cust_session_id = "nova88_willie1016_SessId",
            //    cust_id = "nova88_willie1016",
            //    currency_code = "GBP",
            //    test_cust = true,
            //    country = "GB",
            //    game_group = "Test Group",
            //    rci = "60",
            //    rce = "11"
            //};
            return result;
        }

        //[Route("[controller]/api/get_balance")]
        //[HttpPost]
        //public GetBalanceResp GetBalance(GetBalanceReq req)
        //{
        //    #region InitResult
        //    GetBalanceResp result = new GetBalanceResp();
        //    #endregion

        //    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, req.cust_id);
        //    if (tpCheck == null)
        //    {
        //        result.error_code = (int)SkywindErrorCode.PlayerNotFound;
        //        logger.LogError($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(result)}\r\n TpUserName:{req.cust_id}");
        //        return result;
        //    }
        //}
        #endregion

        private bool ValidateToken(string auth_token)
        {
            return true;
        }
        public enum SkywindErrorCode
        {
            NoErrors = 0,
            DuplicateTransaction = 1,
            MerchantInternalError = -1,
            PlayerNotFound = -2,
            GameTokenExpired = -3,
            PlayerIsSuspended = -301,
            BetLimitWasExceeded = -302,
            InsufficientBalance = -4,
            InsufficientFreeBetsBalance = -5,
            InvalidFreeBet = -6,
            TransactionNotFound = -7,
        }
    }
}
