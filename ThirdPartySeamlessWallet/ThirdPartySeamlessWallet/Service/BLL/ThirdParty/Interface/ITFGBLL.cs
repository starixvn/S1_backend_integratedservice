using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface ITFGBLL
    {
        Task<Result> Transfer(TFGTransferReq req);

        Task<Result> Rollback(TFGRallBackReq req);

        Check_TFG GetBetData(string id);

        IEnumerable<TFGSettle> GetSettleData(string id);

        WagerTFG GetWagerData(string id);
    }
}
