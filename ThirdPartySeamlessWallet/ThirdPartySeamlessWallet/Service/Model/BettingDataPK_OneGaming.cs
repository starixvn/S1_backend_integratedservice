using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataPK_OneGaming
    {
        public decimal amount { get; set; }
        public string transaction_id { get; set; }
        public string l_token { get; set; }
        public string game_key { get; set; }
        public string game_name { get; set; }
        public string bcode { get; set; }
        public string datetime { get; set; }
        public string player_id { get; set; }
        public string ref_tx_id { get; set; }
        public long dateUnix
        {
            get
            {
                if (!string.IsNullOrEmpty(datetime))
                {
                    var time = DateTime.ParseExact(datetime, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                    return new DateTimeOffset(time, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                }
                return 0;
            }
        }
    }

    public class RollbackDataPK_OneGaming
    {
        public decimal amount { get; set; }
        public string transaction_id { get; set; }
        public string type { get; set; }
        public string l_token { get; set; }
        public string game_key { get; set; }
        public string game_name { get; set; }
        public string bcode { get; set; }
        public string datetime { get; set; }
        public string player_id { get; set; }
        public long dateUnix
        {
            get
            {
                if (!string.IsNullOrEmpty(datetime))
                {
                    var time = DateTime.ParseExact(datetime, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                    return new DateTimeOffset(time, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                }
                return 0;
            }
        }
    }

    public class SettleDataPK_OneGaming
    {
        public string ticket { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal Revenue { get; set; }
        public decimal WinLostAmount { get; set; }
        public string Account { get; set; }
        public long SettleTime { get; set; }
    }
}
