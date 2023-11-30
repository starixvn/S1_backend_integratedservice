using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IBNGBLL
    {
        int Transaction(TpCustInfo tpCustInfo, BNGRequest reqData, out decimal afterAmount);

        int Rollback(TpCustInfo tpCustInfo, BNGRequest reqData, out decimal afterAmount);
    }
}
