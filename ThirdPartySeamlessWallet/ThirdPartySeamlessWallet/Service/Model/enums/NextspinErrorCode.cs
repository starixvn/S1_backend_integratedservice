using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum NextspinErrorCode
    {
        [Description("Success")]
        Success = 0,
        [Description("System Error")]
        SystemError = 1,
        [Description("Invalid Request")]
        InvalidRequest = 2,
        [Description("Service Inaccessible")]
        ServiceInaccessible = 3,
        [Description("Missing Parameters")]
        MissingParameters = 105,
        [Description("Invalid Parameters")]
        InvalidParameters = 106,
        [Description("Duplicated Serial No")]
        DuplicatedSerialNo = 107,
        [Description("Merchant Key Error")]
        MerchantKeyError = 108,
        [Description("Duplicated ReferenceId")]
        DuplicatedReferenceId = 109,
        [Description("Record Id Not Found")]
        RecordIdNotFound = 110,
        [Description("Invalid Acct Id")]
        InvalidAcctId = 113,
        [Description("Acct Not Found")]
        AcctNotFound = 50100,
        [Description("Acct Inactive")]
        AcctInactive = 50101,
        [Description("Acct Locked")]
        AcctLocked = 50102,
        [Description("Insufficient Balance")]
        InsufficientBalance = 50110,
        [Description("Currency Invalid")]
        CurrencyInvalid = 50112,
        [Description("Amount Invalid")]
        AmountInvalid = 50113
    }
}
