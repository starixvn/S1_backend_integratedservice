using System;
using System.Collections.Generic;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.Model.SBSW
{
    public enum ReturnCode
    {
        Success = 0,
        UnknownError = -1,
        OperatorNotFound = 301,
        UserNotFound = 203,
        UnSupportedCurrency = 302,
        InvalidToken = 305,
        InvalidVendorId = 311,
        UserLocked = 202,
        TransactionDuplicated = 1,
        InvalidParameters = 101,
        DatabaseError = 901,
        InsufficientBalance = 502,
        TransactionNotFound = 504,
        InvalidAmount = 307,
        InvalidTransactionStatus = 309,
    }

    public enum SBSWTransType
    {
        GetBalance,
        PlaceBet,
        ConfirmBet,
        CancelBet,
        Settle,
        Resettle,
        Unsettle,
        PlaceBetParlay,
        ConfirmBetParlay,
        PlaceBet3rd,
        ConfirmBet3rd,
        HealthCheck
    }

    public class Config
    {
        public string ApiUrl { get; set; }
        public string VendorId { get; set; }

        public Config(Dictionary<string, string> inputs, string merchant)
        {
            try
            {
                ApiUrl = inputs.GetValue($"ApiUrl_{merchant}");
                VendorId = inputs.GetValue($"VendorId_{merchant}");
            }
            catch (Exception ex)
            {
                throw new SBSWException(ReturnCode.OperatorNotFound, ex.Message);
            }
        }
    }

    public class ReturnObj
    {
        public string status { get; set; }

        public string msg { get; set; }

        public ReturnObj(ReturnCode returnCode)
        {
            status = returnCode.CodeToString();
            msg = returnCode.ToString();
        }
        public ReturnObj(ReturnCode returnCode, string message)
        {
            status = returnCode.CodeToString();
            msg = message;
        }
        public ReturnObj(SBSWException exception)
        {
            status = exception.ErrorCode;
            msg = exception.ErrorMessage;
        }
        public ReturnObj(ExecuteResult result)
        {
            status = result.ReturnCode.CodeToString();
            msg = result.Message;
        }
    }

    public class BaseReq<T>
    {
        public string key { get; set; }

        public T message { get; set; }
    }

    public class ExecuteResult
    {

        public int SPResult { get; set; } = -1;

        public int SportType { get; set; }

        public int SetBalanceResult { get; set; } = -1;

        public ReturnCode ReturnCode { get; set; } 

        public string Message { get; set; }

        public ExecuteResult()
        {     
        }

        public ExecuteResult(ReturnCode returnCode)
        {
            ReturnCode = returnCode;
            Message = returnCode.ToString();
        }
        public ExecuteResult(ReturnCode returnCode, string message)
        {
            ReturnCode = returnCode;
            Message = message;
        }
    }

    public class BalanceObj : ReturnObj
    {
        public string userId { get; set; }
        public decimal balance { get; set; }
        public string balanceTs { get; set; }

        public BalanceObj(ReturnCode returnCode) : base(returnCode) { }
    }
    public class GetBalance
    {
        public string action { get; set; }
        public string userId { get; set; }

    }

    public class PlaceBetObj : ReturnObj
    {
        public string refId { get; set; }
        public string licenseeTxId { get; set; }

        public PlaceBetObj(ReturnCode returnCode) : base(returnCode) { }
    }
    public class PlaceBet
    {
        public string action { get; set; }
        public string operationId { get; set; }
        public string userId { get; set; }
        public int currency { get; set; }
        public int matchId { get; set; }
        public int homeId { get; set; }
        public int awayId { get; set; }
        public string homeName { get; set; }
        public string awayName { get; set; }
        public DateTime kickOffTime { get; set; }
        public DateTime betTime { get; set; }
        public decimal betAmount { get; set; }
        public decimal actualAmount { get; set; }
        public int sportType { get; set; }
        public string sportTypeName { get; set; }
        public int betType { get; set; }
        public string betTypeName { get; set; }
        public int oddsType { get; set; }
        public int oddsId { get; set; }
        public decimal odds { get; set; }
        public string betChoice { get; set; }
        public string betChoice_en { get; set; }
        public DateTime updateTime { get; set; }
        public int leagueId { get; set; }
        public string leagueName { get; set; }
        public string leagueName_en { get; set; }
        public string sportTypeName_en { get; set; }
        public string betTypeName_en { get; set; }
        public string homeName_en { get; set; }
        public string awayName_en { get; set; }
        public string IP { get; set; }
        public bool isLive { get; set; }
        public string refId { get; set; }
        public string tsId { get; set; } = "";
        public string point { get; set; } = "";
        public string point2 { get; set; } = "";
        public string betTeam { get; set; } = "";
        public int homeScore { get; set; }
        public int awayScore { get; set; }
        public bool baStatus { get; set; }
        public string excluding { get; set; } = "";
        public string betFrom { get; set; }
        public decimal creditAmount { get; set; }
        public decimal debitAmount { get; set; }
        public string oddsInfo { get; set; } = "";
        public DateTime matchDateTime { get; set; }
    }
   

    public class ConfirmBetObj : ReturnObj
    {
        public decimal balance { get; set; }

        public ConfirmBetObj(ReturnCode returnCode) : base(returnCode) { }
    }
    public class ConfirmBet
    {
        public string action { get; set; }
        public string operationId { get; set; }
        public string userId { get; set; }
        public DateTime updateTime { get; set; }
        public DateTime transactionTime { get; set; }
        public List<Txn> txns { get; set; }
    }
    public class Txn
    {
        public string refId { get; set; }
        public long txId { get; set; }
        public string licenseeTxId { get; set; }
        public decimal odds { get; set; }
        public int oddsType { get; set; }
        public decimal actualAmount { get; set; }
        public bool isOddsChanged { get; set; }
        public decimal creditAmount { get; set; }
        public decimal debitAmount { get; set; }
        public DateTime winlostDate { get; set; }
    }
}
