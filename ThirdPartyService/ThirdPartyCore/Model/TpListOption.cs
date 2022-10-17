using System;
using System.Collections.Generic;
using System.Text;
using ThirdPartyCore.Model.enums;

namespace ThirdPartyCore.Model
{
    public class TpListOption
    {
        public int Id { get; set; }
        public string TpName { get; set; }
        public string Setting { get; set; }
        public int Status { get; set; }
        public long TicketKey { get; set; }
    }
}
