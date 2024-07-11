using Newtonsoft.Json;

namespace ThirdPartyWager.Service.Model.NineW
{
    public class NineWConfig
    {
        public string ApiUrl { get; set; }
        public string DomainUrl { get; set; }
        [JsonProperty("website")]
        public string WebSite { get; set; }
        [JsonProperty("cert")]
        public string Cert { get; set; }
        [JsonProperty("agent")]
        public string Agent { get; set; }
        public Dictionary<string, CurrencyDataNineW> CurrencyData { get; set; }
    }

    public class CurrencyDataNineW
    {
        [JsonProperty("agent")]
        public string Agent { get; set; }
    }

    public class QuerySettleBetHistoryObj<T>
    {
        public string status { get; set; }
        public int currentPage { get; set; }
        public int totalPage { get; set; }

        [JsonProperty("resultList")]
        public IEnumerable<T> BetList { get; set; }
    }

    public class SettleBet
    {
        public string eventTypeName { get; set; } = string.Empty;
        public string eventName { get; set; } = string.Empty;
        public int eventId { get; set; }
        public int site { get; set; }
        public string agentId { get; set; } = string.Empty;
        public int currencyType { get; set; }
        public int eventType { get; set; }
        public string selectionId { get; set; } = string.Empty;
        public int categoryType { get; set; }
        public string ip { get; set; } = string.Empty;
        public decimal matchOddsReq { get; set; }
        public int oddsType { get; set; }
        public string userId { get; set; } = string.Empty;
        public string betId { get; set; } = string.Empty;

