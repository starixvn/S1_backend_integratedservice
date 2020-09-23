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

    public class Record_CQ9
    {
        public string roundid { get; set; }
        public decimal amount { get; set; }
        public DateTime eventtime { get; set; }
        public string Actions { get; set; }
        public string account { get; set; }
        public string currency { get; set; }
        public DateTime CreateTimes { get; set; }
        public DateTime UpdateTime { get; set; }
        public int TicketStatus { get; set; }
        public string CreateFormat
        {
            get
            {
                return new DateTimeOffset(CreateTimes, new TimeSpan(-4, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK");
            }
        }
        public string UpdateFormat
        {
            get
            {
                return new DateTimeOffset(UpdateTime, new TimeSpan(-4, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK");
            }
        }
        public string eventFormat
        {
            get
            {
                return new DateTimeOffset(eventtime, new TimeSpan(-4, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ssK");
            }
        }
    }

    public class RefundDataRNG_CQ9
    {
        public string mtcode { get; set; }
    }

    public class SettleDataRNG_CQ9
    {
        public string mtcode { get; set; }
        public string account { get; set; }
        public string roundid { get; set; }
        public decimal amount { get; set; }
        public long createUnix { get; set; }
    }
}
