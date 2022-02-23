using System;

namespace ThirdPartyWager.Service.Model
{
    public class WagerKMModel
    {
        public string ugsbetid { get; set; }
        public string txid { get; set; }
        public string betid { get; set; }
        public DateTime beton { get; set; }
        public DateTime betclosedon { get; set; }
        public DateTime betupdatedon { get; set; }
        public DateTime timestamp { get; set; }
        public string roundid { get; set; }
        public string roundstatus { get; set; }
        public string userid { get; set; }
        public string username { get; set; }
        public decimal riskamt { get; set; }
        public decimal winamt { get; set; }
        public decimal winloss { get; set; }
        public decimal beforebal { get; set; }
        public decimal postbal { get; set; }
        public string cur { get; set; }
        public string gameprovider { get; set; }
        public string gameprovidercode { get; set; }
        public string gamename { get; set; }
        public string gameid { get; set; }
        public string platformtype { get; set; }
        public string ipaddress { get; set; }
        public string bettype { get; set; }
        public string playtype { get; set; }
        public int playertype { get; set; }
        public decimal turnover { get; set; }
        public decimal validbet { get; set; }
    }
}