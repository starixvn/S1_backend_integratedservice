using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IBNGBLL
    {
        int Transaction(TpCustInfo tpCustInfo, BNGRequest bettingData, out decimal afterAmount);
    }
}
