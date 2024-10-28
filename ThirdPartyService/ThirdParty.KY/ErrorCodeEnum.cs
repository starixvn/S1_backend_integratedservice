namespace ThirdParty.KY
{
    public enum ErrorCodeEnum
    {
        DBErrorRollback = -1,
        Success = 0,
        TokenLoss = 1,
        ChannelNonExistent = 2,
        AuthenticationTimeout = 3,
        AuthenticationError = 4,
        ChannelWhiteListError = 5,
        AuthenticationFieldLoss = 6,
        TokenFailure = 7,
        NonExistentRequest = 8,
        AccountNonExistence =11,
        ChannelAuthenticaionError = 15,
        DataNonExistence = 16,
        AccountForbidden = 20,
        DESDecryptFailure = 22,
        ChannelPullsDataOvertime = 24,
    }
}
