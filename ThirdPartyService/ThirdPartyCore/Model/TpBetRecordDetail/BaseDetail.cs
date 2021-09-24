using System;
using System.Collections.Generic;
using System.Text;

namespace ThirdPartyCore.Model
{
    public class BaseDetail
    {
        public int TpId { get; }

        public int TpCategoryId { get; }

        public int GameType { get; }

        public string TransId { get; }

        public string Detail { get; }

        public TpCurrency TpCurrency { get; }

        public BaseDetail(GetDetailObject detailObject)
        {
            TpId = detailObject.TpBetRecordDetail.TpId;
            TpCategoryId = detailObject.TpCategoryId;
            GameType = detailObject.TpBetRecordDetail.GameType;
            TransId = detailObject.TpBetRecordDetail.TransId;
            Detail = detailObject.TpBetRecordDetail.Detail;
            TpCurrency = detailObject.TpCurrency;
        }

    }
}
