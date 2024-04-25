using System.ComponentModel;

namespace ThirdPartyCore.Model.enums
{
    public enum ErrorCode : int
    {
        Success = 0,
        #region api response fail (1xxxx)
        Fail = 1000,
        NoAuthority = 1001,
        KEY_ERROR = 1002,
        NETWORK_ERROR = 1003,
        OTHER_ERROR = 1999,
        // Account 
        UserNotExist = 1101,
        UserLocked = 1102,
        UserNotLogin = 1103,
        UserDuplicatedLogin = 1104,
        //UserAGGroupDisable = 1105,
        //IBCAcountNotExist = 1106,
        // transfer fail (12xxx)
        TransferFail = 1200,
        MoneyNotEnough = 1201,
        DuplicateTransID = 1202,
        AmountOutOfRange = 1203,
        TransferNotExist = 1204,// Check Transfer
        TransferPending = 1205,// Check Transfer
        TransferWaitManualCheck = 1206,// Check Transfer
        DecimalOutOfRange = 1207,// 小數超出支援範圍
        // auto transfer (13xxx)
        AutoTransferFail = 1300,
        AutoTransferIsOff = 1301,
        AutoTransferIsSeamless = 1302,
        AutoTransferBonusFail = 1303,
        // Maintain
        ThirdPartyUnderMaintenance = 1400,
        #endregion
        #region invalid parameter  (2xxxx)
        InvalidParameter = 2000,
        InvalidUserName = 2001,
        InvalidCurrency = 2002,
        InvalidLanguage = 2003,
        #endregion

        #region BetDetail (3xxxx)
        BetDetailNotFound = 3000,
        GetBetDetailNotImplemented = 3001,
        BetDetailIsUrl = 3002,
        TpBetRecordExtendNotFound = 3003,
        GetTpBetRecordExtendNotImplemented = 3004,
        #endregion

        #region MerchantBalance (4xxxx)

        GetMerchantBalanceNotImplemented = 4000,

        #endregion

        Not_allowed_country = 5001,
        Not_allowed_currency = 5002,
        Not_allowed_environment = 5003,
        #region System Error(9xxxx)
        ConnectionTimeOut = 9002,
        ServerIsBusy = 9003,
        ServerResponseError = 9004,
        ServerNotReady = 9005,
        //app setting data error(93xxx)
        SettingNotFound = 9301,
        //Sql
        SqlException = 9501,
        //Unhandle Error
        SystemError = 9000,
        #endregion
        UM = 10
    }
    public enum TransferStatus
    {
        Success = 0,
        Fail = 1,
        Pending = 2, // 建單，但未扣款,
        PendingD = 3 // 建單，主錢包已扣款
    }

    public enum TransferPrefix
    {
        [Description("Transfer")]
        T = 0,
        [Description("Auto Transfer")]
        AT = 1,
        [Description("Bonus Cancel Transfer")]
        BT = 2
    }

    public enum Direction
    {
        ToTp = 1,
        ToMain = 2
    }
    public enum CustActStatus
    {
        NonDepoist = 1,
        Depoisted = 2,
    }
    public enum CustStatus
    {
        Running = 1,
        Suspend = 2,
        Closed = 3,
    }
    public enum AppType
    {
        Desktop = 1,
        Mobile = 2,
        Native = 3,
    }
    public enum AutoType
    {
        Auto = 1,
        Manual = 2
    }
    public enum WalletType
    {
        Transfer = 0,
        Seamless = 1,
    }

    public enum WalletMode
    {
        Seamless = 0,
        Restore = 1,
        Manual = 2,
    }

    public enum DataBase
    {
        Main, //ConnectionString
        Report,//ConnectionStringReport
        Wager,//ConnectionStringWager
    }
    public enum TpCategory
    {
        Sports = 1,
        Casino = 2,
        LiveCasino = 3,
        Fishing = 4,
        Cockfighting = 5,
        Lottery = 6,
        VirtualSports = 7,
        P2P = 8,
        Games = 10,
        Esports = 11,
    }

    public enum FinancialType
    {
        CancelledBonus = 7
    }

    public enum MaintainType
    {
        Date,
        Day,
        Week,
        Month
    }

    public enum DeviceType
    {
        None,
        Desktop,
        Mobile,
        iOS,
        Android
    }

    public enum OddsTypeEnum
    {
        None = 0,

        /// <summary>
        /// 马来盘
        /// </summary>
        MalayOdds = 1,

        /// <summary>
        /// 中国盘
        /// </summary>
        ChinaOdds = 2,

        /// <summary>
        /// 欧洲盘
        /// </summary>
        DecimalOdds = 3,

        /// <summary>
        /// 印度尼西亚盘
        /// </summary>
        IndoOdds = 4,

        /// <summary>
        /// 美国盘
        /// </summary>
        AmericanOdds = 5,

        /// <summary>
        /// 香港盘
        /// </summary>
        HongKongOdds = 6,

        /// <summary>
        /// 印度盤
        /// </summary>
        IndianOdds = 7
    }

    public enum AdjustWalletTypeEnum
    {
        Main = 1,
        ThirdParty = 2
    }

    public enum WithdrawalLimitLogCreateFromEnum
    {
        Default = 0,
        Bonus = 4
    }

    public enum RecentlyViewedTypeEnum
    {
        LobbyName = 1, // saba sport
        GameName = 2, // Fishing War
    }

    public enum CustomerMemberTypeEnum
    {
        Normal = 1,
        Affiliate = 2,  //後續整合代理系統會使用
        LiveStreaming = 3
    }

	public enum LoginGameTypeEnum
    {
        // from memberSite action
        EntPage = 1, 
        GotoGame = 2
    }

}
