using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IMBSBLL
    {
        MBSTransferInOutResponse Bet(MBSTransferInOutRequest requestData);
        MBSTransferInOutResponse Settle(MBSTransferInOutRequest requestData);
    }
}