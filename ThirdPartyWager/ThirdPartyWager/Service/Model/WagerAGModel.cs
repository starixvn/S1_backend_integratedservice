using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ThirdPartyWager.Service.Model
{
    public class WagerAGModel
    {
        public Result result { get; set; }
    }

    public class Result
    {
        public int info { get; set; }
        public IList<BetAG> row { get; set; }
        public Addition addition { get; set; }
        public string Code { get; set; }
    }

    public class Addition
    {
        public int total { get; set; }
        public int num_per_page { get; set; }
        public int currentpage { get; set; }
        public int totalpage { get; set; }
        public int perpage { get; set; }
    }

    public class BetAG
    {
        [JsonProperty("@billNo")]
        public long billNo { get; set; }

        [JsonProperty("@playName")]
        public string playName { get; set; }

        [JsonProperty("@gameCode")]
        public string gameCode { get; set; }

        [JsonProperty("@netAmount")]
        public decimal netAmount { get; set; }

        [JsonProperty("@betTime")]
        public DateTime betTime { get; set; }

        [JsonProperty("@betAmount")]
        public decimal betAmount { get; set; }

        [JsonProperty("@validBetAmount")]
        public decimal validBetAmount { get; set; }

        [JsonProperty("@flag")]
        public int flag { get; set; }

        [JsonProperty("@playType")]
        public int playType { get; set; }

        [JsonProperty("@currency")]
        public string currency { get; set; }

        [JsonProperty("@tableCode")]
        public string tableCode { get; set; }

        [JsonProperty("@recalcuTime")]
        public DateTime recalcuTime { get; set; }

        [JsonProperty("@beforeCredit")]
        public decimal beforeCredit { get; set; }

        [JsonProperty("@betIP")]
        public string betIP { get; set; }

        [JsonProperty("@platformType")]
        public string platformType { get; set; }

        [JsonProperty("@remark")]
        public string remark { get; set; }

        [JsonProperty("@round")]
        public string round { get; set; }

        [JsonProperty("@result")]
        public string result { get; set; }

        [JsonProperty("@gameType")]
        public string gameType { get; set; }

        [JsonProperty("@deviceType")]
        public int deviceType { get; set; }

        [JsonProperty("@wagersTotal")]
        public int wagersTotal { get; set; }

        public long betUnix
        {
            get
            {
                return new DateTimeOffset(betTime, new TimeSpan(-4, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long recalcuUnix
        {
            get
            {
                return new DateTimeOffset(recalcuTime, new TimeSpan(-4, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class WagerAGTotal
    {
        public int wagerCount { get; set; }
    }
}
