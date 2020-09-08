using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonService.Service.Model.enums
{
    public enum ErrorCode : int
    {
        Success = 0,
        #region api response fail (1xxxx)
        Fail = 1000,
        NoAuthority = 1001,
        KEY_ERROR = 1002,
        NETWORK_ERROR = 1003,
        Too_Many_Requests = 1004,
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
        #endregion
        #region invalid parameter  (2xxxx)
        InvalidParameter = 2000,
        InvalidUserName = 2001,
        InvalidCurrency = 2002,
        InvalidLanguage = 2003,
        #endregion
        #region SMS  (5xxxx)
        //Not_allowed_country = 5001,
        //Not_allowed_currency = 5002,
        #endregion
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
}
