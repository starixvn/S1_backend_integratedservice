using Newtonsoft.Json;
using System;
using System.Linq;
using ThirdPartyCore.Model.TpBetRecordDetail.Interfaces;

namespace ThirdPartyCore.Model.TpBetRecordDetail.ThirdPartyDataModel.Saba
{

    public class SabaLotteryDetail : BaseDetail, ILotteryDetail
    {
        private readonly BetDetail _betDetail;
        private readonly BetNumberDetail _betNumberDetail;
        private readonly TpCurrency _tpCurrency;
        private const int TimeZone = -4;

        public SabaLotteryDetail(GetDetailObject detailObject) : base(detailObject)
        {
            var getBetDetailResult = JsonConvert.DeserializeObject<SabaGetBetDetailResult>(detailObject.TpBetRecordDetail.Detail);
            _betDetail = getBetDetailResult?.BetDetails?.First();
            _betNumberDetail = getBetDetailResult?.BetNumberDetails?.First();
            _tpCurrency = detailObject.TpCurrency;
        }

        public string RefTransId => GameType == 2 ? _betNumberDetail?.trans_id : _betDetail?.trans_id;

        public string TpUserName => GameType == 2 ? _betNumberDetail?.vendor_member_id : _betDetail?.vendor_member_id;

        public DateTime UTCBetTime => GameType == 2 ?
            new DateTimeOffset(_betNumberDetail?.transaction_time ?? DateTime.MinValue, new TimeSpan(TimeZone, 0, 0)).DateTime :
            new DateTimeOffset(_betDetail?.transaction_time ?? DateTime.MinValue, new TimeSpan(TimeZone, 0, 0)).DateTime;

        public DateTime UTCSettledTime => GameType == 2 ?
            new DateTimeOffset(_betNumberDetail?.transaction_time ?? DateTime.MinValue, new TimeSpan(TimeZone, 0, 0)).DateTime :
            new DateTimeOffset((DateTime)(_betDetail?.settlement_time ?? _betDetail?.transaction_time), new TimeSpan(TimeZone, 0, 0)).DateTime;

        public decimal Stake => GameType == 2 ? _betNumberDetail.stake * _tpCurrency.ExchangeRate : _betDetail.stake * _tpCurrency.ExchangeRate;

        public decimal WinlostAmount => GameType == 2 ? _betNumberDetail.winlost_amount * _tpCurrency.ExchangeRate : _betDetail.winlost_amount * _tpCurrency.ExchangeRate;
    }
}