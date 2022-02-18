using System;
using System.Collections.Generic;

namespace ThirdPartyWager.Service.Model
{
    public class WagerIconicModel
    {
        public IList<BetIconic> data { get; set; }
        public int page { get; set; }
        public int pageSize { get; set; }
        public int totalSize { get; set; }
        public ErrorIconic error { get; set; }
    }

    public class BetIconic
    {
        public string id { get; set; }
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
        public decimal win { get; set; }
        public decimal bet { get; set; }
        public decimal validBet { get; set; }
        public string status { get; set; }
        public int parentId { get; set; }
        public string parent { get; set; }
        public int playerId { get; set; }
        public string player { get; set; }
        public int gameId { get; set; }
        public string game { get; set; }
        public string gameType { get; set; }
        public string productId { get; set; }
        public string currency { get; set; }
        public int count { get; set; }
        public long startUnix
        {
            get
            {
                return new DateTimeOffset(startTime).ToUnixTimeSeconds();
            }
        }
        public long endUnix
        {
            get
            {
                return new DateTimeOffset(endTime).ToUnixTimeSeconds();
            }
        }
        public int SettleFlag {
            get
            {
                
                return (status == "finish") ? 0 : 1;
            }
        }
    }

    public class ErrorIconic
    {
        public int status { get; set; }
        public string message { get; set; }
        public string ip { get; set; }

    }

    public class JwtTokenIconic
    {
        public int status { get; set; }
        public string token { get; set; }
        public ErrorIconic error { get; set; }
    }
}
