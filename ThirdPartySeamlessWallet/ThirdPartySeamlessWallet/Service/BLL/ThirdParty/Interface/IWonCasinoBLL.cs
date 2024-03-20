using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

public interface IWonCasinoBLL
{
    WonCasinoGetBalanceResponse GetBalance(WonCasinoBaseRequest request);
    WonCasinoDeductResponse Deduct(WonCasinoDeductRequest request);
    WonCasinoDepositResponse Deposit(WonCasinoDepositRequest request);
    WonCasinoRollBackResponse RollBack(WonCasinoRollBackRequest request);
}