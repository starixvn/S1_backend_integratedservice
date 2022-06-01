using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPartyWager.Service.Model.NineW
{
    public class NineWConfig
    {
        public string ApiUrl { get; set; }
        public string WagerUrl { get; set; }
        public string WebSite { get; set; }
        public string Cert { get; set; }
        public string Agent { get; set; }

        public NineWConfig(Dictionary<string, string> inputs)
        {
            ApiUrl = inputs.ContainsKey($"ApiUrl") && !string.IsNullOrEmpty(inputs[$"ApiUrl"])
            ? inputs[$"ApiUrl"] : throw new Exception($"ApiUrl is empty");
            WagerUrl = inputs.ContainsKey($"WagerUrl") && !string.IsNullOrEmpty(inputs[$"WagerUrl"])
            ? inputs[$"WagerUrl"] : throw new Exception($"WagerUrl is empty");
            WebSite = inputs.ContainsKey($"website") && !string.IsNullOrEmpty(inputs[$"website"])
            ? inputs[$"website"] : throw new Exception($"website is empty");
            Cert = inputs.ContainsKey($"cert") && !string.IsNullOrEmpty(inputs[$"cert"])
            ? inputs[$"cert"] : throw new Exception($"cert is empty");
            Agent = inputs.ContainsKey($"agent") && !string.IsNullOrEmpty(inputs[$"agent"])
            ? inputs[$"agent"] : throw new Exception($"agent is empty");
        }
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
        public string selectionId { get; set; }
        public int categoryType { get; set; }
        public string ip { get; set; } = string.Empty;
        public decimal matchOddsReq { get; set; }
        public string userId { get; set; } = string.Empty;
        public string betId { get; set; } = string.Empty;
        public DateTime betPlaced { get; set; } = default;
        public DateTime matchSettledDate { get; set; } = default;
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
                return new DateTimeOffset(betPlaced, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long settledUnix
        {
            get
            {
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

            //將ReSettleBet欄位對應至SettleBet
            matchSettledDate = reSettleBet.resettledDate;
            profitLoss = reSettleBet.profitLoss;
            matchStake = reSettleBet.stakeTotal;
            var detail = reSettleBet.transactionData?.First();
            if (detail == null)
            {
                throw new Exception($"NineW ReSettleBet to  SettleBet Exception：transactionData is null.");
            }
            betId = detail.betId;
            betStatus = detail.betStatus;
            selectionName = detail.selectionName;
            matchType = detail.matchType;
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
        public string startTime { get; set; }
        public string settledDate { get; set; }
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
    }

    public class Transactiondata
    {
        public string betId { get; set; }
        public string selectionName { get; set; }
        public decimal txOddsMatched { get; set; }
        public decimal txStake { get; set; }
        public string matchType { get; set; }
        public string placed { get; set; }
        public int fancyBetRuns { get; set; }
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

}
