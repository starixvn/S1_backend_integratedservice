using System;
using System.Collections.Generic;
using ThirdPartySeamlessWallet.Extensions;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class TpUnsettle
    {
        public string TransId { get; set; }

        public int SettleFlag { get; set; }

        public DateTime PartitionTime { get; set; }
    }
}
