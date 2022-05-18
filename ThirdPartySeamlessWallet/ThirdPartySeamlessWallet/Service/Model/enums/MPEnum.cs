using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{    
    public enum MPErrorCode
    {
        [Description("Success")]
        Success = 0,
        [Description("cannot find the corresponding account after decryption")]
        InvalidAccount = 11,
        [Description("Order does not exist")]
        InvalidOrder = 26,
        [Description("requestAmount is less than 0")]
        InvalidRequestAmount = 31,
        [Description("request exists and has been successfully processed previously")]
        DuplicateRequest = 34,
        [Description("totalWithdraw not equal to total withdraw amount")]
        InvalidWithdrawAmount = 36,
        [Description("requestAmount is greater than the wallet remaining balance")]
        BalanceInsufficient = 138,
        [Description("request is too frequent")]
        FrequentRequest = 146,
        [Description("When any other exception occurred")]
        Error = 500,
    }
}
