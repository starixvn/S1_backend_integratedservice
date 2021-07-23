using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.Model
{

    public class TCGApiMethod
    {
        public const string GetBalance = "sgb";
        public const string Debit = "db";
        public const string Credit = "cr";
        public const string Reserve = "rsv";
        public const string CancelReserve = "cnrsv";
        public const string ConfirmReserve = "crsv";
        public const string CheckTransactionStatus = "scs";
    }

    public enum TCGTransType
    { 
        Debit,
        Credit,
        Reserve,
    }

    public class TCGReturnCode
    {
        public const int Success = 0;
        public const int UnknownSystemError = 1;
        public const int MissingRequiredParameter = 2;
        public const int MethodNotSupported = 3;
        public const int MerchantNotAllowedProductType = 4;
        public const int MerchantNotFound = 5;
        public const int InvalidParameters = 6;
        public const int InvalidSignature = 7;
        public const int UnSupportedCurrency = 8;
        public const int InvalidAccountType = 9;
        public const int InvalidProducttype = 10;
        public const int InsufficientBalance = 11;
        public const int TransactionAlreadyExists = 12;
        public const int Invalidgamecode = 13;
        public const int UserDoesNotExists = 15;
        public const int Insufficientmerchantcredittofundin = 16;
        public const int Trialmodeisnotsupportedforthisgamecode = 18;
        public const int Batchnotready = 19;
        public const int MethodNotFound = 21;
        public const int ParameterValidationFailed = 22;
        public const int APIisbusy = 23;
        public const int TransactionNotFound = 24;
        public const int Reservationalreadyprocess = 25;
        public const int Decimalnotsupported = 26;
        public const int APIisundermaintenance = 27;
        public const int APIisprohibitedforthismerchant = 28;
        public const int Productisundermaintenance = 30;
    }

    public class TCGConfig
    {
        public string GameUrl { get; set; }
        public string ApiUrl { get; set; }
        public string MerchantCode { get; set; }
        public string DesKey { get; set; }
        public string SignKey { get; set; }
        public TCGConfig(Dictionary<string, string> inputs, string merchant_code)
        {
            try
            {
                var key = inputs.GetKey(merchant_code);
                var currency = key.Split("_")[1];
                GameUrl = inputs.GetValue($"GameUrl_{currency}");
                ApiUrl = inputs.GetValue("ApiUrl");
                MerchantCode = inputs.GetValue($"MerchantCode_{currency}");
                DesKey = inputs.GetValue($"DESKey_{currency}");
                SignKey = inputs.GetValue($"SHA256Key_{currency}");
            }
            catch (Exception ex)
            {
                throw new TCGException(TCGReturnCode.MerchantNotFound, ex.Message);
            }
        }
    }

    public class TCGReturnObj
    {
        public string status { get; set; }

        public string error_desc { get; set; }
    }

    public class TCGBalanceObj : TCGReturnObj
    {
        public string balance { get; set; }
    }

    public class TCGCheckObj : TCGReturnObj
    {
        public IEnumerable<TCGQueryResult> query_result { get; set; }
    }

    public class TCGQueryResult
    {
        public string ref_no { get; set; }
        public string trans_status { get; set; }

        public TCGQueryResult(string ref_no, TCGBettransLottery bettransLottery)
        {
            this.ref_no = ref_no;
            if (bettransLottery == null)
            {
                trans_status = "NOT_FOUND";
                return;
            }

            if (bettransLottery.TransType == TCGTransType.Credit.ToString() 
                || bettransLottery.TransType == TCGTransType.Debit.ToString())
            {
                trans_status = "SUCCESS";
                return;
            }

            if (bettransLottery.TransType == TCGTransType.Reserve.ToString())
            {
                switch (bettransLottery.TicketType)
                {
                    case (int)BettransType.CancelBet:
                        trans_status = "CANCELLED";
                        break;
                    case (int)BettransType.ConfirmReserve:
                        trans_status = "SUCCESS";
                        break;
                    case (int)BettransType.Reserve:
                        trans_status = "RESERVED";
                        break;
                    default:
                        trans_status = "FAILED";
                        break;
                }
                return;
            }
            trans_status = "FAILED";
        }
    }

    public class TCGBettransLottery
    {
        public string username { get; set; }
        public string ref_no { get; set; }
        public decimal amount { get; set; }
        public string TransType { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }
    }

    public class TCGResult
    {

        public int Result { get; set; } = 1;

        public string Message { get; set; }
    }

    public class TCGRequest
    {
        public string method { get; set; }
        
        public string username { get; set; }

        public int product_type { get; set; }

        public List<TCGTransactionData> transactions { get; set; }

        public long request_time { get; set; }

        public List<string> ref_nos { get; set; }

        public List<TCGReservationDetail> reservation_details { get; set; }

        public List<TCGTransactionInfo> transaction_details { get; set; }
    }

    public class TCGTransactionData
    {
        private string _order_no { get; set; } = "";
        public string ref_no { get; set; }
        public string username { get; set; }
        public decimal amount { get; set; }
        public string tx_type { get; set; }
        public string remarks { get; set; }

        public string order_no 
        { 
            get 
            {
                var result = additionalInformation["order_no"];
                return result == null ? _order_no : result;
            }
            set { _order_no = value; } 
        }
        public dynamic additionalInformation { get; set; }
    }

    public class TCGReservationDetail
    {
        private string _order_no { get; set; } = "";
        public decimal amount { get; set; }
        public string ref_no { get; set; }
        public string username { get; set; }
        public string order_no
        {
            get
            {
                var result = additionalInformation["order_no"];
                return result == null ? _order_no : result;
            }
            set { _order_no = value; }
        }
        public dynamic additionalInformation { get; set; }
    }

    public class TCGTransactionInfo
    {
        public decimal amount { get; set; }
        public string ref_no { get; set; }
        public string username { get; set; }
        public dynamic additionalInformation { get; set; }
    }

    public class TCGSettleData : SettleData
    {
        public string OrderNum { get; set; }
    }
}
