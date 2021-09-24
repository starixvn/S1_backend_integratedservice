using Newtonsoft.Json;
using System;
using System.Linq;
using ThirdPartyCore.Model.TpBetRecordDetail.Interfaces;

namespace ThirdPartyCore.Model.TpBetRecordDetail.ThirdPartyDataModel.Saba
{

    public class SabaVirtualSportsDetail : BaseDetail, IVirtualSportsDetail
    {
        private readonly BetVirtualSportDetail _betDetail;
        private readonly TpCurrency _tpCurrency;
        private const int TimeZone = -4;

        public SabaVirtualSportsDetail(GetDetailObject detailObject) : base(detailObject)
        {
            _betDetail = JsonConvert.DeserializeObject<SabaGetBetDetailResult>(detailObject.TpBetRecordDetail.Detail)?.BetVirtualSportDetails?.First();
            _tpCurrency = detailObject.TpCurrency;          
        }
        public string RefTransId => _betDetail?.trans_id;

        public string TpUserName => _betDetail?.vendor_member_id;

        public DateTime UTCBetTime => new DateTimeOffset(_betDetail.transaction_time, new TimeSpan(TimeZone, 0, 0)).DateTime;

        public DateTime UTCSettledTime => new DateTimeOffset(_betDetail.match_datetime, new TimeSpan(TimeZone, 0, 0)).DateTime;

        public decimal Stake => _betDetail.stake * _tpCurrency.ExchangeRate;

        public decimal WinlostAmount => _betDetail.winlost_amount * _tpCurrency.ExchangeRate;
    }
}