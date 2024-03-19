using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty;

public class WonCasinoBLL : IWonCasinoBLL
{ 
    public WonCasinoGetBalanceResponse GetBalance(WonCasinoBaseRequest request)
    {
        return new WonCasinoGetBalanceResponse()
        {
            result = 0,
            desc = "OK",
            balance = 100000
        };
    }

    public WonCasinoDeductResponse Deduct(WonCasinoDeduct request)
    {
        return new WonCasinoDeductResponse()
        {
            result = 0,
            desc = "OK",
            serialnumber = GenerateRandomString(1),
            balance = 100000 - Math.Abs(request.bets.FirstOrDefault().betamount),
            orderno = GenerateRandomString(0)
        };
    }

    public WonCasinoDepositResponse Deposit(WonCasinoDeposit request)
    {
        return new WonCasinoDepositResponse()
        {
            result = 0,
            desc = "OK",
            balance = 100000 - Math.Abs(request.bets.FirstOrDefault().betamount),
            orderno = GenerateRandomString(0)
        };
    }

    public WonCasinoRollBackResponse RollBack(WonCasinoRollBack request)
    {
        return new WonCasinoRollBackResponse()
        {
            result = 0,
            desc = "OK",
            balance = 100000 - Math.Abs(request.amount),
        };
    }
    
    
    #region private

    private bool VerifySign(string sign)
    {
        return true;
    }
    private static string GenerateRandomString(int mode)
    {
        var random = new Random();
        var numericChars = "0123456789";
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var stringChars = new char[8];
        var sourceChars = (mode == 0) ? numericChars : chars;

        for (var i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = sourceChars[random.Next(sourceChars.Length)];
        }
        
        return new string(stringChars);
    }
    
    #endregion

    
}