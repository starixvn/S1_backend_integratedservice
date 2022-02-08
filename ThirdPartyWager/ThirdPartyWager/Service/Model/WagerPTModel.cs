using System;
using System.Collections.Generic;

namespace ThirdPartyWager.Service.Model
{
    public class WagerPTModel
    {
        public string? error { get; set; }
        public int errorcode { get; set; }
        public IList<BetPT> result { get; set; }
        public Pagination pagination { get; set; }
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
}
