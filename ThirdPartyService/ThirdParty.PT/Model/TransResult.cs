using System;

namespace ThirdParty.PT
{
    public partial class TpHelper
    {
        private class TransResult
        {
            public decimal amount { get; set; }
            public string currentplayerbalance { get; set; }
            public string executiontime { get; set; }
            public string externaltransactionid { get; set; }
            public string instantcashtype { get; set; }
            public string kiosktransactionid { get; set; }
            public DateTime kiosktransactiontime { get; set; }
            public string ptinternaltransactionid { get; set; }
            public string result { get; set; }
        }
    }
}