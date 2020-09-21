using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_CQ9
    {
        public string account { get; set; }
        public string eventTime { get; set; }
        public string gamehall { get; set; }
        public string gamecode { get; set; }
        public string roundid { get; set; }
        public decimal amount { get; set; }
        public string mtcode { get; set; }
        public string session { get; set; }
    }

    public class EndRoundDataRNG_CQ9
    {
        public string account { get; set; }
        public string gamehall { get; set; }
        public string gamecode { get; set; }
        public string roundid { get; set; }
        public string createTime { get; set; }
        public IEnumerable<EndRound_CQ9> data { get; set; }
    }

    public class EndRound_CQ9
    {
        public string mtcode { get; set; }
        public decimal amount { get; set; }
        public string eventtime { get; set; }
    }

    public class RefundDataRNG_CQ9
    {
        public string mtcode { get; set; }
    }
}
