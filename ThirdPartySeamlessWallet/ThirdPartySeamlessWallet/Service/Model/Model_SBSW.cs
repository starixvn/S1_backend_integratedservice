using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.enums;
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
        HealthCheck,
        PlaceBetEnt,
        SettleEnt,
        CancelBetEnt,
        GetTicketInfo,
        AdjustBalance,
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

    public class PlaceBetEntObj : ReturnObj
    {
        public string userId { get; set; }
        public decimal balance { get; set; }
        public List<BetObj> ticketList { get; set; }
        public PlaceBetEntObj(ReturnCode returnCode) : base(returnCode) { }
    }

    public class GetTicketInfoObj : ReturnObj
    {
        public string ticketStatus { get; set; }
        public decimal actualStake { get; set; }
        public decimal winlostAmount { get; set; }
        public GetTicketInfoObj(ReturnCode returnCode) : base(returnCode) { }
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
            tsId = placeBetParlay.tsId ?? string.Empty;
            betFrom = placeBetParlay.betFrom;

            refId = parlayDetail.refId;
            parlayType = parlayDetail.parlayType;
            betAmount = parlayDetail.betAmount;
            creditAmount = parlayDetail.creditAmount;
            debitAmount = parlayDetail.debitAmount;
            detail = parlayDetail.detail != null ? JsonConvert.SerializeObject(parlayDetail.detail) : "{}";
        }
        public Bet(PlaceBet3rd placeBet3rd, Ticket ticket)
        {
            //3rd
            sportType = placeBet3rd.productId;
            action = placeBet3rd.action;
            operationId = placeBet3rd.operationId;
            userId = placeBet3rd.userId;
            currency = placeBet3rd.currency;
            sportType = placeBet3rd.productId;
            productId = placeBet3rd.productId;
            gameId = placeBet3rd.gameId;
            betTime = placeBet3rd.betTime;
            IP = placeBet3rd.IP;
            tsId = placeBet3rd.tsId ?? string.Empty;
            sportTypeName = placeBet3rd.productName_en;
            leagueName = placeBet3rd.gameName_en;
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
        public Bet(PlaceBetEnt placeBetEnt, Ticketlist ticket)
        {
            action = placeBetEnt.action;
            userId = placeBetEnt.userId;
            currency = placeBetEnt.currency;
            sportType = placeBetEnt.productId;
            productId = placeBetEnt.productId;
            gameId = placeBetEnt.gameId;
            IP = placeBetEnt.IP;
            sportTypeName = placeBetEnt.productName_en;
            leagueName = placeBetEnt.gameName_en;
            productName_en = placeBetEnt.productName_en;
            gameName_en = placeBetEnt.gameName_en;
            betFrom = placeBetEnt.betFrom;
            roundId = placeBetEnt.roundId;
            betTime = placeBetEnt.betTime;
            refId = ticket.refId;
            betChoice_en = ticket.betChoice_en;
            betAmount = ticket.stake;
            actualAmount = ticket.actualStake;
            extra = ticket.extra != null ? JsonConvert.SerializeObject(ticket.extra) : "{}";
            creditAmount = placeBetEnt.creditAmount;
            debitAmount = placeBetEnt.debitAmount;
        }
        public string action { get; set; } = string.Empty;
        public string operationId { get; set; } = string.Empty;
        public string userId { get; set; } = string.Empty;
        public int currency { get; set; }
        public int matchId { get; set; }
        public int homeId { get; set; }
        public int awayId { get; set; }
        public string homeName { get; set; } = string.Empty;
        public string awayName { get; set; } = string.Empty;
        public DateTime kickOffTime { get; set; } = DateTime.MinValue;
        public DateTime betTime { get; set; } = DateTime.MinValue;
        public decimal betAmount { get; set; }
        public decimal actualAmount { get; set; }
        public int sportType { get; set; }
        public string sportTypeName { get; set; } = string.Empty;
        public int betType { get; set; }
        public string betTypeName { get; set; } = string.Empty;
        public int oddsType { get; set; }
        public int oddsId { get; set; }
        public decimal odds { get; set; }
        public string betChoice { get; set; } = string.Empty;
        public string betChoice_en { get; set; } = string.Empty;
        public DateTime updateTime { get; set; } = DateTime.MinValue;
        public int leagueId { get; set; }
        public string leagueName { get; set; } = string.Empty;
        public string leagueName_en { get; set; } = string.Empty;
        public string sportTypeName_en { get; set; } = string.Empty;
        public string betTypeName_en { get; set; } = string.Empty;
        public string homeName_en { get; set; } = string.Empty;
        public string awayName_en { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public bool isLive { get; set; }
        public string refId { get; set; } = string.Empty;
        public string tsId { get; set; } = string.Empty;
        public string point { get; set; } = string.Empty;
        public string point2 { get; set; } = string.Empty;
        public string betTeam { get; set; } = string.Empty;
        public int homeScore { get; set; }
        public int awayScore { get; set; }
        public bool baStatus { get; set; }
        public string excluding { get; set; } = string.Empty;
        public string betFrom { get; set; } = string.Empty;
        public decimal creditAmount { get; set; }
        public decimal debitAmount { get; set; }
        public string oddsInfo { get; set; } = string.Empty;
        public DateTime matchDateTime { get; set; } = DateTime.MinValue;


        //confirm
        public DateTime transactionTime { get; set; } = DateTime.MinValue;
        public long txId { get; set; }
        public long licenseeTxId { get; set; }
        public bool isOddsChanged { get; set; }
        public DateTime winlostDate { get; set; } = DateTime.MinValue;

        //3rd
        public int productId { get; set; }
        public int gameId { get; set; }
        public string productName_en { get; set; } = string.Empty;
        public string gameName_en { get; set; } = string.Empty;
        public string extra { get; set; } = "{}";

        //parlay

        public string parlayType { get; set; } = string.Empty;
        public string detail { get; set; } = "{}";

        //PlaceBetEnt
        public string roundId { get; set; } = string.Empty;
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
        public string tsId { get; set; } = string.Empty;
        public string productName_en { get; set; }
        public string gameName_en { get; set; }
        public string betFrom { get; set; }
    }
    public class Ticket
    {
        public string refId { get; set; }
        public string betChoice_en { get; set; } = string.Empty;
        public decimal odds { get; set; }
        public int oddsType { get; set; }
        public decimal betAmount { get; set; }
        public decimal actualAmount { get; set; }
        public dynamic extra { get; set; }
        public decimal creditAmount { get; set; }
        public decimal debitAmount { get; set; }
    }


    public class PlaceBetEnt
    {
        public string action { get; set; }
        public DateTime betTime { get; set; }
        public string userId { get; set; }
        public Ticketlist[] ticketList { get; set; }
        public int currency { get; set; }
        public int productId { get; set; }
        public string productName_en { get; set; }
        public int gameId { get; set; }
        public string gameName_en { get; set; }
        public string roundId { get; set; }
        public string IP { get; set; }
        public string betFrom { get; set; }
        public decimal creditAmount { get; set; }
        public decimal debitAmount { get; set; }
    }

    public class Ticketlist
    {
        public string refId { get; set; }
        public string betChoice_en { get; set; } = string.Empty;
        public decimal stake { get; set; }
        public decimal actualStake { get; set; }
        public dynamic extra { get; set; }
    }


    public class PlaceBetParlay
    {
        public string action { get; set; }
        public string operationId { get; set; }
        public string userId { get; set; }
        public int currency { get; set; }
        public DateTime betTime { get; set; }
        public DateTime updateTime { get; set; }
        public decimal totalBetAmount { get; set; }
        public string IP { get; set; }
        public string tsId { get; set; } = string.Empty;
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
        public string licenseeTxId { get; set; } = string.Empty;
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
            transactionTime = confirmBet.transactionTime;
            refId = detail.refId;
            txId = detail.txId;
            licenseeTxId = detail.licenseeTxId;
            odds = detail.odds;
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
        public string operationId { get; set; } = string.Empty;
        public SettleDetail[] txns { get; set; }
    }
    public class SettleDetail
    {
        public string userId { get; set; }
        public string refId { get; set; }
        public long txId { get; set; }
        public DateTime updateTime { get; set; }
        public DateTime winlostDate { get; set; }
        public string status { get; set; } = string.Empty;
        public decimal payout { get; set; }
        public decimal creditAmount { get; set; }
        public decimal debitAmount { get; set; }
        public string extraStatus { get; set; }
        public string extra_status { get; set; }
    }


    public class SettleEnt
    {
        public string action { get; set; }
        public string userId { get; set; }
        public string refId { get; set; }
        public string status { get; set; }
        public decimal actualStake { get; set; }
        public decimal stake { get; set; }
        public decimal netStake { get; set; }
        public DateTime winLostDate { get; set; }
        public decimal creditAmount { get; set; }
        public decimal debitAmount { get; set; }
        public decimal winlostAmount { get; set; }
        public long[] txIds { get; set; }
    }

    public class GetTicketInfo
    {
        public string action { get; set; }
        public string userId { get; set; }
        public string refId { get; set; }
    }

    public class AdjustBalance
    {
        public string action { get; set; }
        public DateTime time { get; set; }
        public string userId { get; set; }
        public int currency { get; set; }
        public long txId { get; set; }
        public string refId { get; set; }
        public string operationId { get; set; }
        public int betType { get; set; }
        public string betTypeName { get; set; }
        public DateTime winlostDate { get; set; }
        public Balanceinfo balanceInfo { get; set; }
    }

    public class Balanceinfo
    {
        public decimal creditAmount { get; set; }
        public decimal debitAmount { get; set; }
    }


    public class BetInfo
    {
        public decimal actualAmount { get; set; }
        public int TicketType { get; set; }
    }

    public class SettleInfo
    {
        public long SQ { get; set; }
        public decimal creditAmount { get; set; }
        public decimal debitAmount { get; set; }
        public string status { get; set; }
    }
    #endregion

    public class SBSWUnsettle : TpUnsettle
    {
        public GameType GameType { get; set; }
    }
    public class GetBetDetailData
    {
        public long last_version_key { get; set; }
        public IEnumerable<BetDetail> BetDetails { get; set; }
        public IEnumerable<BetNumberDetail> BetNumberDetails { get; set; }
        public IEnumerable<BetVirtualSportDetail> BetVirtualSportDetails { get; set; }
    }
    public interface ISabaBet
    {
        decimal odds { get; set; }
        decimal stake { get; set; }
        public decimal? discount_stake { get; set; }
        public int? odds_type { get; set; }
        public int? bet_type { get; set; }
        public string ticket_status { get; set; }
        string tokenmoney { get; }
    }
    public class BaseBetDetail : ISabaBet
    {
        public decimal odds { get; set; }
        public decimal stake { get; set; }
        public decimal? discount_stake { get; set; }
        public int? odds_type { get; set; }
        public int? bet_type { get; set; }
        public string ticket_status { get; set; }
        public string tokenmoney { get; set; }
        public int? sport_type { get; set; }
        public DateTime transaction_time { get; set; }
        public decimal winlost_amount { get; set; }
        public long trans_id { get; set; }
        public string vendor_member_id { get; set; }
        public DateTime? winlost_datetime { get; set; }
        public string ticket_extra_status { get; set; } = string.Empty;
        public string settlement_time { get; set; }
    }
    public class BetDetail: BaseBetDetail
    {
        public string operator_id { get; set; }
        public int? league_id { get; set; }
        public int? match_id { get; set; }
        public int? home_id { get; set; }
        public int? away_id { get; set; }
        public string match_datetime { get; set; }
        public long? parlay_ref_no { get; set; }
        public decimal after_amount { get; set; }
        public int currency { get; set; }
        public string bet_team { get; set; }
        public string isLucky { get; set; }
        public decimal home_hdp { get; set; }
        public decimal away_hdp { get; set; }
        public decimal? hdp { get; set; }
        public string betfrom { get; set; }
        public string islive { get; set; }
        public int? home_score { get; set; }
        public int? away_score { get; set; }
        public string customInfo1 { get; set; }
        public string customInfo2 { get; set; }
        public string customInfo3 { get; set; }
        public string customInfo4 { get; set; }
        public string customInfo5 { get; set; }
        public string ba_status { get; set; }
        public int version_key { get; set; }
        public object ParlayData { get; set; }
        public decimal? original_stake { get; set; }
        public IEnumerable<Leaguename> leaguename { get; set; }
        public IEnumerable<Sportname> sportname { get; set; }
        public IEnumerable<Bettypename> bettypename { get; set; }
        public string betchoice { get; set; } = string.Empty;
    }
    public class Leaguename
    {
        public string lang { get; set; }
        public string name { get; set; }
    }
    public class Sportname
    {
        public string lang { get; set; }
        public string name { get; set; }
    }
    public class Bettypename
    {
        public string lang { get; set; }
        public string name { get; set; }
    }

    public class BetNumberDetail : BaseBetDetail
    {
        public string operator_id { get; set; }
        public int? match_id { get; set; }
        public string betfrom { get; set; }
        public string islive { get; set; }
        public int? last_ball_no { get; set; }
        public string bet_team { get; set; }
        public int currency { get; set; }
        public decimal after_amount { get; set; }
        public string customInfo1 { get; set; }
        public string customInfo2 { get; set; }
        public string customInfo3 { get; set; }
        public string customInfo4 { get; set; }
        public string customInfo5 { get; set; }
        public string ba_status { get; set; }
        public long version_key { get; set; }
        public string betchoice { get; set; } = string.Empty;
    }
    public class BetVirtualSportDetail : BaseBetDetail
    {
        public string operator_id { get; set; }
        public int? league_id { get; set; }
        public int? match_id { get; set; }
        public int? home_id { get; set; }
        public int? away_id { get; set; }
        public string match_datetime { get; set; }
        public long? parlay_ref_no { get; set; }
        public decimal after_amount { get; set; }
        public int currency { get; set; }
        public string bet_team { get; set; }
        public decimal? home_hdp { get; set; }
        public decimal? away_hdp { get; set; }
        public decimal? hdp { get; set; }
        public string betfrom { get; set; }
        public string islive { get; set; }
        public string customInfo1 { get; set; }
        public string customInfo2 { get; set; }
        public string customInfo3 { get; set; }
        public string customInfo4 { get; set; }
        public string customInfo5 { get; set; }
        public string ba_status { get; set; }
        public long version_key { get; set; }
        public dynamic ParlayData { get; set; }
        public string race_number { get; set; }
        public string race_lane { get; set; }
    }
    public class RngBetDetail : BaseBetDetail
    {
        public decimal bet_amount { get; set; }
        public decimal after_amount { get; set; }
        public string ticket_status { get; set; }
        public int currency { get; set; }
        public long ref_no { get; set; }
        public long version_key { get; set; }
    }
}
