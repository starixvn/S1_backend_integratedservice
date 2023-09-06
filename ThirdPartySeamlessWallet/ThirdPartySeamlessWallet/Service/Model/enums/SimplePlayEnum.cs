using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum SimplePlayErrorCode
    {
        Success = 0,
        CustNotExists = 1000,
        CurrencyInvaild = 1001,
        AmountInvaild = 1002,
        CustLocked = 1003,
        BalanceNotEnough = 1004,
        Error = 1005,
        DecryptInvaild = 1006,
        SessionExpird = 1007,
        SystemError = 9999
    }
}
