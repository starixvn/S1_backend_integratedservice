using Newtonsoft.Json;
using System;
using ThirdPartyCore.Model.TpBetRecordDetail.Interfaces;

namespace ThirdPartyCore.Model.TpBetRecordDetail.ThirdPartyDataModel.PG
{
    public class PGCasinoDetail : BaseDetail, ICasinoDetail
    {
        private readonly PGDetailResult _betDetail;
        private readonly TpCurrency _tpCurrency;

        public PGCasinoDetail(GetDetailObject detailObject) : base(detailObject)
        {
            _betDetail = JsonConvert.DeserializeObject<PGDetailResult>(detailObject.TpBetRecordDetail.Detail);
            _tpCurrency = detailObject.TpCurrency;    
        }

        public string RefTransId => _betDetail?.parentBetId.ToString();

        public string TpUserName => _betDetail?.playerName;

        public DateTime UTCBetTime => DateTimeOffset.FromUnixTimeMilliseconds(_betDetail?.betTime ?? 0).DateTime;

        public DateTime UTCSettledTime => DateTimeOffset.FromUnixTimeMilliseconds(_betDetail?.betEndTime ?? 0).DateTime;

        public decimal Stake => _betDetail.betAmount * _tpCurrency.ExchangeRate;

        public decimal WinlostAmount => (_betDetail.winAmount - _betDetail.betAmount) * _tpCurrency.ExchangeRate;      
    }  
}