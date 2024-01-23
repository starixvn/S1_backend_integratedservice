using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IGABLL
    {
        int Bet(TpCustInfo tpCustInfo, GATransferReq reqData);
        int Payout(TpCustInfo tpCustInfo, GATransferReq reqData);
        int RollBack(TpCustInfo tpCustInfo, GATransferReq reqData);
        int Unsettle(TpCustInfo tpCustInfo, GATransferReq reqData);
    }
}
