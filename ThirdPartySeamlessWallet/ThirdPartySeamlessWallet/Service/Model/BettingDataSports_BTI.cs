using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{

    public class BettingDataBTIReserve
    {
        public string cust_id { get; set; }

        public string reserve_id { get; set; }

        public decimal amount { get; set; } = 0;

        public string req_id { get; set; }

        public string requestBody { get; set; }
    }



}
