using Newtonsoft.Json;
using System;
using System.Linq;
using ThirdPartyCore.Model.TpBetRecordDetail.Interfaces;

namespace ThirdPartyCore.Model.TpBetRecordDetail.ThirdPartyDataModel.Saba
{

    public class SabaCasinoDetail : BaseDetail, ICasinoDetail
    {
        private readonly BetDetail _betDetail;
        private readonly TpCurrency _tpCurrency;
        private const int TimeZone = -4;

        public SabaCasinoDetail(GetDetailObject detailObject) : base(detailObject)
        {
            _betDetail = JsonConvert.DeserializeObject<SabaGetBetDetailResult>(detailObject.TpBetRecordDetail.Detail)?.BetDetails?.First();
            _tpCurrency = detailObject.TpCurrency;          
        }
        public string RefTransId => _betDetail?.trans_id;

        public string TpUserName => _betDetail?.vendor_member_id;

        public DateTime UTCBetTime => new DateTimeOffset(_betDetail.transaction_time, new TimeSpan(TimeZone, 0, 0)).DateTime;

        public DateTime UTCSettledTime => new DateTimeOffset(_betDetail.settlement_time, new TimeSpan(TimeZone, 0, 0)).DateTime;

        public decimal Stake => _betDetail.stake * TpCurrency.ExchangeRate;

        public decimal WinlostAmount => _betDetail.winlost_amount * TpCurrency.ExchangeRate;
    }
}