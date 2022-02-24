using System;
using System.Collections.Generic;


namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_HB
    {

        public string accountid { get; set; }
        public string gameinstanceid { get; set; }
        public long friendlygameinstanceid { get; set; }
        public int gamestatemode { get; set; }
        public string transferid { get; set; }
        public string currencycode { get; set; }
        public decimal amount { get; set; }
        public decimal bonusamount { get; set; }
        public bool jpwin { get; set; }
        public decimal jpcont { get; set; }
        public bool isbonus { get; set; }
        public DateTime dtevent { get; set; }
        public string initialdebittransferid { get; set; }
        public int accounttransactiontype { get; set; }
        public bool gameinfeature { get; set; }
        public string keyname { get; set; }
        public string gametypename { get; set; }
        public int channel { get; set; }
        public int TicketType { get; set; }

    }
    public class SettleDataRNG_HB
    {
        public string TransId { get; set; }
        public string TpUserName { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal ProgressiveBet { get; set; }
        public decimal ProgressiveWin { get; set; }
        public decimal WinLostAmount { get; set; }
        public long SettledTime { get; set; }
        public long TransTime { get; set; }
        public string GameId { get; set; }
    }

    public class PlaceBetRngDto_HB
    {
        public string accountid { get; set; }
        public string gameinstanceid { get; set; }
        public long friendlygameinstanceid { get; set; }
        public int gamestatemode { get; set; }
        public string transferid { get; set; }
        public string currencycode { get; set; }
        public decimal amount { get; set; }
        public decimal bonusamount { get; set; }
        public bool jpwin { get; set; }
        public decimal jpcont { get; set; }
        public bool isbonus { get; set; }
        public DateTime dtevent { get; set; }
        public string initialdebittransferid { get; set; }
        public int accounttransactiontype { get; set; }
        public bool gameinfeature { get; set; }
        public string gamename { get; set; }
        public string keyname { get; set; }
        public string gametypename { get; set; }
        public int channel { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
    }

    public class PayoutRngDto_HB
    {
        public string accountid { get; set; }
        public string gameinstanceid { get; set; }
        public long friendlygameinstanceid { get; set; }
        public int gamestatemode { get; set; }
        public string transferid { get; set; }
        public string currencycode { get; set; }
        public decimal amount { get; set; }
        public decimal bonusamount { get; set; }
        public bool jpwin { get; set; }
        public decimal jpcont { get; set; }
        public bool isbonus { get; set; }
        public DateTime dtevent { get; set; }
        public string initialdebittransferid { get; set; }
        public int accounttransactiontype { get; set; }
        public bool gameinfeature { get; set; }
        public string gamename { get; set; }
        public string keyname { get; set; }
        public string gametypename { get; set; }
        public int channel { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
    }

    public class CancelBetRngDto_HB
    {
        public string TransferId { get; set; }
        public string RoundId { get; set; }
        public string AccountId { get; set; }
        public decimal Amount { get; set; }
        public DateTime dtevent { get; set; }
    }

    public class BettransData_HB
    {
        public long SQ { get; set; }
        public string accountid { get; set; }
        public string transferid { get; set; }
        public decimal amount { get; set; }
        public string initialdebittransferid { get; set; }
        public int TicketStatus { get; set; }
    }

    public class TransactionResult_HB
    {
        public int result { get; set; }
        public decimal afterAmount { get; set; }
        public int BonusId { get; set; }
    }
    
    #region Auth
    public class HBPlayerDetailRequest
    {
        public string type { get; set; }
        public DateTime dtsent { get; set; }
        public Basegame basegame { get; set; }
        public Auth auth { get; set; }
        public Playerdetailrequest playerdetailrequest { get; set; }
    }
    
    public class Basegame
    {
        public string brandgameid { get; set; }
        public string keyname { get; set; }
    }

    public class Auth
    {
        public string username { get; set; }
        public string passkey { get; set; }
        public string machinename { get; set; }
        public string locale { get; set; }
        public string brandid { get; set; }
    }

    public class Playerdetailrequest
    {
        public string token { get; set; }
        public bool gamelaunch { get; set; }
    }

    public class HBPlayerDetailResponse
    {
        public playerdetailresponse playerdetailresponse { get; set; }
    }
    
    public class HBStatus
    {
        public bool success { get; set; }
        public bool autherror { get; set; }
        public string message { get; set; }
    }

    public class playerdetailresponse
    {
        public HBStatus Status { get; set; }
        public string accountid { get; set; }
        public string accountname { get; set; }
        public decimal balance { get; set; }
        public string currencycode { get; set; }
    }
    
    #endregion

    
    public class Fundinfo
    {
        public int gamestatemode { get; set; }
        public string transferid { get; set; }
        public string currencycode { get; set; }
        public decimal amount { get; set; }
        public decimal bonusamount { get; set; }
        public bool jpwin { get; set; }
        public decimal jpcont { get; set; }
        public bool isbonus { get; set; }
        public string dtevent { get; set; }
        public string initialdebittransferid { get; set; }
        public int accounttransactiontype { get; set; }
        public bool gameinfeature { get; set; }
    }

    public class Funds
    {
        public bool debitandcredit { get; set; }
        public List<Fundinfo> fundinfo { get; set; }
    }

    public class Gamedetails
    {
        public string name { get; set; }
        public string keyname { get; set; }
        public int gametypeid { get; set; }
        public string gametypename { get; set; }
        public string brandgameid { get; set; }
        public string gamesessionid { get; set; }
        public string gameinstanceid { get; set; }
        public int friendlygameinstanceid { get; set; }
        public decimal maxpaylimit { get; set; }
        public int channel { get; set; }
        public string device { get; set; }
        public string browser { get; set; }
    }

    public class Fundtransferrequest
    {
        public string token { get; set; }
        public string accountid { get; set; }
        public int customplayertype { get; set; }
        public string gameinstanceid { get; set; }
        public long friendlygameinstanceid { get; set; }
        public bool isretry { get; set; }
        public int retrycount { get; set; }
        public bool isrefund { get; set; }
        public bool isrecredit { get; set; }
        public Funds funds { get; set; }
        public Gamedetails gamedetails { get; set; }
    }

    public class HBFundTransferRequest
    {
        public string type { get; set; }
        public DateTime dtsent { get; set; }
        public Basegame basegame { get; set; }
        public Auth auth { get; set; }
        public dynamic fundtransferrequest { get; set; }
    }
    
    public class Status
    {
        public bool success { get; set; }
        public bool nofunds { get; set; }
        public bool successdebit { get; set; }
        public bool successcredit { get; set; }
    }

    public class Fundtransferresponse
    {
        public Status status { get; set; }
        public decimal balance { get; set; }
        public string currencycode { get; set; }
    }

    public class HBFundTransferResponse
    {
        public dynamic fundtransferresponse { get; set; }
    }
    public class TokenInfo
    {
        public string shortTimeToken { get; set; }
        public string longTimeToken { get; set; }
        public string tpUserName { get; set; }
        public string gameId { get; set; }
    }
}