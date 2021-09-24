using System;

namespace ThirdPartyCore.Model.TpBetRecordDetail.Interfaces
{
    public interface IVirtualSportsDetail
    {
        /// <summary>
        /// 關聯單號
        /// </summary>
        string RefTransId { get; }

        /// <summary>
        /// 注单识别码
        /// </summary>
        string TransId { get; }

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
        /// 投注金额
        /// </summary>
        decimal Stake { get; }

        /// <summary>
        /// 输赢金额
        /// </summary>
        decimal WinlostAmount { get; }
    }
}
