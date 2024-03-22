namespace ThirdPartyWager.Service.Model
{
    public class WagerWonCasinoModel
    {
        public int result { get; set; }
        public int arraysize { get; set; }
        public string desc { get; set; }
        public int total { get; set; }
        public WonCasinoDetailData[] array { get; set; }
    }
    public class WonCasinoDetailData
    {
        public int id { get; set; }
        public string serialnumber { get; set; } = string.Empty;
        public int userid { get; set; }
        public string username { get; set; }
        public decimal gameid { get; set; }
        public int gameroundid { get; set; }
        public int gameroundno { get; set; } = 0;
        public string gameresult { get; set; }
        public int state { get; set; }
        public int bettype { get; set; }
        public string betpoint { get; set; }
        public decimal betamount { get; set; }
        public decimal winlost { get; set; }
        public decimal commamount { get; set; }
        public string commratio { get; set; } = string.Empty;
        public decimal balance { get; set; }
        public decimal betodds { get; set; }
        public DateTime updatetime { get; set; }
        public DateTime createtime { get; set; }
    }
}
