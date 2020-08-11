using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyWager.Service.Model
{
    public class WagerDGModel
    {
        public int codeId { get; set; }
        public string token { get; set; }
        public string random { get; set; }
        public IEnumerable<Bet> list { get; set; }
    }

    public class WagerRedoDGModel
    {
        public int codeId { get; set; }
        public Data data { get; set; }
    }


    public class Bet
    {
        public long id { get; set; }
        public int tableId { get; set; }
        public long shoeId { get; set; }
        public long playId { get; set; }
        public int lobbyId { get; set; }
        public int gameType { get; set; }
        public int gameId { get; set; }
        public long memberId { get; set; }
        public int parentId { get; set; }
        public DateTime betTime { get; set; }
        public DateTime calTime { get; set; }
        public decimal winOrLoss { get; set; }
        public decimal balanceBefore { get; set; }
        public decimal betPoints { get; set; }
        public decimal betPointsz { get; set; }
        public decimal availableBet { get; set; }
        public string userName { get; set; }
        public string result { get; set; }
        public string betDetail { get; set; }
        public string ip { get; set; }
        public string ext { get; set; }
        public int isRevocation { get; set; }
        public int currencyId { get; set; }
        public int deviceType { get; set; }
        public string serial { get; set; }
        public int roadid { get; set; }
        public long pluginid { get; set; }
        public long betUnix
        {
            get
            {
                return new DateTimeOffset(betTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long calUnix
        {
            get
            {
                return new DateTimeOffset(calTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class Data
    {
        public IList<Bet> records { get; set; }
        public int pageCount { get; set; }
        public int pageSize { get; set; }
        public int pageNow { get; set; }
        public int rowCount { get; set; }
        public int pagecode { get; set; }
        public Pageindex pageindex { get; set; }
    }

    public class Pageindex
    {
        public int startindex { get; set; }
        public int endindex { get; set; }

    }
}
