using System.Text;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
            logger = provider.GetRequiredService<ILogger<SkywindController>>();
            thisTp = Tp.Skywind;
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
        public class DebitResp
        {
            public int error_code { get; set; }
            public string error_msg { get; set; }
            public decimal balance { get; set; }
            public string trx_id { get; set; }
        }
        public class CreditResp
        {
            public int error_code { get; set; }
            public string error_msg { get; set; }
            public decimal balance { get; set; }
            public string trx_id { get; set; }
        }
        public class RollbackResp
        {
            public int error_code { get; set; }
            public decimal balance { get; set; }
            public string trx_id { get; set; }
        }
        public class ValidateTerminalTicketResp
        {
            public int error_code { get; set; }
            public string cust_session_id { get; set; }
            public string cust_id { get; set; }
            public string currency_code { get; set; }
            public bool test_cust { get; set; }
            public string country { get; set; }
            public string game_group { get; set; }
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
        public class DebitReq
        {
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
            public string cust_id { get; set; }
            public string cust_session_id { get; set; }
            public string round_id { get; set; }
            public decimal amount { get; set; }
            public string currency_code { get; set; } = "";
            public string game_code { get; set; } = "";
            public string trx_id { get; set; } = "";
            public long game_id { get; set; }
            public string event_type { get; set; } = "";
            public int event_id { get; set; }
            public long timestamp { get; set; }
            public string game_type { get; set; } = "";
            public string platform { get; set; } = "";
            public string promo_id { get; set; } = "";
            public string promo_pid { get; set; } = "";
            public decimal jp_contribution { get; set; }
            public long operation_ts { get; set; }
            public string distribution_type { get; set; } = "";
        }
        public class CreditReq
        {
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
            public string cust_id { get; set; }
            public string cust_session_id { get; set; }
            public string round_id { get; set; } = "";
            public decimal amount { get; set; }
            public string currency_code { get; set; } = "";
            public string game_code { get; set; } = "";
            public string trx_id { get; set; } = "";
            public long game_id { get; set; }
            public string event_type { get; set; } = "";
            public bool jp_win { get; set; }
            public int event_id { get; set; }
            public long timestamp { get; set; }
            public string game_type { get; set; } = "";
            public string game_status { get; set; } = "";
            public string platform { get; set; } = "";
            public long operation_ts { get; set; }
            public string[] jp_ids { get; set; } = null;
            public string promo_id { get; set; } = "";
            public string promo_pid { get; set; } = "";
            public string distribution_type { get; set; } = "";
        }
        public class RollbackReq
        {
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
            public string cust_id { get; set; }
            public string cust_session_id { get; set; }
            public string round_id { get; set; } = "";
            public string currency_code { get; set; } = "";
            public string game_code { get; set; } = "";
            public string trx_id { get; set; } = "";
            public long game_id { get; set; }
            public string event_type { get; set; } = "";
            public long timestamp { get; set; }
            public long operation_ts { get; set; }
        }
        public class ValidateTerminalTicketReq
        {
            public string ticket { get; set; }
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
            public string ip { get; set; }
        }
        #endregion

        #endregion

        #region enum
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
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(req.ticket)));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string authToken = sessionKeyArr[0];
                string sessionToken = sessionKeyArr[1];
                string tpUserName = sessionKeyArr[2];
                if (!authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                {
                    return result;
                }
                var tpCheck = thirdPartyBLL.ThirdPartyCheck((int)thisTp, tpUserName);
                if (tpCheck == null || tpCheck.Token != sessionToken)
                    return result;
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Skywind, tpUserName);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Skywind, tpCustInfo.CurrencyId);
                var tpCountryCode = tpCustInfoBLL.GetTpCurrencyCode((int)Tp.Skywind, tpCustInfo.CurrencyId);

                result.error_code = 0;
                result.cust_session_id = sessionToken;
                result.cust_id = tpUserName;
                result.country = tpCountryCode.CountryCode;
                result.currency_code = tpCurrency.TpCurrencyId;
                if (environment.IsDevelopment() || tpCustInfoAuth.IsTest)
                {
                    result.test_cust = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"ValidateTicket\r\n{JsonConvert.SerializeObject(req)}\r\n{ex.ToString()}");
            }
            return result;
        }

        [Route("[controller]/api/validate_terminal_ticket")]
        [HttpPost]
        public ValidateTerminalTicketResp ValidateTerminalTicket(ValidateTicketReq req)
        {
            ValidateTerminalTicketResp result = new ValidateTerminalTicketResp()
            {
                error_code = -1
            };

            try
            {
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(req.ticket)));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string authToken = sessionKeyArr[0];
                string sessionToken = sessionKeyArr[1];
                string tpUserName = sessionKeyArr[2];
                if (!authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                {
                    return result;
                }
                var tpCheck = thirdPartyBLL.ThirdPartyCheck((int)thisTp, tpUserName);
                if (tpCheck == null || tpCheck.Token != sessionToken)
                    return result;
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Skywind, tpUserName);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Skywind, tpCustInfo.CurrencyId);
                var tpCountryCode = tpCustInfoBLL.GetTpCurrencyCode((int)Tp.Skywind, tpCustInfo.CurrencyId);

                result.error_code = 0;
                result.cust_session_id = sessionToken;
                result.cust_id = tpUserName;
                result.country = tpCountryCode.CountryCode;
                result.currency_code = tpCurrency.TpCurrencyId;
                if (environment.IsDevelopment() || tpCustInfoAuth.IsTest)
                {
                    result.test_cust = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"ValidateTerminalTicket\r\n{JsonConvert.SerializeObject(req)}\r\n{ex.ToString()}");
            }

            return result;
        }

        [Route("[controller]/api/get_balance")]
        [HttpPost]
        public GetBalanceResp GetBalance(GetBalanceReq req)
        {
            #region InitResult
            GetBalanceResp result = new GetBalanceResp();
            #endregion

            var tpCheck = thirdPartyBLL.ThirdPartyCheck((int)thisTp, req.cust_id);
            if (tpCheck == null || tpCheck.Token != req.cust_session_id)
            {
                result.error_code = (int)SkywindErrorCode.PlayerNotFound;
                logger.LogError($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(result)}\r\n TpUserName:{req.cust_id}");
                return result;
            }
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Skywind, req.cust_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Skywind, tpCustInfo.CurrencyId);
            var gameType = tpCustInfoBLL.GetCustGotoGameType(tpCustInfo.CustId, (int)Tp.Skywind);
            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, req.cust_id, gameType);

            result.error_code = (int)SkywindErrorCode.NoErrors;
            result.balance = Math.Truncate(tpCustBalance.Balance);
            result.currency_code = tpCurrency.TpCurrencyId;

            return result;
        }

        [Route("[controller]/api/debit")]
        [HttpPost]
        public DebitResp Debit(DebitReq req)
        {
            DebitResp result = new DebitResp();
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Skywind, req.cust_id);
            var gameType = tpCustInfoBLL.GetCustGotoGameType(tpCustInfo.CustId, (int)Tp.Skywind);

            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, req.cust_id);
            if (tpCheck == null)
            {
                result.error_code = (int)SkywindErrorCode.PlayerNotFound;
                result.error_msg = SkywindErrorCode.PlayerNotFound.ToString();
                logger.LogError($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(result)}");
                return result;
            }
            else if (tpCheck.Token != req.cust_session_id)
            {
                result.error_code = (int)SkywindErrorCode.GameTokenExpired;
                result.error_msg = SkywindErrorCode.GameTokenExpired.ToString();
                logger.LogError($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(result)}\r\n Token:{tpCheck.Token}");
                return result;
            }
            else if (tpCheck.Locked)
            {
                result.error_code = (int)SkywindErrorCode.PlayerNotFound;
                result.error_msg = SkywindErrorCode.PlayerNotFound.ToString();
                logger.LogError($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(result)}\r\n Locked");
                return result;
            }
            else if (req.amount < 0)
            {
                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, req.cust_id, gameType);
                result.balance = Math.Truncate(tpCustBalance.Balance);
                result.error_code = (int)SkywindErrorCode.InsufficientBalance;
                result.error_msg = SkywindErrorCode.InsufficientBalance.ToString();
                logger.LogError($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(result)}\r\n Stake < 0");
                return result;
            }
            else
            {
                BettingData_Skywind betReq = new BettingData_Skywind()
                {
                    merch_id = req.merch_id,
                    merch_pwd = req.merch_pwd,
                    cust_id = req.cust_id,
                    cust_session_id = req.cust_session_id,
                    round_id = req.round_id,
                    amount = req.amount,
                    currency_code = req.currency_code,
                    game_code = req.game_code,
                    trx_id = req.trx_id,
                    game_id = req.game_id,
                    event_type = req.event_type,
                    event_id = req.event_id,
                    timestamp = req.timestamp,
                    game_type = req.game_type,
                    platform = req.platform,
                    promo_id = req.promo_id,
                    promo_pid = req.promo_pid,
                    jp_contribution = req.jp_contribution,
                    operation_ts = req.operation_ts,
                    distribution_type = req.distribution_type,
                };

                if (bettingBLL.PlaceBet_Skywind(tpCustInfo, betReq, gameType, out decimal tpAffterAmount) > 0)
                {
                    result.balance = Math.Truncate(tpAffterAmount);
                    result.error_code = (int)SkywindErrorCode.NoErrors;
                    result.trx_id = req.trx_id;
                }
                else
                {
                    result.balance = Math.Truncate(tpAffterAmount);
                    result.error_code = (int)SkywindErrorCode.MerchantInternalError;
                    result.trx_id = req.trx_id;
                }
            }
            return result;
        }
        [Route("[controller]/api/credit")]
        [HttpPost]
        public CreditResp Credit(CreditReq req)
        {
            #region InitResult
            CreditResp result = new CreditResp();
            #endregion

            try
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Skywind, req.cust_id);
                var gameType = tpCustInfoBLL.GetCustGotoGameType(tpCustInfo.CustId, (int)Tp.Skywind);

                var tpCheck = thirdPartyBLL.ThirdPartyCheck((int)thisTp, req.cust_id);
                if (tpCheck == null || tpCheck.Token != req.cust_session_id)
                {
                    result.error_code = (int)SkywindErrorCode.PlayerNotFound;
                    logger.LogError($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(result)}\r\n TpUserName:{req.cust_id}");
                    return result;
                }

                BettingData_Skywind creditReq = new BettingData_Skywind()
                {
                    merch_id = req.merch_id,
                    merch_pwd = req.merch_pwd,
                    cust_id = req.cust_id,
                    cust_session_id = req.cust_session_id,
                    round_id = req.round_id,
                    amount = req.amount,
                    currency_code = req.currency_code,
                    game_code = req.game_code,
                    trx_id = req.trx_id,
                    game_id = req.game_id,
                    event_type = req.event_type,
                    event_id = req.event_id,
                    timestamp = req.timestamp,
                    game_type = req.game_type,
                    platform = req.platform,
                    promo_id = req.promo_id,
                    operation_ts = req.operation_ts,
                    distribution_type = req.distribution_type,
                    jp_win = req.jp_win ? 1 : 0,
                    game_status = req.game_status,
                    jp_ids = req.jp_ids == null ? null : JsonConvert.SerializeObject(req.jp_ids)
                };

                int SettleResult = settledBLL.Settled_Skywind(tpCustInfo, creditReq, req.cust_id, gameType, out decimal tpAffterAmount);

                if (SettleResult > 0)
                {
                    result.error_code = (int)SkywindErrorCode.NoErrors;
                    result.balance = Math.Truncate(tpAffterAmount);
                    result.trx_id = req.trx_id;
                    logger.LogInformation($"{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}");
                }
                else if (SettleResult == -1)
                {
                    result.error_code = (int)SkywindErrorCode.DuplicateTransaction;
                    result.balance = Math.Truncate(tpAffterAmount);
                    result.trx_id = req.trx_id;
                    logger.LogInformation($"{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}");
                }
                else
                {
                    result.error_code = (int)SkywindErrorCode.MerchantInternalError;
                    result.balance = Math.Truncate(tpAffterAmount);
                    result.trx_id = req.trx_id;
                    logger.LogError($"{JsonConvert.SerializeObject(result)}\r\nSettleResult:{SettleResult}");
                }
            }
            catch (Exception ex)
            {
                result.error_code = (int)SkywindErrorCode.MerchantInternalError;
                result.balance = 0;
                result.trx_id = req.trx_id;
                logger.LogError(ex, $"{JsonConvert.SerializeObject(result)}");
            }
            return result;
        }

        [Route("[controller]/api/rollback")]
        [HttpPost]
        public RollbackResp Rollback(RollbackReq req)
        {
            #region InitResult
            RollbackResp result = new RollbackResp();
            #endregion
            try
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Skywind, req.cust_id);
                var gameType = tpCustInfoBLL.GetCustGotoGameType(tpCustInfo.CustId, (int)Tp.Skywind);
                var tpCheck = thirdPartyBLL.ThirdPartyCheck((int)thisTp, req.cust_id);
                if (tpCheck == null || tpCheck.Token != req.cust_session_id)
                {
                    result.error_code = (int)SkywindErrorCode.PlayerNotFound;
                    logger.LogError($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(result)}\r\n TpUserName:{req.cust_id}");
                    return result;
                }

                CancelBetData_Skywind cancel = new CancelBetData_Skywind()
                {
                    merchid = req.merch_id,
                    merchpwd = req.merch_pwd,
                    custid = req.cust_id,
                    custsessionid = req.cust_session_id,
                    roundid = req.round_id,
                    currencycode = req.currency_code,
                    gamecode = req.game_code,
                    trxid = req.trx_id,
                    gameid = req.game_id,
                    eventtype = req.event_type,
                    timestamp = req.timestamp,
                    operationts = req.operation_ts
                };

                var cancelresult = settledBLL.CancelBet_Skywind(tpCustInfo, cancel, gameType, out decimal tpAffterAmount);

                if (cancelresult > 0)
                {
                    result.error_code = (int)SkywindErrorCode.NoErrors;
                    result.balance = Math.Truncate(tpAffterAmount);
                    result.trx_id = req.trx_id;
                    logger.LogInformation($"{JsonConvert.SerializeObject(result)}\r\n cancelresult:{cancelresult}");
                    return result;
                }
                else if (cancelresult == -1)
                {
                    result.error_code = (int)SkywindErrorCode.DuplicateTransaction;
                    result.balance = Math.Truncate(tpAffterAmount);
                    result.trx_id = req.trx_id;
                    logger.LogInformation($"{JsonConvert.SerializeObject(result)}\r\n cancelresult:{cancelresult}");
                    return result;
                }
                else if (cancelresult == -2)
                {
                    result.error_code = (int)SkywindErrorCode.TransactionNotFound;
                    result.balance = Math.Truncate(tpAffterAmount);
                    result.trx_id = req.trx_id;
                    logger.LogError($"{JsonConvert.SerializeObject(result)}\r\n cancelresult:{cancelresult}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.error_code = (int)SkywindErrorCode.MerchantInternalError;
                result.balance = 0;
                result.trx_id = req.trx_id;
                logger.LogError(ex, $"{JsonConvert.SerializeObject(result)}");
            }
            return result;
        }
        #endregion


    }
}