        public DateTime? startTime { get; set; }
        public DateTime betPlaced
        {
            get
            {
                if (_betPlaced == null && startTime == null)
                {
                    return default;
                }

                if (startTime != null)
                {
                    return startTime.Value;
                }

                if (_betPlaced != null)
                {
                    return _betPlaced.Value;
                }

                return default;
            }
            set
            {
                _betPlaced = value;
            }
        }
        private DateTime? _betPlaced { get; set; }
        public DateTime matchSettledDate { get; set; } = new DateTime(1970, 1, 1);
        public DateTime resettledDate { get; set; } = new DateTime(1970, 1, 1);
        public string marketName { get; set; } = string.Empty;
        public string selectionName { get; set; } = string.Empty;
        public string matchType { get; set; } = string.Empty;
        public string marketId { get; set; } = string.Empty;
        public string fancyBetRuns { get; set; } = string.Empty;
        public string extension1 { get; set; } = string.Empty;
        public decimal matchAvgOdds { get; set; }
        public string betStatus { get; set; } = string.Empty;
        public decimal reduction { get; set; }
        public decimal matchStake { get; set; }
        public decimal profitLoss { get; set; }
        public string updateDate { get; set; } = string.Empty;
        public long betUnix
        {
            get
            {
                if (betPlaced == default(DateTime))
                {
                    return 0;
                }
                return new DateTimeOffset(betPlaced, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long settledUnix
        {
            get
            {
                if (matchSettledDate == new DateTime(1970, 1, 1))
                {
                    return 0;
                }
                return new DateTimeOffset(matchSettledDate, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }


        public SettleBet() { }

        public SettleBet(ReSettleBet reSettleBet)
        {
            //共同欄位
            userId = reSettleBet.userId;
            agentId = reSettleBet.agentId;
            categoryType = reSettleBet.categoryType;
            eventType = reSettleBet.eventType;
            eventTypeName = reSettleBet.eventTypeName;
            eventName = reSettleBet.eventName;
            eventId = reSettleBet.eventId;
            marketName = reSettleBet.marketName;
            marketId = reSettleBet.marketId;
            currencyType = reSettleBet.currencyType;

            //將ReSettleBet欄位對應至SettleBet
            matchSettledDate = reSettleBet.resettledDate;
            profitLoss = reSettleBet.profitLoss;
            matchStake = reSettleBet.stakeTotal;
            oddsType = reSettleBet.oddsType;
            betPlaced = reSettleBet.startTime;
            var detail = reSettleBet.transactionData?.First();

            if (detail == null)
            {
                throw new Exception($"NineW ReSettleBet to  SettleBet Exception：transactionData is null.");
            }
            betId = detail.betId;
            betStatus = detail.betStatus;
            selectionName = detail.selectionName;
            matchType = detail.matchType;
            matchOddsReq = detail.txOddsMatched;
        }

        public SettleBet(MatchedResult unSettleBet)
        {
            userId = unSettleBet.userId;
            agentId = unSettleBet.agentId;
            categoryType = unSettleBet.categoryType;
            eventType = unSettleBet.eventType;
            eventTypeName = unSettleBet.eventTypeName;
            eventName = unSettleBet.eventName;
            eventId = unSettleBet.eventId;
            marketName = unSettleBet.marketName;
            marketId = unSettleBet.marketId;
            betId = unSettleBet.betId;
            betStatus = unSettleBet.betStatus;
            selectionName = unSettleBet.selectionName;
            selectionId = unSettleBet.selectionId;
            matchType = unSettleBet.matchType;
            matchSettledDate = new DateTime(1970, 1, 1);
            profitLoss = default;
            oddsType = unSettleBet.oddsType;
            betPlaced = unSettleBet.betPlaced;
            site = unSettleBet.site;
            currencyType = unSettleBet.currencyType;

            var detail = unSettleBet.transactionData?.First();
            if (detail == null)
            {
                throw new Exception($"NineW ReSettleBet to  SettleBet Exception：transactionData is null.");
            }
            matchStake = detail.txStake;
            matchOddsReq = detail.txOddsMatched;
        }

        public SettleBet(UnmatchedResult unSettleBet)
        {
            userId = unSettleBet.userId;
            agentId = unSettleBet.agentId;
            categoryType = unSettleBet.categoryType;
            eventType = unSettleBet.eventType;
            eventTypeName = unSettleBet.eventTypeName;
            eventName = unSettleBet.eventName;
            eventId = unSettleBet.eventId;
            marketName = unSettleBet.marketName;
            marketId = unSettleBet.marketId;
            betId = unSettleBet.betId;
            betStatus = unSettleBet.betStatus;
            selectionName = unSettleBet.selectionName;
            selectionId = unSettleBet.selectionId;
            matchType = unSettleBet.matchType;
            matchSettledDate = new DateTime(1970, 1, 1);
            profitLoss = default;
            oddsType = unSettleBet.oddsType;
            betPlaced = unSettleBet.betPlaced;
            matchStake = default;
            matchOddsReq = default;
            site = unSettleBet.site;
            currencyType = unSettleBet.currencyType;
        }
    }

    public class ReSettleBet
    {
        public string userId { get; set; }
        public string agentId { get; set; }
        public int categoryType { get; set; }
        public int eventType { get; set; }
        public string eventTypeName { get; set; }
        public string eventName { get; set; }
        public int eventId { get; set; }
        public string marketName { get; set; }
        public string marketId { get; set; }
        public DateTime startTime { get; set; }
        public DateTime settledDate { get; set; }
        public DateTime resettledDate { get; set; } = default;
        public decimal profitLoss { get; set; }
        public decimal marketTotal { get; set; }
        public decimal commission { get; set; }
        public decimal agentCommission { get; set; }
        public decimal netTotal { get; set; }
        public decimal stakeTotal { get; set; }
        public int currencyType { get; set; }
        public Transactiondata[] transactionData { get; set; }
        public decimal backSubTotal { get; set; }
        public decimal laySubTotal { get; set; }
        public int oddsType { get; set; }
    }

    public class Transactiondata
    {
        public string betId { get; set; }
        public string selectionName { get; set; }
        public decimal txOddsMatched { get; set; }
        public decimal txStake { get; set; }
        public string matchType { get; set; }
        public string placed { get; set; }
        public int? fancyBetRuns { get; set; } = 0;
        public decimal txLiability { get; set; }
        public string betStatus { get; set; }
    }

    public class CountSummaryTransaction
    {
        public string status { get; set; }
        public Checkdata[] checkData { get; set; }
    }

    public class Checkdata
    {
        public int currency { get; set; }
        public string settledDateStart { get; set; }
        public string settledDateEnd { get; set; }
        public decimal profitLoss { get; set; }
        public decimal commission { get; set; }
        public decimal stakeTotal { get; set; }
    }

    public class SummaryTransaction
    {
        public string userId { get; set; }
        public string agentId { get; set; }
        public int categoryType { get; set; }
        public int eventType { get; set; }
        public string eventTypeName { get; set; }
        public string eventName { get; set; }
        public int eventId { get; set; }
        public string marketName { get; set; }
        public decimal marketId { get; set; }
        public DateTime startTime { get; set; }
        public DateTime settledDate { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? resettledDate { get; set; } = null;
        public decimal profitLoss { get; set; }
        public decimal marketTotal { get; set; }
        public decimal commission { get; set; }
        public decimal agentCommission { get; set; }
        public decimal stakeTotal { get; set; }
        public int currencyType { get; set; }
        public long startTimeUnix
        {
            get
            {
                return new DateTimeOffset(startTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long settledDateUnix
        {
            get
            {
                return new DateTimeOffset(settledDate, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long resettledDateUnix
        {
            get
            {
                return resettledDate != null ? new DateTimeOffset(resettledDate.Value, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds() : 0;
            }
        }
    }


    public class UnSettleBet
    {
        public string status { get; set; }
        public int totalPage { get; set; }
        public int currentPage { get; set; }
        public Matched matched { get; set; }
        public Unmatched unmatched { get; set; }
        public long lastUpdateDate { get; set; }
    }

    public class Matched
    {
        public MatchedResult[] resultList { get; set; }
    }

    public class MatchedResult
    {
        public string eventTypeName { get; set; }
        public string eventName { get; set; }
        public int eventId { get; set; }
        public int site { get; set; }
        public int currencyType { get; set; }
        public int eventType { get; set; }
        public string selectionId { get; set; }
        public int categoryType { get; set; }
        public string ip { get; set; }
        public decimal matchOddsReq { get; set; }
        public string userId { get; set; }
        public string betId { get; set; }
        public int oddsType { get; set; }
        public long updateDate { get; set; }
        public string agentId { get; set; }
        public DateTime betPlaced { get; set; }
        public string matchSettledDate { get; set; }
        public string marketName { get; set; }
        public string selectionName { get; set; }
        public string matchType { get; set; }
        public string marketId { get; set; }
        public decimal? fancyBetRuns { get; set; }
        public decimal? fancyBetExtendedRuns { get; set; }
        public int fancyBetMarketType { get; set; }
        public object extension1 { get; set; }
        public UnSettleBetTransactiondata[] transactionData { get; set; }
        public decimal? matchAvgOdds { get; set; }
        public string betStatus { get; set; }
        public decimal? matchAmount { get; set; }
        public decimal? unMatchAmount { get; set; }
        public DateTime dateMatched { get; set; }
    }

    public class UnSettleBetTransactiondata
    {
        public DateTime betTaken { get; set; }
        public decimal txStake { get; set; }
        public decimal txLiability { get; set; }
        public decimal txOddsMatched { get; set; }
        public decimal actualOdds { get; set; }
        public decimal? txFancyBetRuns { get; set; }
    }

    public class Unmatched
    {
        public UnmatchedResult[] resultList { get; set; }
    }

    public class UnmatchedResult
    {
        public string eventTypeName { get; set; }
        public string eventName { get; set; }
        public int eventId { get; set; }
        public int site { get; set; }
        public int currencyType { get; set; }
        public int eventType { get; set; }
        public string selectionId { get; set; }
        public int categoryType { get; set; }
        public string ip { get; set; }
        public decimal matchOddsReq { get; set; }
        public string userId { get; set; }
        public string betId { get; set; }
        public int oddsType { get; set; }
        public long updateDate { get; set; }
        public string agentId { get; set; }
        public DateTime betPlaced { get; set; }
        public string matchSettledDate { get; set; }
        public string marketName { get; set; }
        public string selectionName { get; set; }
        public string matchType { get; set; }
        public string marketId { get; set; }
        public decimal? fancyBetRuns { get; set; }
        public decimal? fancyBetExtendedRuns { get; set; }
        public int fancyBetMarketType { get; set; }
        public object extension1 { get; set; }
        public object transactionData { get; set; }
        public decimal? matchAvgOdds { get; set; }
        public string betStatus { get; set; }
        public decimal? matchAmount { get; set; }
        public decimal? unMatchAmount { get; set; }
    }
    
    public class QueryDomainResponse
    {
        public string status { get; set; }
          
        [JsonProperty("privateDomains")]
        public List<DomainModel> PrivateDomains { get; set; }

        [JsonProperty("domains")]
        public List<DomainModel> Domains { get; set; }
    }

    public class DomainModel
    {
        public string domain { get; set; }
        public int priority { get; set; }
    }
    
}
