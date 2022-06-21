using System;
using System.Collections.Generic;
using System.Text;
using ThirdPartyCore.Model.enums;

namespace ThirdPartyCore.Model.TpBetRecordExtend
{
    public class TpBetRecordExtend
    {
        public TpBetRecordExtend()
        {

        }
        public TpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo)
        {
            TpId = betRecordInfo.TpId;
            TransId = betRecordInfo.TransId;
            GameType = betRecordInfo.GameType;     
            
            ProviderID = tpGameType.TpId.ToString();
            Provider = tpGameType.TpName;
            Product = tpGameType.TypeName;
            Category = tpGameType.CategoryId.ToString();

            Currency = tpCustInfo.TpCurrencyId;
        }

        #region private Set

        public int TpId { get; private set; }
        public string TransId { get; private set; }
        public int GameType { get; private set; }
        /// <summary>
        /// 我方TpId
        /// </summary>
        public string ProviderID { get; private set; }
        /// <summary>
        /// 第三方幣別代碼 = TpCurrencyId
        /// </summary>
        public string Currency { get; private set; }
        /// <summary>
        /// 我方TpName
        /// </summary>
        public string Provider { get; private set; }
        /// <summary>
        /// 遊戲產品 = 我方TpGameTypeName
        /// </summary>
        public string Product { get; private set; }
        /// <summary>
        /// 我方遊戲分類名稱
        /// </summary>
        public string Category { get; private set; }

        #endregion

        /// <summary>
        /// 關聯單號
        /// </summary>
        public string RefNo { get; set; } = string.Empty;
        /// <summary>
        /// 遊戲代碼 (GameCode)
        /// </summary>
        public string GameID { get; set; } = string.Empty;

        /// <summary>
        /// 遊戲名稱 (僅支援英文)
        /// </summary>
        public string Game { get; set; } = string.Empty;
        /// <summary>
        /// 遊戲標題
        /// </summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>
        /// 局號/桌號/賽事號/回合號
        /// </summary>
        public string MatchID { get; set; } = string.Empty;
        /// <summary>
        /// 下注類型
        /// </summary>
        public string BetType { get; set; } = string.Empty;
        /// <summary>
        /// 下注項目
        /// </summary>
        public string BetChoice { get; set; } = string.Empty;
        /// <summary>
        /// 遊戲結果
        /// </summary>
        public string Result { get; set; } = string.Empty;
        /// <summary>
        /// 下注賠率
        /// </summary>
        public decimal Odds { get; set; } = 0;
        /// <summary>
        /// 賠率類型
        /// </summary>
        public string OddsType { get; set; } = string.Empty;

        /// <summary>
        /// 下注裝置
        /// </summary>
        public string Platform 
        { 
            get
            {
                return DeviceType.ToString();
            }
        } 

        /// <summary>
        /// 下注裝置
        /// </summary>
        public DeviceType DeviceType { get; set; } = DeviceType.None;

        /// <summary>
        /// 滾球
        /// </summary>
        public string Live { get; set; } = string.Empty;
        /// <summary>
        /// 玩家IP位址
        /// </summary>
        public string IP { get; set; } = string.Empty;
    }
}
