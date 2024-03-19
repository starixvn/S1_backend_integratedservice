using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

public interface IWonCasinoBLL
{
    WonCasinoGetBalanceResponse GetBalance(WonCasinoBaseRequest request);
    WonCasinoDeductResponse Deduct(WonCasinoDeduct request);
    WonCasinoDepositResponse Deposit(WonCasinoDeposit request);
    WonCasinoRollBackResponse RollBack(WonCasinoRollBack request);
}