using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class GPIController : ControllerBase
    {
        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly Log2File _log2File;
        private readonly Tp _thisTp;
        private readonly string _tpName = Tp.GPI.ToString();

        public GPIController(IServiceProvider provider)
        {
            _log2File = provider.GetRequiredService<Log2File>();
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thisTp = Tp.GPI;
        }

        #region Request

        public class GetbalanceReq
        {
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
            public string cust_id { get; set; }
            public string currency { get; set; }
            public string Operator { get; set; }
            public string game_id { get; set; }
            public string platform { get; set; }
        }

        public class DebitReq : BettingDataRNG_GPI
        {
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
        }

        public class CreditReq : SettledDataRNG_GPI
        {
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
        }

        public class VoidBetReq : BettingDataRNG_GPI
        {
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
        }

        public class EditBetReq : BettingDataRNG_GPI
        {
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
        }

        public class CancelBetReq : CancelBet_GPI
        {
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
        }

        public class EndgameReq : EndgameRNG_GPI
        {
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
        }

        #endregion

        #region Response

        [XmlRoot(ElementName = "resp")]
        public class ValidateTicketResp
        {
            [XmlElement(ElementName = "error_code")]
            public int Error_code { get; set; }
            [XmlElement(ElementName = "error_msg")]
            public string Error_msg { get; set; }
            [XmlElement(ElementName = "cust_id")]
            public string Cust_id { get; set; }
            [XmlElement(ElementName = "cust_name")]
            public string Cust_name { get; set; }
            [XmlElement(ElementName = "currency_code")]
            public string Currency_code { get; set; }
            [XmlElement(ElementName = "language")]
            public string Language { get; set; }
            [XmlElement(ElementName = "test_cust")]
            public bool Test_cust { get; set; }
            [XmlElement(ElementName = "country")]
            public string Country { get; set; }
            [XmlElement(ElementName = "date_of_birth")]
            public string Date_of_birth { get; set; }
            [XmlElement(ElementName = "ip")]
            public string Ip { get; set; }
        }

        [XmlRoot(ElementName = "resp")]
        public class GetbalanceResp
        {
            [XmlElement(ElementName = "error_code")]
            public int Error_code { get; set; }
            [XmlElement(ElementName = "error_msg")]
            public string Error_msg { get; set; }
            [XmlElement(ElementName = "balance")]
            public decimal Balance { get; set; }
        }

        [XmlRoot(ElementName = "resp")]
        public class DebitResp
        {
            [XmlElement(ElementName = "error_code")]
            public int Error_code { get; set; }
            [XmlElement(ElementName = "error_msg")]
            public string Error_msg { get; set; }
            [XmlElement(ElementName = "before")]
            public decimal Before { get; set; }
            [XmlElement(ElementName = "after")]
            public decimal After { get; set; }
            [XmlElement(ElementName = "exchange_rate")]
            public decimal Exchange_rate { get; set; }
            [XmlElement(ElementName = "trx_id")]
            public string Trx_id { get; set; }
        }

        [XmlRoot(ElementName = "resp")]
        public class CreditResp
        {
            [XmlElement(ElementName = "error_code")]
            public int Error_code { get; set; }
            [XmlElement(ElementName = "error_msg")]
            public string Error_msg { get; set; }
            [XmlElement(ElementName = "before")]
            public decimal Before { get; set; }
            [XmlElement(ElementName = "after")]
            public decimal After { get; set; }
            [XmlElement(ElementName = "exchange_rate")]
            public decimal Exchange_rate { get; set; }
            [XmlElement(ElementName = "trx_id")]
            public string Trx_id { get; set; }
        }

        [XmlRoot(ElementName = "resp")]
        public class VoidBetResp : CreditResp
        {
        }

        [XmlRoot(ElementName = "resp")]
        public class EditBetResp : CreditResp
        {
        }

        [XmlRoot(ElementName = "resp")]
        public class CancelBetResp : CreditResp
        {
        }

        [XmlRoot(ElementName = "resp")]
        public class EndgameResp
        {
            [XmlElement(ElementName = "error_code")]
            public int Error_code { get; set; }
            [XmlElement(ElementName = "error_msg")]
            public string Error_msg { get; set; }
        }

        #endregion

        [Route("[controller]/ms/validateticket")]
        [HttpPost]
        [Produces("application/xml")]
        public ValidateTicketResp Validateticket([FromForm] string ticket)
        {
            string logName = $"{_tpName}_validateticket";

            ValidateTicketResp validateTicketResp = new();
            if (_authBLL.Authenticate(ticket, out TpCustInfoAuth tpCustInfoAuth))
            {
                var tpCountryCode = _tpCustInfoBLL.GetTpCurrencyCodeByCurrencyId((int)_thisTp, tpCustInfoAuth.TpCurrencyId);
                if (tpCountryCode == null)
                {
                    validateTicketResp.Error_code = -3;
                    validateTicketResp.Error_msg = "Authentication Failed";
                    _log2File.Error($"TpCountryCodeError\r\n{JsonConvert.SerializeObject(validateTicketResp)}", logName);
                    return validateTicketResp;
                }

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, tpCustInfoAuth.TpUserName);
                if (tpCheck == null)
                {
                    validateTicketResp.Error_code = -3;
                    validateTicketResp.Error_msg = "Authentication Failed";
                }
                else if (tpCheck.Locked)
                {
                    validateTicketResp.Error_code = -3;
                    validateTicketResp.Error_msg = "Authentication Failed";
                }
                else
                {
                    validateTicketResp.Error_code = 0;
                    validateTicketResp.Cust_id = tpCustInfoAuth.TpUserName;
                    validateTicketResp.Cust_name = tpCustInfoAuth.TpUserName;
                    validateTicketResp.Currency_code = tpCustInfoAuth.TpCurrencyId;
                    validateTicketResp.Country = tpCountryCode.CountryCode;
                    validateTicketResp.Language = tpCustInfoAuth.TpLangId;
                    validateTicketResp.Test_cust = tpCustInfoAuth.IsTest;
                    validateTicketResp.Date_of_birth = "30-7-1970";
                    validateTicketResp.Ip = tpCustInfoAuth.ClientIP;

                    _log2File.Info(JsonConvert.SerializeObject(validateTicketResp), logName);
                    return validateTicketResp;
                }
            }
            else
            {
                validateTicketResp.Error_code = -3;
                validateTicketResp.Error_msg = "Authentication Failed";
            }

            _log2File.Error(JsonConvert.SerializeObject(validateTicketResp), logName);
            return validateTicketResp;
        }

        [Route("[controller]/ms/getbalance")]
        [HttpPost]
        [Produces("application/xml")]
        public GetbalanceResp Getbalance([FromForm] GetbalanceReq getbalanceReq)
        {
            string logName = $"{_tpName}_getbalance";

            GetbalanceResp getbalanceResp = new();

            var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, getbalanceReq.game_id);
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getbalanceReq.cust_id, (int)gameType);
            if (getbalanceReq.currency != tpCustBalance.TpCurrencyId)
            {
                getbalanceResp.Error_code = -29;
                getbalanceResp.Error_msg = "Currency mismatch";
            }
            else
            {
                getbalanceResp.Balance = tpCustBalance.Balance;
                _log2File.Info($"{JsonConvert.SerializeObject(getbalanceReq)}\r\n{JsonConvert.SerializeObject(getbalanceResp)}", logName);
                return getbalanceResp;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(getbalanceReq)}\r\n{JsonConvert.SerializeObject(getbalanceResp)}", logName);
            return getbalanceResp;
        }

        [Route("[controller]/ms/debit")]
        [HttpPost]
        [Produces("application/xml")]
        public DebitResp Debit([FromForm] DebitReq debitReq)
        {
            string logName = $"{_tpName}_debit";

            DebitResp debitResp = new()
            {
                Error_code = -1,
                Error_msg = "Unknown error"
            };

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, debitReq.cust_id);
            if (tpCheck == null)
            {
                debitResp.Error_code = -7;
                debitResp.Error_msg = "Customer is blocked";
            }
            else if (tpCheck.Locked)
            {
                debitResp.Error_code = -7;
                debitResp.Error_msg = "Customer is blocked";
            }
            else
            {
                try
                {
                    var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, debitReq.game_id);
                    int result = (gameType == GameType.GPI_RNG) ? _bettingBLL.PlaceBetRNG_GPI(debitReq, out decimal tpAffterAmount, out string TransId) : _bettingBLL.PlaceBetLT_GPI(debitReq, (int)gameType, out tpAffterAmount, out TransId);

                    if (result > 0)
                    {
                        debitResp.Error_code = 0;
                        debitResp.Error_msg = "No errors";
                        if (debitReq.amount == 0)
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, debitReq.cust_id, (int)gameType);
                            debitResp.After = tpCustBalance.Balance;
                            debitResp.Before = debitResp.After;
                        }
                        else
                        {
                            debitResp.After = tpAffterAmount;
                            debitResp.Before = tpAffterAmount - debitReq.amount;
                        }
                        debitResp.Exchange_rate = 1;
                        debitResp.Trx_id = TransId;

                        _log2File.Info($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(debitResp)}", logName);
                        return debitResp;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        debitResp.Error_code = -4;
                        debitResp.Error_msg = "Insufficient balance";
                    }
                    else
                    {
                        debitResp.Error_code = 1;
                        debitResp.Error_msg = "Transaction already exists";
                    }
                }
                catch (Exception ex)
                {
                    debitResp.Error_code = -1;
                    debitResp.Error_msg = "Unknown error";
                    _log2File.Error($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(debitResp)}\r\n{ex}", logName, ex);
                    return debitResp;
                }
            }

            _log2File.Error($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(debitResp)}", logName);
            return debitResp;
        }

        [Route("[controller]/ms/credit")]
        [HttpPost]
        [Produces("application/xml")]
        public CreditResp Credit([FromForm] CreditReq creditReq)
        {
            string logName = $"{_tpName}_credit";

            CreditResp creditResp = new()
            {
                Exchange_rate = 1,
                Error_code = -1
            };

            try
            {
                var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, creditReq.game_id);
                if (gameType != GameType.GPI_RNG && creditReq.amount == 0)
                {
                    // Live Casino & Lottery system do not send credit request to seamless wallet endpoint with amount = 0
                    creditResp.Error_code = -33;
                    creditResp.Error_msg = "Invalid amount";
                    _log2File.Error($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(creditResp)}", logName);
                    return creditResp;
                }
                int result = (gameType == GameType.GPI_RNG) ? _settledBLL.PayoutRNG_GPI(creditReq, out decimal tpAffterAmount, out string TransId) : _settledBLL.PayoutLT_GPI(creditReq, (int)gameType, out tpAffterAmount, out TransId);

                if (result > 0)
                {
                    creditResp.Trx_id = TransId;
                    if (creditReq.amount > 0)
                    {
                        creditResp.After = tpAffterAmount;
                        creditResp.Before = tpAffterAmount - creditReq.amount;
                        creditResp.Error_code = 0;
                        creditResp.Error_msg = "No errors";

                        _log2File.Info($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(creditResp)}", logName);
                        return creditResp;
                    }
                    else if (creditReq.amount == 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, creditReq.cust_id, (int)gameType);
                        creditResp.After = tpCustBalance.Balance;
                        creditResp.Before = tpCustBalance.Balance;
                        creditResp.Error_code = 0;
                        creditResp.Error_msg = "No errors";

                        _log2File.Info($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(creditResp)}", logName);
                        return creditResp;
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, creditReq.cust_id, (int)gameType);
                    creditResp.After = tpCustBalance.Balance;
                    creditResp.Before = tpCustBalance.Balance;
                    creditResp.Error_code = 0;
                    creditResp.Error_msg = "No errors";

                    _log2File.Info($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(creditResp)}", logName);
                    return creditResp;
                }
            }
            catch (Exception ex)
            {
                creditResp.Error_code = -1;
                creditResp.Error_msg = "Unknown error";
                _log2File.Error($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(creditResp)}\r\n{ex}", logName, ex);
                throw;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(creditResp)}", logName);
            return creditResp;
        }

        [Route("[controller]/ms/changeBalance")]
        [HttpPost]
        [Produces("application/xml")]
        public VoidBetResp ChangeBalance([FromForm] VoidBetReq voidBetReq)
        {
            string logName = $"{_tpName}_changeBalance";

            VoidBetResp voidBetResp = new()
            {
                Exchange_rate = 1,
                Error_code = -1
            };

            try
            {
                var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, voidBetReq.game_id);
                if (gameType == GameType.GPI_RNG)
                {
                    voidBetResp.Error_msg = "Unknown error(not support slot)";
                    _log2File.Error($"{JsonConvert.SerializeObject(voidBetReq)}\r\n{JsonConvert.SerializeObject(voidBetResp)}", logName);
                    return voidBetResp;
                }
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, voidBetReq.cust_id, (int)gameType);
                int result = _settledBLL.VoidBetLT_GPI(voidBetReq, (int)gameType, out decimal tpAffterAmount, out string TransId);

                if (result > 0)
                {
                    voidBetResp.Trx_id = TransId;
                    voidBetResp.After = (voidBetReq.amount == 0) ? tpCustBalance.Balance : tpAffterAmount;
                    voidBetResp.Before = (voidBetReq.amount == 0) ? tpCustBalance.Balance : tpAffterAmount - voidBetReq.amount;
                    voidBetResp.Error_code = 0;
                    voidBetResp.Error_msg = "No errors";

                    _log2File.Info($"{JsonConvert.SerializeObject(voidBetReq)}\r\n{JsonConvert.SerializeObject(voidBetResp)}", logName);
                    return voidBetResp;
                }
                else if (result == -1)
                {
                    voidBetResp.After = tpCustBalance.Balance;
                    voidBetResp.Before = tpCustBalance.Balance;
                    voidBetResp.Error_code = 1;
                    voidBetResp.Error_msg = "Transaction already exists";
                }
                else if (result == -2)
                {
                    voidBetResp.After = tpCustBalance.Balance;
                    voidBetResp.Before = tpCustBalance.Balance;
                    voidBetResp.Error_code = -201;
                    voidBetResp.Error_msg = "Transaction not exist";
                }
            }
            catch (Exception ex)
            {
                voidBetResp.Error_code = -1;
                voidBetResp.Error_msg = "Unknown error";
                _log2File.Error($"{JsonConvert.SerializeObject(voidBetReq)}\r\n{JsonConvert.SerializeObject(voidBetResp)}\r\n{ex}", logName, ex);
                throw;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(voidBetReq)}\r\n{JsonConvert.SerializeObject(voidBetResp)}", logName);
            return voidBetResp;
        }

        [Route("[controller]/ms/editResult")]
        [HttpPost]
        [Produces("application/xml")]
        public EditBetResp EditResult([FromForm] EditBetReq editBetReq)
        {
            // Gameplay，上午 10:50 是的，目前彩票类暂不使用此API
            string logName = $"{_tpName}_editResult";

            EditBetResp editBetResp = new()
            {
                Exchange_rate = 1,
                Error_code = -1
            };
            _log2File.Error($"{JsonConvert.SerializeObject(editBetReq)}\r\n{JsonConvert.SerializeObject(editBetResp)}", logName);
            return editBetResp;
            /*
            try
            {
                var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, editBetReq.game_id);
                
                if (gameType == GameType.GPI_RNG)
                {
                    editBetResp.Error_msg = "Unknown error(not support slot)";
                    _log2File.Error($"{JsonConvert.SerializeObject(editBetReq)}\r\n{JsonConvert.SerializeObject(editBetResp)}", logName);
                    return editBetResp;
                }
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, editBetReq.cust_id, (int)gameType);
                int result = _settledBLL.EditBetLT_GPI(editBetReq, (int)gameType, out decimal tpAffterAmount, out string TransId);

                if (result > 0)
                {
                    editBetResp.Trx_id = TransId;
                    editBetResp.After = (editBetReq.amount == 0) ? tpCustBalance.Balance : tpAffterAmount;
                    editBetResp.Before = (editBetReq.amount == 0) ? tpCustBalance.Balance : tpAffterAmount - editBetReq.amount;
                    editBetResp.Error_code = 0;
                    editBetResp.Error_msg = "No errors";

                    _log2File.Info($"{JsonConvert.SerializeObject(editBetReq)}\r\n{JsonConvert.SerializeObject(editBetResp)}", logName);
                    return editBetResp;
                }
                else if (result == -1)
                {
                    editBetResp.After = tpCustBalance.Balance;
                    editBetResp.Before = tpCustBalance.Balance;
                    editBetResp.Error_code = 1;
                    editBetResp.Error_msg = "Transaction already exists";
                }
                else if (result == -2)
                {
                    editBetResp.After = tpCustBalance.Balance;
                    editBetResp.Before = tpCustBalance.Balance;
                    editBetResp.Error_code = -201;
                    editBetResp.Error_msg = "Transaction not exist";
                }
            }
            catch (Exception ex)
            {
                editBetResp.Error_code = -1;
                editBetResp.Error_msg = "Unknown error";
                _log2File.Error($"{JsonConvert.SerializeObject(editBetReq)}\r\n{JsonConvert.SerializeObject(editBetResp)}\r\n{ex}", logName, ex);
                throw;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(editBetReq)}\r\n{JsonConvert.SerializeObject(editBetResp)}", logName);
            return editBetResp;
            */
        }

        [Route("[controller]/ms/rollback")]
        [HttpPost]
        [Produces("application/xml")]
        public CancelBetResp Rollback([FromForm] CancelBetReq cancelBetReq)
        {
            string logName = $"{_tpName}_rollback";

            CancelBetResp cancelBetResp = new()
            {
                Exchange_rate = 1,
                Error_code = -1
            };

            try
            {
                var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, cancelBetReq.game_id);
                if (gameType == GameType.GPI_RNG)
                {
                    cancelBetResp.Error_msg = "Unknown error(not support slot)";
                    _log2File.Error($"{JsonConvert.SerializeObject(cancelBetReq)}\r\n{JsonConvert.SerializeObject(cancelBetResp)}", logName);
                    return cancelBetResp;
                }
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, cancelBetReq.cust_id, (int)gameType);
                int result = _settledBLL.CancelBetLT_GPI(cancelBetReq, (int)gameType, out decimal tpAffterAmount, out string TransId);

                if (result > 0)
                {
                    cancelBetResp.Trx_id = TransId;
                    cancelBetResp.After = tpAffterAmount;
                    cancelBetResp.Before = tpCustBalance.Balance;
                    cancelBetResp.Error_code = 0;
                    cancelBetResp.Error_msg = "No errors";

                    _log2File.Info($"{JsonConvert.SerializeObject(cancelBetReq)}\r\n{JsonConvert.SerializeObject(cancelBetResp)}", logName);
                    return cancelBetResp;
                }
                else if (result == -1)
                {
                    cancelBetResp.After = tpCustBalance.Balance;
                    cancelBetResp.Before = tpCustBalance.Balance;
                    cancelBetResp.Error_code = 1;
                    cancelBetResp.Error_msg = "Transaction already exists";
                }
                else if (result == -2)
                {
                    cancelBetResp.After = tpCustBalance.Balance;
                    cancelBetResp.Before = tpCustBalance.Balance;
                    cancelBetResp.Error_code = -201;
                    cancelBetResp.Error_msg = "Transaction not exist";
                }
            }
            catch (Exception ex)
            {
                cancelBetResp.Error_code = -1;
                cancelBetResp.Error_msg = "Unknown error";
                _log2File.Error($"{JsonConvert.SerializeObject(cancelBetReq)}\r\n{JsonConvert.SerializeObject(cancelBetResp)}\r\n{ex}", logName, ex);
                throw;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(cancelBetReq)}\r\n{JsonConvert.SerializeObject(cancelBetResp)}", logName);
            return cancelBetResp;
        }

        [Route("[controller]/ms/endgame")]
        [HttpPost]
        [Produces("application/xml")]
        public EndgameResp Endgame([FromForm] EndgameReq endgameReq)
        {
            string logName = $"{_tpName}_endgame";

            EndgameResp endgameResp = new()
            {
                Error_code = -1,
                Error_msg = "Unknown error"
            };

            try
            {
                var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, endgameReq.game_id);
                _tpUnSettledBLL.InsertTpUnsettle(_thisTp, gameType, endgameReq.round_id);
            }
            catch (Exception ex)
            {
                endgameResp.Error_code = -1;
                endgameResp.Error_msg = "Unknown error";
                _log2File.Error($"{JsonConvert.SerializeObject(endgameReq)}\r\n{JsonConvert.SerializeObject(endgameResp)}\r\n{ex}", logName, ex);
                throw;
            }

            endgameResp.Error_code = 0;
            endgameResp.Error_msg = "No errors";

            string endgameLog = $"{JsonConvert.SerializeObject(endgameReq)}\r\n{JsonConvert.SerializeObject(endgameResp)}";
            _log2File.Info(endgameLog, logName);

            return endgameResp;
        }

    }
}