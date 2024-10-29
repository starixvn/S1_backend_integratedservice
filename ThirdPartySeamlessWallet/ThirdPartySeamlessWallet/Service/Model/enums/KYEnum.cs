namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum SubTypeOfKY
    {
        GetBalance = 1001,
        RequestBet = 1002,
        ReturnBalance = 1003,
        GetOrderStatus = 1004,
        CancelBet = 1005,
    }

    #region ErrorCode

    public enum ErrorCodeOfKY
    {
        Success = 0,
        // Insufficient balance
        Insufficient = 1,
        // Account does not exist
        Account = 2,
        // maintains
        Maintains = 3,
        // token validation error
        Token = 4,
        // data format error
        Format = 5,
        // decryption error
        Decryption = 6,
        // MD5 key error
        Md5 = 7,
        // unknown sub-type operation
        Subtype = 8,
        // Duplicate order
        Duplicate = 9,
        // Agent does not exist
        Agent = 10,
        // Account under deposit or withdrawn
        Transaction = 11,
        // Order does not exist
        Order = 12,
        // Internal server error
        Server = 13,

    }

    #endregion
}
