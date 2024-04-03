namespace ThirdPartyWager.Service.Model
{
    public class WagerPlayTechModel
    {
        public string requestId { get; set; }
        public string error { get; set; }
        public string message { get; set; }
        public int total { get; set; }
        public int current { get; set; }
        public int size { get; set; }
        public int pages { get; set; }
        public List<Record_PlayTech> records { get; set; }
    }

    public class Record_PlayTech
    {
        public string playerId { get; set; }
        public int brandId { get; set; }
        public string providerCode { get; set; }
        public string sessionId { get; set; }
        public string roundId { get; set; }
        public string roundType { get; set; }
        public string roundStatus { get; set; }
        public string gameCode { get; set; }
        private string _currency;
        public string currency
        {
            get
            {
                if (_currency == "Playtech_N")
                {
                    return "BRL";
                }
                return _currency;
            }
            set
            {
                _currency = value;
            }
        }
        public string platform { get; set; } = "";
        public decimal bets { get; set; }
        public decimal wins { get; set; }
        public decimal cancels { get; set; }
        public decimal transIn { get; set; }
        public decimal transOut { get; set; }
        public decimal balanceEnd { get; set; }
        public decimal bonusBalanceEnd { get; set; }
        public decimal rake { get; set; }
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
        public decimal jpc { get; set; }
        public decimal jpw { get; set; }
        public string detailUrl { get; set; } = "";
        public long startTimeUnix
        {
            get
            {
                return new DateTimeOffset(startTime, new TimeSpan(0, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long endTimeUnix
        {
            get
            {
                return new DateTimeOffset(endTime, new TimeSpan(0, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class CurrencyStats_PlayTech
    {
        public string requestId { get; set; }
        public string error { get; set; }
        public string message { get; set; }
        public List<CurrencyStatsData_PlayTech> data { get; set; }
    }

    public class CurrencyStatsData_PlayTech
    {
        public decimal ggr { get; set; }
        public decimal totalWins { get; set; }
        public decimal totalCancels { get; set; }
        public int players { get; set; }
        public int games { get; set; }
        public decimal totalBets { get; set; }
        private string _currency;
        public string currency
        {
            get
            {
                if (_currency == "Playtech_N")
                {
                    return "BRL";
                }
                return _currency;
            }
            set
            {
                _currency = value;
            }
        }
        public decimal totalJpw { get; set; }
        public decimal totalRakes { get; set; }
        public decimal totalJpc { get; set; }
    }
}
