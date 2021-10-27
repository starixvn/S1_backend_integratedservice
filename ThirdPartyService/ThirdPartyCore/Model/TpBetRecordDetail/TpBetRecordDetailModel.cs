using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ThirdPartyCore.Model
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
                return _detail.IndexOf("gzip64") > -1 ? GZipDecompress() : _detail;
            }
            set
            {
                _detail = value;
            }
        }

        private string _detail;

        private string GZipDecompress()
        {
            var gzip64 = JObject.Parse(_detail)["gzip64"].ToString();
            using var msi = new MemoryStream(Convert.FromBase64String(gzip64));
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
                gs.CopyTo(mso);
            }
            return Encoding.UTF8.GetString(mso.ToArray());
        }
    }
}
