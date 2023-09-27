using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

public interface IPegasusBLL
{
    int Bet_Pegasus(BettingRequest_Pegasus bet, string tpUserName, Tp tpId, GameType gameType, out ResultPegasus resp);

    int Settle_Pegasus(SettleRequest_Pegasus settle, string tpUserName, Tp tpId, GameType gameType,
        out ResultPegasus resp);
    
    int Cancel_Pegasus(CancelRequest_Pegasus settle, string tpUserName, Tp tpId, GameType gameType,
        out ResultPegasus resp);
    
    int Bonus_Pegasus(BonusRequest_Pegasus settle, string tpUserName, Tp tpId, GameType gameType,
        out BonusResultPegasus resp);
    
    int SetSettleFlag_TpWager_Pegasus(string spName, string transId);
}
