namespace ThirdPartyWager.Service.Model
{
    public class WagerKiss918Model
    {
        public int playerID { get; set; }
        public int extplayerID { get; set; }
        public string gameID { get; set; } = "";
        public string methodType { get; set; } = "";
        public string playSessionID { get; set; } = "";
        public string referenceID { get; set; } = "";
        public string status { get; set; } = "";
        public DateTime created { get; set; }
        public DateTime updated { get; set; }
        public decimal betAmount { get; set; } = 0;
        public decimal winAmount { get; set; } = 0;
        public int jackpotModule { get; set; } = 0;
        public decimal jackpotContributionAmt { get; set; } = 0;
        public string currency { get; set; } = "";
        public string resultUrl { get; set; } = "";
        public string roundDetails { get; set; } = "";
        public string platform { get; set; } = "";
    }

}
