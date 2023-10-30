using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

public interface IIMBLL
{
    int Bet_IM(List<BetTransactions_IM> betList, string tpUserName, Tp tpId, GameType gameType, out List<BetResultIM> resp);

    int Settle_IM(SettleTransactions_IM data, string tpUserName, Tp tpId, GameType gameType, out object resp);

    int Refund_SPRIBE(RefundTransaction_Spribe data, string tpUserName, Tp tpId, GameType gameType, out object resp);

    int SetSettleFlag_BettransRng_IM(string spName, string transId, DateTime settleTime);

    int SetSettleFlag_Bettrans_IM(string spName, string transId);

    int SetSettleFlag_TpWager_IM(string spName, string transId);

    TransType GetTransType(int actionId);
}
