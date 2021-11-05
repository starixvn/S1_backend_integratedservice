using System;

namespace ThirdPartyCore.Model.TpBetRecordDetail.Response
{
    public class SportsDetail : BaseDetail
    {
        public SportsDetail(TpGameTypeOption tpGameTypeOption) : base(tpGameTypeOption) { }

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
        /// 注单赔率
        /// </summary>
        public string Odds { get; set; }

        /// <summary>
        /// 投注金额
        /// </summary>
        public decimal Stake { get; set; }

        /// <summary>
        /// 输赢金额
        /// </summary>
        public decimal WinlostAmount { get; set; }

        /// <summary>
        /// 联盟名称
        /// </summary>
        public string LeagueName { get; set; }

        /// <summary>
        /// 主队名称
        /// </summary>
        public string HomeTeamName { get; set; }

        /// <summary>
        /// 客队名称
        /// </summary>
        public string AwayTeamName { get; set; }

        /// <summary>
        /// 主队让球
        /// </summary>
        public string HomeHandicap { get; set; }

        /// <summary>
        /// 客队让球
        /// </summary>
        public string AwayHandicap { get; set; }

        /// <summary>
        /// 让球
        /// </summary>
        public string Handicap { get; set; }

        /// <summary>
        /// 注单状态
        /// </summary>
        public string TicketStatus { get; set; }

        /// <summary>
        /// 下注时客队得分
        /// </summary>
        public string AwayScore { get; set; }

        /// <summary>
        /// 下注时主队得分
        /// </summary>
        public string HomeyScore { get; set; }

        /// <summary>
        /// 賽事名稱
        /// </summary>
        public string EventName { get; set; }


        /// <summary>
        /// 下注名稱
        /// </summary>
        public string SelectionName { get; set; }
    }
}
