using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_Iconic
    {
        public string token { get; set; }
        public DateTime roundCreatedAt { get; set; }
        public DateTime roundUpdatedAt { get; set; }
        public long transactionId { get; set; }
        public int roundId { get; set; }
        public int gameId { get; set; }
        public string productId { get; set; }
        public decimal amount { get; set; }
        public string hash { get; set; }
        public string username { get; set; }
    }
}
