using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataLC_Allbet
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("client")]
        public string Client { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("details")]
        public List<Detail> Details { get; set; }

        [JsonProperty("originalTranId")]
        public long OriginalTranId { get; set; }

        [JsonProperty("tranId")]
        public long TranId { get; set; }

        [JsonProperty("transferType")]
        public int TransferType { get; set; }
    }

    public class Detail
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("betNum")]
        public long BetNum { get; set; }
    }

    public class UnsettleBettingDataLC_Allbet
    {
        public long BetNum { get; set; }
        public string Client { get; set; }
    }
}
