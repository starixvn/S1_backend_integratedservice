using System.IO.Compression;
using System.Text;

namespace ThirdPartyCore.Model.TpBetRecordDetail
{
    public class TpBetRecordDetailModel
    {
        public int TpId { get; set; }

        public int GameType { get; set; }

        public string TransId { get; set; }

        public int CurrencyId { get; set; }

        public string Detail
        {
            get
            {
                return BinaryDetail?.Length > 0 ? GZipDecompress() : _detail;
            }
            set
            {
                _detail = value;
            }
        }

        public byte[] BinaryDetail { get; set; }

        private string _detail;

        private string GZipDecompress()
        {
            using var msi = new MemoryStream(BinaryDetail);
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
                gs.CopyTo(mso);
            }
            return Encoding.UTF8.GetString(mso.ToArray());
        }
    }
}
