using System;

namespace ThirdPartyCore.Model.TpBetRecordDetail.Response
{
    public class PokerDetail : BaseDetail
    {
        public PokerDetail(TpGameTypeOption tpGameTypeOption) : base(tpGameTypeOption) { }

        /// <summary>
        /// 關聯單號
        /// </summary>
        public string RefTransId { get; set; }

        /// <summary>
        /// 会员识别码
        /// </summary>
        public string TpUserName { get; set; }

        /// <summary>
        /// 投注交易时间 (UTC+0)
        /// </summary>
        public DateTime UTCBetTime { get; set; }

        /// <summary>
        /// 注单结算的时间 (UTC+0)
        /// </summary>
        public DateTime UTCSettledTime { get; set; }

        /// <summary>
        /// 投注金额
        /// </summary>
        public decimal Stake { get; set; }

        /// <summary>
        /// 输赢金额
        /// </summary>
        public decimal WinlostAmount { get; set; }

        /// <summary>
        /// 遊戲代號
        /// </summary>
        public string GameCode { get; set; }

        /// <summary>
        /// 遊戲名稱
        /// </summary>
        public string GameName { get; set; }
    }
}