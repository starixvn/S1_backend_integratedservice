using System;
using System.Collections.Generic;
using System.Text;
using ThirdPartyCore.Model.enums;

namespace ThirdPartyCore.Model
{
    public class TpGameTypeOption
    {
        public int Type { get; set; }
        public string TypeName { get; set; }
        public int TpId { get; set; }
        public string TpName { get; set; }
        public TpCategory CategoryId { get; set; }
    }
}
