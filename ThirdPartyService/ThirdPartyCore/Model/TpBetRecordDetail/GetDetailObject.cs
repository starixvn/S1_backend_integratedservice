using System;
using System.Collections.Generic;
using System.Text;

namespace ThirdPartyCore.Model
{
    public class GetDetailObject
    {
        public int TpCategoryId { get; set; }

        public TpCurrency TpCurrency { get; set; }

        public TpBetRecordDetailModel TpBetRecordDetail { get; set; }
    }
}
