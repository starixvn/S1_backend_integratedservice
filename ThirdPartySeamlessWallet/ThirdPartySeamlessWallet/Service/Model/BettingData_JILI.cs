using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingData_JILI
    {
        public string reqId { get; set; }
        public string token { get; set; }
        public string currency { get; set; }
        public int game { get; set; }
        public long round { get; set; }
        public long wagersTime { get; set; }
        public decimal betAmount { get; set; }
        public decimal winloseAmount { get; set; }
        public string userId { get; set; }
        public bool isFreeRound { get; set; }
        public long TransactionID { get; set; }
        
    }

    public class CancelBetData_JILI
    {
        public string reqId { get; set; }
        public string currency { get; set; }
        public int game { get; set; }
        public long round { get; set; }
        public decimal betAmount { get; set; }
        public decimal winloseAmount { get; set; }
        public string userId { get; set; }
    }

    //public class RecordData_JILI
    //{
    //    public string currency { get; set; }
    //    public int game { get; set; }
    //    public long round { get; set; }
    //    public decimal betAmount { get; set; }
    //    public decimal winloseAmount { get; set; }
    //    public string TpUserName { get; set; }
    //}

    public class BettransData_JILI : BettingData_JILI
    {
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
    }

    public class SettleData_JILI
    {
        public long TransId { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinLostAmount { get; set; }
        public string Account { get; set; }
        public long SettleTime { get; set; }
        public int TicketStatus { get; set; }
        public int GameId { get; set; }
    }
}
