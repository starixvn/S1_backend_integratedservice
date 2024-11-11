using ThirdPartyCore.Model;

namespace ThirdParty.RNGKeno;

internal class KenoConfig : TpSiteConfig
{
    public string OperatorPwd { get; set; }
    public string OperatorKey { get; set; }
    public string GameType { get; set; }
}