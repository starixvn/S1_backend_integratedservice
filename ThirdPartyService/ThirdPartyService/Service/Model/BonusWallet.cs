using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyService.Service.Model
{
    public class BonusWallet
    {
        public int BonusId { get; set; }
        public decimal Bonus { get; set; }
        public int PromoCategoryId { get; set; }
        public decimal Rollover { get; set; }
        public decimal Turnover { get; set; }
        public int Status { get; set; }
        public int TpId { get; set; }
    }
}
