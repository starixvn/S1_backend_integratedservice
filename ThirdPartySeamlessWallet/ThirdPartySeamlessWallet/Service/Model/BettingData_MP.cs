using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingData_MP
    {
    }

    public class TransferData_MP
    {
        public string channelId { get; set; }
        public string account { get; set; }
        public decimal requestAmount { get; set; }
        public string gameId { get; set; }
        public string orderId { get; set; }
        public string roundId { get; set; }
        public string gameNo { get; set; }
        public string player
        {
            get
            {
                string[] acc = account.Split('_');
                return acc[1];
            }
        }
    }

    public class SettleDataPK_MP : SettleData
    {
        public int GameType { get; set; }
    }
}
