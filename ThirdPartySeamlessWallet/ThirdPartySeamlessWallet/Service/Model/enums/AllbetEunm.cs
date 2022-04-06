using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum AllbetResultCode
    {
        Success = 0,
        InvalidOperatorId = 10000,
        InvalidSignature = 10001,
        UnknownAccount = 10003,
        BlockAccount = 10005,
        TranIdNotExists = 10006,
        [Description("invalid status: transaction had already been cancelled/settled")]
        InvalidCode = 10007,
        OfflineAccount = 10008,
        BlockBet = 10100,
        InsufficientBalance = 10101,
        UnderMaintenance = 10200,
        InvalidParameters = 40000,
        SystemError = 50000
    }
    
    public enum AllbetTransferType
    {
        Bet = 10,
        Cancel = 11,
        Settle = 20,
        ReSettle = 21,
        TransferIn = 30,
        TransferOut = 31,
        EventSettle = 40
    }

    public enum AllbetType
    {
        GetBalance,
        Transfer,
        CancelTransfer
    }
}