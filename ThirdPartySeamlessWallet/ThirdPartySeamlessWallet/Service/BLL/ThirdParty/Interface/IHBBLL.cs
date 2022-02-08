using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IHBBLL
    {
        HBPlayerDetailResponse Auth(HBPlayerDetailRequest request);
    }
}