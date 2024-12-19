
using ThirdPartyCore.Model;

namespace ThirdParty.SBSW;

public class SabaConfig : TpSiteConfig
{
    public string VendorId { get; set; }
    public string OperatorId { get; set; }
    public int WebSkinType { get; set; } = -1;
    public int Skin { get; set; } = -1;
}
