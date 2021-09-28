using System;
using System.Collections.Generic;
using System.Text;

namespace ThirdPartyCore.Model
{
    public class BaseDetail
    {
        public int TpId { get; }

        public int TpCategoryId { get;  }

        public int GameType { get;}

        public string GameTypeName { get;}

        /// <summary>
        /// 注单识别码
        /// </summary>
        public string TransId { get; set; }

        /// <summary>
        /// 第三方回傳細單資料(Json格式)
        /// </summary>
        public string Detail { get; set; }

        public TpCurrency TpCurrency { get; set; }

        public BaseDetail(TpGameTypeOption tpGameTypeOption)
        {
            TpId =tpGameTypeOption.TpId;
            TpCategoryId = (int)tpGameTypeOption.CategoryId;
            GameType = tpGameTypeOption.Type;
            GameTypeName = tpGameTypeOption.TypeName;
        }

    }
}
