using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyWager.Service.Model
{

    public class TicketAGModel
    {
        public TicketResult result { get; set; }
    }

    public class TicketResult
    {
        public TicketAG row { get; set; }
    }

    public class TicketAG
    {
        [JsonProperty("@billNo")]
        public string billNo { get; set; }

        [JsonProperty("@status")]
        public long status { get; set; }
    }
}
