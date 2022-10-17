using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.Model
{
    public class TpBetRecordDetail
    {
        public int TpId { get; set; }

        public int GameType { get; set; }

        public int CurrencyId { get; set; }

        public string TransId { get; set; }

        public DateTime SettledTime { get; set; }

        public byte[] BinaryDetail
        {
            get
            {
                return GZipCompress();
            }
        }

        public object Detail
        {
            get
            {
                return JsonConvert.SerializeObject(_detail);
            }
            set
            {
                _detail = value;
            }
        }

        private object _detail;

        private byte[] GZipCompress()
        {
            var data = JsonConvert.SerializeObject(_detail);
            using var msi = new MemoryStream(Encoding.UTF8.GetBytes(data));
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(mso, CompressionMode.Compress))
            {
                msi.CopyTo(gs);
            }
            return mso.ToArray();
        }

        public TpBetRecordDetail(int tpId, GameType gameType, int currencyId, Vs3rdbetdetails vsDetails)
        {
            TpId = tpId;
            GameType = (int)gameType;
            TransId = vsDetails.trans_id.ToString();
            CurrencyId = currencyId;
            //體育結算日期會超過下個月改用現在日期
            SettledTime = vsDetails.transaction_time > DateTime.UtcNow.AddMonths(1) ? DateTime.UtcNow : vsDetails.transaction_time;

            var detail = new GetBetDetailData
            {
                BetVirtualSportDetails = new List<BetVirtualSportDetail>()
                {
                  new BetVirtualSportDetail()
                  {
                    trans_id = vsDetails.trans_id,
                    vendor_member_id = vsDetails.vendor_member_id,
                    transaction_time = vsDetails.transaction_time,
                    stake = vsDetails.stake,
                    ticket_status = vsDetails.ticket_status,
                    winlost_amount = vsDetails.winlost_amount,
                    bet_type = vsDetails.bet_type,
                    currency = vsDetails.currency,
                    version_key =vsDetails.version_key,
                    odds = vsDetails.odds,
                    customInfo1 = vsDetails.betchoice,
                  }
                }
            };

            Detail = detail;
        }
    }
}
