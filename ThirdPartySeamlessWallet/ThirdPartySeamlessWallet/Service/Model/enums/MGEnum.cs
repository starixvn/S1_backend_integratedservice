namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum MGTxnType
    {
        DEBIT,
        CREDIT
    }

    public enum MGErrorCode
    {
        TransactionNotExists,
        PlayerIsLocked,
        BalanceFailed,
        GameTypeError,
        InsufficientBalance,
        TransactionIncompatible,
        SystemError
    }
}