using Newtonsoft.Json;
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

        public long licenseeTxId { get; set; }

        public string refId { get; set; }

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

    #region PlaceBet

    public class PlaceBetObj : ReturnObj
    {
        public string refId { get; set; }
        public string licenseeTxId { get; set; }
        public PlaceBetObj(ReturnCode returnCode) : base(returnCode) { }
    }
    public class PlaceBetParlayObj : ReturnObj
    {
        public List<BetObj> txns { get; set; }
        public PlaceBetParlayObj(ReturnCode returnCode) : base(returnCode) { }
    }
    public class PlaceBet3rdObj : ReturnObj
    {
        public decimal balance { get; set; }
        public List<BetObj> txns { get; set; }
        public PlaceBet3rdObj(ReturnCode returnCode) : base(returnCode) { }
    }
    public class BetObj
    {
        public string refId { get; set; }
        public string licenseeTxId { get; set; }
    }


    public class Bet
    {
        public Bet() { }
        public Bet(PlaceBetParlay placeBetParlay, ParlayDetail parlayDetail)
        {
            //Parlay
            sportType = 1;//Parlay直接設1

            action = placeBetParlay.action;
            operationId = placeBetParlay.operationId;
            userId = placeBetParlay.userId;
            currency = placeBetParlay.currency;
            betTime = placeBetParlay.betTime;
            updateTime = placeBetParlay.updateTime;
            IP = placeBetParlay.IP;
            tsId = placeBetParlay.tsId;
            betFrom = placeBetParlay.betFrom;

            refId = parlayDetail.refId;
            parlayType = parlayDetail.parlayType;
            betAmount = parlayDetail.betAmount;
            creditAmount = parlayDetail.creditAmount;
            debitAmount = parlayDetail.debitAmount;
            detail = parlayDetail.detail;
        }
        public Bet(PlaceBet3rd placeBet3rd, Ticket ticket)
        {
            //3rd
            sportType = placeBet3rd.productId;
            action = placeBet3rd.action;
            operationId = placeBet3rd.operationId;
            userId = placeBet3rd.userId;
            currency = placeBet3rd.currency;     
            productId = placeBet3rd.productId;
            gameId = placeBet3rd.gameId;
            betTime = placeBet3rd.betTime;
            IP = placeBet3rd.IP;
            tsId = placeBet3rd.tsId;
            productName_en = placeBet3rd.productName_en;
            gameName_en = placeBet3rd.gameName_en;
            betFrom = placeBet3rd.betFrom;
            refId = ticket.refId;
            betChoice_en = ticket.betChoice_en;
            odds = ticket.odds;
            oddsType = ticket.oddsType;
            betAmount = ticket.betAmount;
            actualAmount = ticket.actualAmount;
            extra = ticket.extra != null ? JsonConvert.SerializeObject(ticket.extra) : "{}";
            creditAmount = ticket.creditAmount;
            debitAmount = ticket.debitAmount;
        }

        public string action { get; set; } = "";
        public string operationId { get; set; } = "";
        public string userId { get; set; } = "";
        public int currency { get; set; }
        public int matchId { get; set; }
        public int homeId { get; set; }
        public int awayId { get; set; }
        public string homeName { get; set; } = "";
        public string awayName { get; set; } = "";
        public DateTime kickOffTime { get; set; } = DateTime.MinValue;
        public DateTime betTime { get; set; } = DateTime.MinValue;
        public decimal betAmount { get; set; }
        public decimal actualAmount { get; set; }
        public int sportType { get; set; }
        public string sportTypeName { get; set; } = "";
        public int betType { get; set; }
        public string betTypeName { get; set; } = "";
        public int oddsType { get; set; }
        public int oddsId { get; set; }
        public decimal odds { get; set; }
        public string betChoice { get; set; } = "";
        public string betChoice_en { get; set; } = "";
        public DateTime updateTime { get; set; } = DateTime.MinValue;
        public int leagueId { get; set; }
        public string leagueName { get; set; } = "";
        public string leagueName_en { get; set; } = "";
        public string sportTypeName_en { get; set; } = "";
        public string betTypeName_en { get; set; } = "";
        public string homeName_en { get; set; } = "";
        public string awayName_en { get; set; } = "";
        public string IP { get; set; } = "";
        public bool isLive { get; set; }
        public string refId { get; set; } = "";
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
        public DateTime matchDateTime { get; set; } = DateTime.MinValue;

        //3rd
        public int productId { get; set; }
        public int gameId { get; set; }
        public string productName_en { get; set; } = "";
        public string gameName_en { get; set; } = "";
        public string extra { get; set; } = "{}";

        //parlay

        public string parlayType { get; set; } = "";
        public string detail { get; set; } = "{}";
    }

    public class PlaceBet3rd
    {
        public string action { get; set; }
        public string operationId { get; set; }
        public string userId { get; set; }
        public int currency { get; set; }
        public int productId { get; set; }
        public int gameId { get; set; }
        public Ticket[] ticketList { get; set; }
        public DateTime betTime { get; set; }
        public string IP { get; set; }
        public string tsId { get; set; }
        public string productName_en { get; set; }
        public string gameName_en { get; set; }
        public string betFrom { get; set; }
    }
    public class Ticket
    {
        public string refId { get; set; }
        public string betChoice_en { get; set; }
        public decimal odds { get; set; }
        public int oddsType { get; set; }
        public decimal betAmount { get; set; }
        public decimal actualAmount { get; set; }
        public dynamic extra { get; set; }
        public decimal creditAmount { get; set; }
        public decimal debitAmount { get; set; }
    }

    public class PlaceBetParlay
    {
        public string action { get; set; }
        public string operationId { get; set; }
        public string userId { get; set; }
        public int currency { get; set; }
        public DateTime betTime { get; set; }
        public DateTime updateTime { get; set; }
        public string IP { get; set; }
        public string tsId { get; set; }
        public string betFrom { get; set; }
        public ParlayDetail[] txns { get; set; }
    }
    public class ParlayDetail
    {
        public string refId { get; set; }
        public string parlayType { get; set; }
        public decimal betAmount { get; set; }
        public decimal creditAmount { get; set; }
        public decimal debitAmount { get; set; }
        public dynamic detail { get; set; }
    }
    #endregion

    #region ConfirmBet

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
        public ConfirmDetail[] txns { get; set; }
    }
    public class ConfirmDetail
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

    public class Confirm
    {
        public Confirm(ConfirmBet confirmBet, ConfirmDetail detail)
        {
            action = confirmBet.action;
            operationId = confirmBet.operationId;
            userId = confirmBet.userId;
            updateTime = confirmBet.updateTime;
            transactionTime =confirmBet.transactionTime;
            refId = detail.refId;
            txId = detail.txId;
            licenseeTxId = detail.licenseeTxId;
            odds =detail.odds;
            oddsType = detail.oddsType;
            actualAmount = detail.actualAmount;
            isOddsChanged = detail.isOddsChanged;
            creditAmount = detail.creditAmount;
            debitAmount = detail.debitAmount;
            winlostDate = detail.winlostDate;
        }
        public string action { get; set; }
        public string operationId { get; set; }
        public string userId { get; set; }
        public DateTime updateTime { get; set; }
        public DateTime transactionTime { get; set; }
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

    #endregion

    #region Settle

    public class Settle
    {
        public string action { get; set; }
        public string operationId { get; set; }
        public SettleDetail[] txns { get; set; }
    }
    public class SettleDetail
    {
        public string userId { get; set; }
        public string refId { get; set; }
        public long txId { get; set; }
        public DateTime updateTime { get; set; }
        public DateTime winlostDate { get; set; }
        public string status { get; set; } = "";
        public decimal payout { get; set; }
        public decimal creditAmount { get; set; }
        public decimal debitAmount { get; set; }
        public string extraStatus { get; set; }
        public string extra_status { get; set; }

    }

    #endregion
}
