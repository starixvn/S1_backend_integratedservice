using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum EdayResultCode
    {
        [Description("Success")] 
        Success = 0,
        [Description("Invalid OperatorId")] 
        InvalidOperatorId = 1000,
        [Description("Invalid Signature")] 
        InvalidSignature = 1001,
        [Description("Player Account Not Exist")] 
        PlayerAccountNotExist = 1002,
        [Description("Transaction Not Exist")] 
        TransactionNotExist = 1004,
        [Description("Invalid Status")] 
        InvalidStatus = 1005,
        [Description("Player Offline")] 
        PlayerOffline = 1006,
        [Description("Player Frozen")] 
        PlayerFrozen = 1007,
        [Description("Insufficient Balance")] 
        InsufficientBalance = 1008,
        [Description("System Under Maintenance")] 
        SystemUnderMaintenance = 1009,
        [Description("Transaction failed")] 
        TransactionFailed = 1010,
        [Description("Player Account Not Allowed To Login")] 
        PlayerAccountNotAllowedToLogin = 1011,
        [Description("Invalid Request Parameter")] 
        InvalidRequestParameter = 4000,
        [Description("TxId Information Mismatch")] 
        TxIdInformationMismatch = 4001,
        [Description("BillId Information Mismatch")] 
        BillIdInformationMismatch = 4002,
        [Description("ServerError")] 
        ServerError = 5000,
        [Description("Invalid Parameters")] 
        InvalidParameters = 5001,
    }
}