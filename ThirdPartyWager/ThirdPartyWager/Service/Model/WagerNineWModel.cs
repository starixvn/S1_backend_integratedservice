using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyWager.Service.Model
{
    public class NineWConfig
    {
        public string WagerUrl { get; set; }
        public string WebSite { get; set; }
        public string Cert { get; set; }
        public string Agent { get; set; }

        public NineWConfig(Dictionary<string, string> inputs)
        {
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


    public class QuerySettleBetHistoryObj
    {
        public string status { get; set; }
        public int currentPage { get; set; }
        public int totalPage { get; set; }

        [JsonProperty("resultList")]
        public IEnumerable<SettleBet> SettleBetList { get; set; }
    }

    public class SettleBet
    {
        public string eventTypeName { get; set; }
        public string eventName { get; set; }
        public int eventId { get; set; }
        public int site { get; set; }
        public string agentId { get; set; }
        public int currencyType { get; set; }
        public int eventType { get; set; }
        public int selectionId { get; set; }
        public int categoryType { get; set; }
        public string ip { get; set; }
        public decimal matchOddsReq { get; set; }
        public string userId { get; set; }
        public string betId { get; set; }
        public string betPlaced { get; set; }
        public string matchSettledDate { get; set; }
        public string marketName { get; set; }
        public string selectionName { get; set; }
        public string matchType { get; set; }
        public string marketId { get; set; }
        public string fancyBetRuns { get; set; }
        public string extension1 { get; set; }
        public decimal matchAvgOdds { get; set; }
        public string betStatus { get; set; }
        public decimal reduction { get; set; }
        public decimal matchStake { get; set; }
        public decimal profitLoss { get; set; }
        public string updateDate { get; set; }
    }

    public class Transactiondata
    {
        public string betTaken { get; set; }
        public float txStake { get; set; }
        public float txOddsMatched { get; set; }
        public float actualOdds { get; set; }
        public object txFancyBetRuns { get; set; }
    }

}
