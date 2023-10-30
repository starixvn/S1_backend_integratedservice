namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum PegasusResultCode
    {
        Success = 0,
        PlayerDoesNotExist = 404001,
        TransactionIdNotFound = 404002,
        NotEnoughBalance = 404003,
        PlayerIsInactive = 403601,
        InvalidArgument = 403001,
        Duplicate = 500029,
        Exception = 500001
    }
}