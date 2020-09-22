using Newtonsoft.Json;
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
        public long? eventUnix { get; set; }
    }

    public class EndRoundDataRNG_CQ9
    {
        public string account { get; set; }
        public string gamehall { get; set; }
        public string gamecode { get; set; }
        public string roundid { get; set; }
        public string createTime { get; set; }
        public string data { get; set; }
        public IEnumerable<EndRound_CQ9> detail
        {
            get
            {
                return JsonConvert.DeserializeObject<IEnumerable<EndRound_CQ9>>(data);
            }
        }
    }

    public class EndRound_CQ9
    {
        public string mtcode { get; set; }
        public decimal amount { get; set; }
        public string eventtime { get; set; }
        public decimal validbet { get; set; }
        public long? eventUnix { get; set; }
        public long? createUnix { get; set; }
    }

    public class RefundDataRNG_CQ9
    {
        public string mtcode { get; set; }
    }
}
