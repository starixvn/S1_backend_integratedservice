using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface ITFGBLL
    {
        Task<TFGTransferResp> Transfer(TFGTransferReq req);

        Task<TFGRallBackResp> Rollback(TFGRallBackReq req);
    }
}
