using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingData_CF
    {
        public string api_key { get; set; }
        public string agent_code { get; set; }
        public long ticket_id { get; set; }
        public string login_id { get; set; }
        public string arena_code { get; set; }
        public string match_no { get; set; }
        public DateTime match_date { get; set; }
        public int fight_no { get; set; }
        public DateTime fight_datetime { get; set; }
        public string bet_on { get; set; }
        public decimal odds_given { get; set; }
        public decimal stake { get; set; }
        public decimal stake_money { get; set; }
        public DateTime created_datetime { get; set; }
        public TicketStatus TicketStatus { get; set; }
    }

    public class PayoutData_CF
    {
        public long ticket_id { get; set; }
        public string login_id { get; set; }
        public decimal winloss { get; set; }
        public decimal payout { get; set; }
        public int SettleFlag { get; set; }
        public string status { get; set; }
    }
   
    public class SettleData_CF
    {
        public long ticket_id { get; set; }
        public string login_id { get; set; }
        public decimal stake { get; set; }
        public decimal stake_money { get; set; }
        public decimal winloss { get; set; }
        public decimal payout { get; set; }
        public string status { get; set; }
    }

    public class CancelBetData_CF
    {
        public string api_key { get; set; }
        public string agent_code { get; set; }
        public long ticket_id { get; set; }
        public string login_id { get; set; }
        public decimal stake { get; set; }
        public decimal stake_money { get; set; }
    }

    public class BetRecordData_CF
    {
        public long ticket_id { get; set; }
        public string login_id { get; set; }
        public int SettleFlag { get; set; }
        public decimal Payout { get; set; }
    }

}
