using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IThirdPartyBLL
    {
       Tp ThirdParty { get; set; }
       GameType GameType { get; set; }
    }
}
