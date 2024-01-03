using ThirdPartyCore.Model;

namespace ThirdParty.DG
{
    internal class DGConfig : TpSiteConfig
    {
        public string APIkey { get; set; }

        public string WalletUrl { get; set; }

        public string AgentAccount { get; set; }
    }
}
