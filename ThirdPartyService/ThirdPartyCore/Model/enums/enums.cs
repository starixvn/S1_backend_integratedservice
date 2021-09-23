using System;
using System.Collections.Generic;
using System.Text;

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
        // auto transfer (13xxx)
        AutoTransferFail = 1300,
        AutoTransferIsOff = 1301,
        AutoTransferIsSeamless = 1302,
        AutoTransferBonusFail = 1303,
        #endregion
        #region invalid parameter  (2xxxx)
        InvalidParameter = 2000,
        InvalidUserName = 2001,
        InvalidCurrency = 2002,
        InvalidLanguage = 2003,
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
        Pending = 2,
        PendingD = 3
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
}
