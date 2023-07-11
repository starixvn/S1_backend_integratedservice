using Newtonsoft.Json;
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

        public TpBetRecordDetail(int tpId, GameType gameType, int currencyId, string transId, DateTime settledTime)
        {
            TpId = tpId;
            GameType = (int)gameType;
            TransId = transId;
            CurrencyId = currencyId;
            //體育結算日期會超過下個月改用現在日期
            SettledTime = settledTime > DateTime.UtcNow.AddMonths(1) ? DateTime.UtcNow : settledTime;
        }
    }
}
