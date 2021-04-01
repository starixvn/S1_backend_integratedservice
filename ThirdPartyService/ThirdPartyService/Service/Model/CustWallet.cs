using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyService.Service.Model
{
    public class CustWalletModel
    {
        public decimal Balance { set; get; }
        public int AutoTransfer { set; get; }
        public int InGame { set; get; }
    }
}
