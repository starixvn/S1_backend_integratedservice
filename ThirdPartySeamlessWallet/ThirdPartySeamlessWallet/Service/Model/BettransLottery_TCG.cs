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

    public class TCGErrorCode
    {
        public const string Success = "0";
        public const string UnknownSystemError = "1";
        public const string MissingRequiredParameter = "2";
        public const string MethodNotSupported = "3";
        public const string MerchantNotAllowedProductType = "4";
        public const string MerchantNotFound = "5";
        public const string InvalidParameters = "6";
        public const string InvalidSignature = "7";
        public const string UnSupportedCurrency = "8";
        public const string InvalidAccountType = "9";
        public const string InvalidProducttype = "10";
        public const string Insufficientbalancetofundoutwithdraw = "11";
        public const string Transactionalreadyexists = "12";
        public const string Invalidgamecode = "13";
        public const string UserDoesNotExists = "15";
        public const string Insufficientmerchantcredittofundin = "16";
        public const string Trialmodeisnotsupportedforthisgamecode = "18";
        public const string Batchnotready = "19";
        public const string Methodnotfound = "21";
        public const string ParameterValidationFailed = "22";
        public const string APIisbusy = "23";
        public const string Transactionnotfound = "24";
        public const string Reservationalreadyprocess = "25";
        public const string Decimalnotsupported = "26";
        public const string APIisundermaintenance = "27";
        public const string APIisprohibitedforthismerchant = "28";
        public const string Productisundermaintenance = "30";
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
                GameUrl = inputs.GetValue("GameUrl");
                ApiUrl = inputs.GetValue("ApiUrl");
                MerchantCode = inputs.GetValue($"MerchantCode_{currency}");
                DesKey = inputs.GetValue($"DESKey_{currency}");
                SignKey = inputs.GetValue($"SHA256Key_{currency}");
            }
            catch (Exception ex)
            {
                throw new TCGException(TCGErrorCode.MerchantNotFound, ex.Message);
            }
        }
    }

    public class TCGBaseResult
    {
        public string status { get; set; }

        public string error_desc { get; set; }
    }

    public class TCGRequest
    {
        public string method { get; set; }
        
        public string username { get; set; }

        public int product_type { get; set; }

        public List<TransactionData> transactions { get; set; }

        public long request_time { get; set; }

        public List<string> ref_nos { get; set; }

        public List<TransactionData> reservation_details { get; set; }

        public List<TransactionInfo> transaction_details { get; set; }
    }

    public class TransactionData
    {
        public string ref_no { get; set; }
        public string username { get; set; }
        public decimal amount { get; set; }
        public string tx_type { get; set; }
        public string remarks { get; set; }
        public dynamic additionalInformation { get; set; }
    }

    public class ReservationDetail
    {
        public decimal amount { get; set; }
        public string ref_no { get; set; }
        public string username { get; set; }       
        public dynamic additionalInformation { get; set; }
    }

    public class TransactionInfo
    {
        public decimal amount { get; set; }
        public string ref_no { get; set; }
        public string username { get; set; }
        public dynamic additionalInformation { get; set; }
    }
}
