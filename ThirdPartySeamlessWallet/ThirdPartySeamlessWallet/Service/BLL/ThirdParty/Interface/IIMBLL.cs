using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

public interface IIMBLL
{
    int Bet_IM(List<BetTransactions_IM> betList ,string tpUserName, GameType gameType , out List<BetResultIM> resp);

    int Settle_IM(SettleTransactions_IM data, string tpUserName, GameType gameType, out object resp);
}
