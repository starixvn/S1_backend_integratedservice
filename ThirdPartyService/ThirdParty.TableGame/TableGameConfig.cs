using ThirdPartyCore.Model;

namespace ThirdParty.TableGame;

internal class TableGameConfig : TpSiteConfig
{
    public string OperatorPwd { get; set; }
    public string OperatorKey { get; set; }
    public string GameType { get; set; }
}