using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model;

public class TpRoundIdGen
{
    public int SQ { get; set; }

    public Tp TpId { get; set; }

    public GameType GameType { get; set; }

    public string TpRoundId { get; set; }

}
