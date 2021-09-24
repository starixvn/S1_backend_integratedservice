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

        public string GameTypeName { get; }

        public string TransId { get; }

        public string Detail { get; }

        public TpCurrency TpCurrency { get; }

        public BaseDetail(GetDetailObject detailObject)
        {
            TpId = detailObject.TpBetRecordDetail.TpId;
            TpCategoryId = (int)detailObject.TpGameTypeOption.CategoryId;
            GameType = detailObject.TpBetRecordDetail.GameType;
            GameTypeName = detailObject.TpGameTypeOption.TypeName;
            TransId = detailObject.TpBetRecordDetail.TransId;
            Detail = detailObject.TpBetRecordDetail.Detail;
            TpCurrency = detailObject.TpCurrency;
        }

    }
}
