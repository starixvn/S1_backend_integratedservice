using System;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class TpUnReward
    {
        public string TransId { get; set; }

        public int SettleFlag { get; set; }

        public DateTime PartitionTime { get; set; }
    }

    public class SettleRewardData
    {
        public string TransId { get; set; }
        public decimal Amount { get; set; }
        public string Account { get; set; }
        public long SettleTime { get; set; }
    }
}
