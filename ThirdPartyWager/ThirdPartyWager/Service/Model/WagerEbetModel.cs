namespace ThirdPartyWager.Service.Model
{
    public class CurrencyStats_Ebet
    {
        public int status { get; set; }
        public string apiVersion { get; set; }
        public int count { get; set; }
        public int remainingVisits { get; set; }
        public decimal totalBet { get; set; }
        public decimal totalPayout { get; set; }
        public decimal totalWithholding { get; set; }
        public long startTime { get; set; }
        public long endTime { get; set; }
        public decimal totalBalance { get; set; }
        public int CurrencyId { get; set; }
    }
}
