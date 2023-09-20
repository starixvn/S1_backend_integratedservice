using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IFaChaiBLL
    {
        int Bet(TpCustInfo tpCustInfo, BettingDataRNG_FaChai bettingData, out decimal afterAmount);
        FCResponse Cancel(TpCustInfo tpCustInfo, CnacelData_FaChai cancelData);
    }
}
