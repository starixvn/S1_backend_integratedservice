using ThirdPartyCore.Model;

namespace ThirdParty.PP
{
    public class PPConfig : TpSiteConfig
    {
        public string OperatorPwd { get; set; }
        public string OperatorKey { get; set; }
        public string GameType { get; set; }
    }
}