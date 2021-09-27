using Newtonsoft.Json;
using System;
using System.Linq;
using ThirdPartyCore.Model.TpBetRecordDetail.Interfaces;

namespace ThirdPartyCore.Model.TpBetRecordDetail.ThirdPartyDataModel.Saba
{

    public class SabaSportsDetail : BaseDetail, ISportsDetail
    {
        private readonly BetDetail _betDetail;
        private readonly TpCurrency _tpCurrency;
        private const int TimeZone = -4;

        public SabaSportsDetail(GetDetailObject detailObject) : base(detailObject)
        {
            _betDetail = JsonConvert.DeserializeObject<SabaGetBetDetailResult>(detailObject.TpBetRecordDetail.Detail)?.BetDetails?.First();
            _tpCurrency = detailObject.TpCurrency;       
        }

        public string TpUserName => _betDetail?.vendor_member_id;

        public DateTime UTCBetTime =>  new DateTimeOffset(_betDetail?.transaction_time ?? DateTime.MinValue, new TimeSpan(TimeZone, 0, 0)).DateTime;

        public DateTime UTCSettledTime => new DateTimeOffset(_betDetail?.settlement_time ?? DateTime.MinValue, new TimeSpan(TimeZone, 0, 0)).DateTime;

        public string Odds => _betDetail?.odds;

        public decimal Stake => _betDetail.stake * _tpCurrency.ExchangeRate;

        public decimal WinlostAmount => _betDetail.winlost_amount * _tpCurrency.ExchangeRate;

        public string LeagueName => _betDetail?.leaguename?.First()?.name;

        public string HomeTeamName => _betDetail?.hometeamname?.First()?.name;

        public string AwayTeamName => _betDetail?.awayteamname?.First()?.name;

        public string HomeHandicap => _betDetail?.home_hdp;

        public string AwayHandicap => _betDetail?.away_hdp;

        public string Handicap => _betDetail?.hdp;

        public string TicketStatus => _betDetail?.ticket_status;    

        public string AwayScore => _betDetail?.away_score;

        public string HomeyScore => _betDetail?.home_score;

        public string RefTransId => _betDetail?.parlay_ref_no;
    }
}