namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum SubTypeOfV8
    {
        GetBalance = 1001,
        RequestBet = 1002,
        ReturnBalance = 1003,
        GetOrderStatus = 1004,
        CancelBet = 1005,
    }

    // 当状态为 2 或 4 时，会进行重新派奬, 成功后修正订单为已处理
    public enum StatusTypeOfV8
    {
        // 成功
        Success = 1,
        // 失败
        Failure = 2,
        // 处理中
        Process = 3,
        // 查无订单
        NoResult = 4,
        // 其他错误
        OtherError = 5,
    }

    #region ErrorCode

    public enum ErrorCodeOfV8
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
    }

    #endregion

}
