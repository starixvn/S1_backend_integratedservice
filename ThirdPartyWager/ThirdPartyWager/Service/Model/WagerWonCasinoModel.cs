namespace ThirdPartyWager.Service.Model
{
    public class WagerWonCasinoModel
    {
        public int result { get; set; }
        public int arraysize { get; set; }
        public WonCasinoDetailData[] array { get; set; }
    }
    public class WonCasinoDetailData
    {
        public decimal id { get; set; }
        public decimal serialnumber { get; set; }
        public decimal userid { get; set; }
        public string username { get; set; }
        public decimal gameid { get; set; }
        public decimal gameroundid { get; set; }
        public decimal gameroundno { get; set; }
        public string gameresult { get; set; }
        public int state { get; set; }
        public int bettype { get; set; }
        public string betpoint { get; set; }
        public decimal betamount { get; set; }
        public decimal winlost { get; set; }
        public decimal commamount { get; set; }
        public string commratio { get; set; }
        public decimal balance { get; set; }
        public decimal betodds { get; set; }
        public long updatetime { get; set; }
        public long createtime { get; set; }

    }
}
