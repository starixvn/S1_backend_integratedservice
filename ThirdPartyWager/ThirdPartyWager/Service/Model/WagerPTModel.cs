using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.Model
{
    public class WagerPTModel
    {
        public string? error { get; set; }
        public int errorcode { get; set; }
        public IList<BetPT> result { get; set; }
        public Pagination pagination { get; set; }
    }
    
    public class PTRequestModel
    {
        public int SiteId { get; set; }
        public string Currency { get; set; }
        public int TimeZone { get; set; }
    }

    public class BetPT
    {
        public string PLAYERNAME { get; set; }
        public int WINDOWCODE { get; set; }
        public int GAMEID { get; set; }
        public string GAMECODE { get; set; }
        public string GAMETYPE { get; set; }
        public string GAMENAME { get; set; }
        public string SESSIONID { get; set; }
        public string CURRENCYCODE { get; set; }
        public decimal BET { get; set; }
        public decimal WIN { get; set; }
        public decimal PROGRESSIVEBET { get; set; }
        public decimal PROGRESSIVEWIN { get; set; }
        public decimal BALANCE { get; set; }
        public decimal CURRENTBET { get; set; }
        public DateTime GAMEDATE { get; set; }
        public string LIVENETWORK { get; set; }
        public int EXITGAME { get; set; }
        public string INFO { get; set; }
        public int BONUSTYPE { get; set; }
        public decimal GOLDENCHIPBET { get; set; }
        public decimal GOLDENCHIPWIN { get; set; }
        public decimal RETURNEDGOLDENCHIPS { get; set; }
        public string SHORTNAME { get; set; }

        public long gameUnix { get; set; }
    }
    public class Pagination
    {
        public int currentPage { get; set; }
        public int totalPages { get; set; }
        public int itemsPerPage { get; set; }
        public int totalCount { get; set; }

    }
    
    public class BetRecordInfo
    {
        public int TpId { get; set; }
        public string TransId { get; set; }
        public GameType GameType { get; set; }
        public string TpUserName { get; set; }
        public DateTime SettledTime { get; set; }
        public DateTime TransTime { get; set; }
        public decimal Stake { get; set; }
        public decimal? ValidBet { get; set; }
        public decimal? Revenue { get; set; }
        public decimal ProgressiveBet { get; set; }
        public decimal ProgressiveWin { get; set; }
        public decimal WinlostAmount { get; set; }
        public SettlementType SettlementType { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public string OriginalState { get; set; }
        public int TicketKey { get; set; }
        public int BonusId { get; set; }
    }

    public class CurrencyStats_PT
    {
        public dynamic error { get; set; }
        public int errorcode { get; set; }
        public IList<CurrencyStatsData_PT> result { get; set; }
        public Pagination pagination { get; set; }
    }

    public class CurrencyStatsData_PT
    {
        public string ENTITY { get; set; }
        public string PLAYERS { get; set; }
        public string GAMES { get; set; }
        public string CURRENCYCODE { get; set; }
        public decimal BETS { get; set; }
        public decimal WINS { get; set; }
        public int FREESPINCOUNT { get; set; }
        public decimal FREESPINBET { get; set; }
        public decimal FREESPINWIN { get; set; }
        public int GOLDENCHIPCOUNT { get; set; }
        public decimal GOLDENCHIPBET { get; set; }
        public decimal GOLDENCHIPWIN { get; set; }
        public string GOLDENCHIPINCOMESHARE { get; set; }
        public string FREESPININCOMESHARE { get; set; }
        public decimal INCOME { get; set; }
        [JsonProperty("JACKPOTBETS#")]
        public decimal JACKPOTBETS { get; set; }
        [JsonProperty("JACKPOTWINS#")]
        public decimal JACKPOTWINS { get; set; }

    }
}
