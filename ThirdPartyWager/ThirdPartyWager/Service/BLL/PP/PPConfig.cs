using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL.PP
{
    public class PPConfig : MaxGameConfig
    {
        public string LiveCasinoGameType { get; set; }

        public string LiveCasinoApiUrl { get; set; }
    }
}
