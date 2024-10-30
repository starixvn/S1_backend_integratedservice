using ThirdPartyCore.Model;

namespace ThirdParty.FaChai;

internal class FaChaiConfig : TpSiteConfig
{
    public string OperatorPwd { get; set; }
    public string OperatorKey { get; set; }
    public string GameType { get; set; }
}