namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum IMResultCode
    {
        Success = 0,
        PlayerDoesNotExist = 504,
        NotEnoughBalance = 510,
        PlayerIsInactive = 542,
        TransactionIdNotFound = 545,
        InvalidArgument = 612,
        Exception = 999
    }

    public enum IMActionId
    {
        PlaceBetRefund = 1005,
        DangerRefund = 2001,
        LiveBallRefund = 2011,
        BTBuyBack = 3001,
        BTCancelBuyBack = 3002,
        BTRestoreBuyBack = 3003,
        LiveBallRefundRevert = 3011,
        SettleHT = 4001,
        SettleFT = 4002,
        SettleOutright = 4003,
        SettleParlay = 4004,
        SettleOnHold = 4005,
        UnsettleHT = 5001,
        UnsettleFT = 5002,
        UnsettleOutright = 5003,
        UnsettleParlay = 5004,
        UnsettleOnHold = 5005,
        CancelHT = 6001,
        CancelFT = 6002,
        CancelOutright = 6003,
        CancelSingleWager = 6011,
        CancelParlay = 6014,
        UncancelHT = 7001,
        UncancelFT = 7002,
        UncancelOutright = 7003,
        UncancelSingleWager = 7011,
        UncancelParlay = 7014,
        Undefined = 0
    }
}