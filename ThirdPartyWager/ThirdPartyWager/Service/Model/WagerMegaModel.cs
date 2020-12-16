using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyWager.Service.Model
{
    public class WagerMegaReport
    {
        public string code { get; set; }
        public string message { get; set; }
        public IList<ReportMega> result { get; set; }
    }

    public class ReportMega
    {
        public long userId { get; set; }
        public string loginId { get; set; }
        public long idx { get; set; }
        public decimal bet { get; set; }
        public decimal win { get; set; }
        public decimal yield { get; set; }
        public string memo { get; set; }
        public DateTime WagerDate { get; set; }
        public long WagerUnix
        {
            get
            {
                return new DateTimeOffset(WagerDate, new TimeSpan(-4, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public string Currency { get; set; }
    }

}
