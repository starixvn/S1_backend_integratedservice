using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IGABLL
    {
        int Bet(TpCustInfo tpCustInfo, GATransferReq reqData);
    }
}
