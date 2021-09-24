using System;

namespace ThirdPartyCore.Model.TpBetRecordDetail.Interfaces
{
    public interface ISportsDetail
    {

        /// <summary>
        /// 注单识别码
        /// </summary>
        string TransId { get; }

        /// <summary>
        /// 關聯單號
        /// </summary>
        string RefTransId { get; }

        /// <summary>
        /// 会员识别码
        /// </summary>
        string TpUserName { get; }

        /// <summary>
        /// 投注交易时间 (UTC+0)
        /// </summary>
        DateTime UTCBetTime { get; }

        /// <summary>
        /// 注单结算的时间 (UTC+0)
        /// </summary>
        DateTime UTCSettledTime { get; }

        /// <summary>
        /// 注单赔率
        /// </summary>
        string Odds { get; }

        /// <summary>
        /// 投注金额
        /// </summary>
        decimal Stake { get; }

        /// <summary>
        /// 输赢金额
        /// </summary>
        decimal WinlostAmount { get; }

        /// <summary>
        /// 联盟名称
        /// </summary>
        string LeagueName { get; }

        /// <summary>
        /// 主队名称
        /// </summary>
        string HomeTeamName { get; }

        /// <summary>
        /// 客队名称
        /// </summary>
        string AwayTeamName { get; }

        /// <summary>
        /// 主队让球
        /// </summary>
        string HomeHandicap { get; }

        /// <summary>
        /// 客队让球
        /// </summary>
        string AwayHandicap { get; }

        /// <summary>
        /// 让球
        /// </summary>
        string Handicap { get; }

        /// <summary>
        /// 注单状态
        /// </summary>
        string TicketStatus { get; }



        /// <summary>
        /// 下注时客队得分
        /// </summary>
        string AwayScore { get; }

        /// <summary>
        /// 下注时主队得分
        /// </summary>
        string HomeyScore { get; }
    }
}
