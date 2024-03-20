using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty;

public class WonCasinoBLL : IWonCasinoBLL
{ 
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly TpCustInfoBLL _tpCustInfoBLL;
    private readonly BettingService _bettingService;
    private readonly SettledService _settledService;
    private readonly Log2File _log2File;
    private readonly Tp _thisTp;
    private readonly int _gameType = 0;

    public WonCasinoBLL(IServiceProvider provider)
    {
        _thisTp = Tp.WonCasino;
        _gameType = (int)GameType.WonCasino_LiveCasino;
        _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _bettingService = provider.GetRequiredService<BettingService>();
        _settledService = provider.GetRequiredService<SettledService>();
        _log2File = provider.GetRequiredService<Log2File>();
    }
    
    public WonCasinoGetBalanceResponse GetBalance(WonCasinoBaseRequest request)
    {
        var getErrorResponse = new WonCasinoGetBalanceResponse()
        {
            result = (int)WonCasinoErrorCodeEnum.SystemError,
            desc = WonCasinoErrorCodeEnum.SystemError.GetEnumDescription(),
            balance = 0.00m
        };

        if (!VerifySign(request.sign))
        {
            getErrorResponse.result = (int)WonCasinoErrorCodeEnum.SignError;
            getErrorResponse.desc = WonCasinoErrorCodeEnum.SignError.GetEnumDescription();
            return getErrorResponse;
        }
        
        // if (string.IsNullOrEmpty(request.token))
        // {
        //     getErrorResponse.result = (int)WonCasinoErrorCodeEnum.PlayerInvalid;
        //     getErrorResponse.desc = WonCasinoErrorCodeEnum.PlayerInvalid.GetEnumDescription();
        //     return getErrorResponse;
        // }
        
        var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, request.username);
            
        if (tpCheck == null)
        {
            getErrorResponse.result = (int)WonCasinoErrorCodeEnum.PlayerInvalid;
            getErrorResponse.desc = WonCasinoErrorCodeEnum.PlayerInvalid.GetEnumDescription();
            return getErrorResponse;
        }
            
        if (tpCheck.Locked)
        {
            getErrorResponse.result = (int)WonCasinoErrorCodeEnum.PlayerInvalid;
            getErrorResponse.desc = WonCasinoErrorCodeEnum.PlayerInvalid.GetEnumDescription();
            return getErrorResponse;
        }
            
        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, request.username, _gameType);
        
        return new WonCasinoGetBalanceResponse()
        {
            result = 0,
            desc = "OK",
            balance = tpCustBalance.Balance
        };
    }

    public WonCasinoDeductResponse Deduct(WonCasinoDeductRequest request)
    {
        var logName = $"{_thisTp.ToString()}_Deduct";
        var getErrorResponse = new WonCasinoDeductResponse()
        {
            result = (int)WonCasinoErrorCodeEnum.SystemError,
            desc = WonCasinoErrorCodeEnum.SystemError.GetEnumDescription(),
            balance = 0.00m,
            orderno = GenerateRandomString(0)
        };
        try
        {
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.WonCasino, request.username);

            if (tpCheck == null)
            {
                getErrorResponse.result = (int)WonCasinoErrorCodeEnum.PlayerInvalid;
                getErrorResponse.desc = WonCasinoErrorCodeEnum.PlayerInvalid.GetEnumDescription();
                return getErrorResponse;
            }

            if (tpCheck.Locked)
            {
                getErrorResponse.result = (int)WonCasinoErrorCodeEnum.PlayerFrozen;
                getErrorResponse.desc = WonCasinoErrorCodeEnum.PlayerFrozen.GetEnumDescription();
                return getErrorResponse;
            }

            var result = Deduct_WonCasino(request, out decimal tpAffterAmount);

            if (result > 0 || result == -1)
            {
                if (tpAffterAmount == 0 || result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.username, _gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }

                return new WonCasinoDeductResponse
                {
                    result = (int)WonCasinoErrorCodeEnum.Success,
                    desc = WonCasinoErrorCodeEnum.Success.GetEnumDescription(),
                    balance = tpAffterAmount,
                    orderno = GenerateRandomString(0)
                };
            }

            if (tpAffterAmount < 0)
            {
                getErrorResponse.result = (int)WonCasinoErrorCodeEnum.InsufficientBalance;
                getErrorResponse.desc = WonCasinoErrorCodeEnum.InsufficientBalance.GetEnumDescription();
                _log2File.Error($"{request}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                return getErrorResponse;
            }
        }
        catch (Exception ex)
        {
            getErrorResponse.result = (int)WonCasinoErrorCodeEnum.SystemError;
            getErrorResponse.desc = WonCasinoErrorCodeEnum.SystemError.GetEnumDescription();
            _log2File.Error($"{request}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName, ex);
            return getErrorResponse;
        }

        _log2File.Error($"result={getErrorResponse}", logName);
        return getErrorResponse;
    }

    public WonCasinoDepositResponse Deposit(WonCasinoDepositRequest request)
    {
        var logName = $"{_thisTp.ToString()}_Deposit";
        var getErrorResponse = new WonCasinoDepositResponse()
        {
            result = (int)WonCasinoErrorCodeEnum.SystemError,
            desc = WonCasinoErrorCodeEnum.SystemError.GetEnumDescription(),
            balance = 0.00m,
            orderno = GenerateRandomString(0)
        };
        
        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, request.username, _gameType);
        
        return new WonCasinoDepositResponse()
        {
            result = 0,
            desc = "OK",
            balance = tpCustBalance.Balance - Math.Abs(request.bets.betamount),
            orderno = GenerateRandomString(0)
        };
    }

    public WonCasinoRollBackResponse RollBack(WonCasinoRollBackRequest request)
    {
        return new WonCasinoRollBackResponse()
        {
            result = 0,
            desc = "OK",
            balance = 100000 - Math.Abs(request.amount),
        };
    }
    
    #region Exchange

    private int Deduct_WonCasino(WonCasinoDeductRequest bettingData_WonCasino, out decimal afterAmount)
    {
        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, bettingData_WonCasino.username);
        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);
        var custBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, bettingData_WonCasino.username, _gameType);

        if (tpCurrency.ExchangeRate != 0)
        {
            bettingData_WonCasino.amount *= tpCurrency.ExchangeRate;
            custBalance.Balance *= tpCurrency.ExchangeRate;
        }

        if (custBalance.Balance < 0)
        {
            afterAmount = custBalance.Balance / tpCurrency.ExchangeRate;
            return 0;
        }

        var checkBalance = custBalance.Balance - Math.Abs(bettingData_WonCasino.amount);
        if (checkBalance < 0)
        {
            afterAmount = checkBalance / tpCurrency.ExchangeRate;
            return 0;
        }
        
        //var result = _bettingService.DeductLC_WonCasino(tpCustInfo, bettingData_WonCasino, out afterAmount);
        var result = 1;
        afterAmount = checkBalance;
        if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
        {
            afterAmount /= tpCurrency.ExchangeRate;
        }
        return result;
    }
        
        
    #endregion
    
    
    
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