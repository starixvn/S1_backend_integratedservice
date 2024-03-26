using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.SBSW;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty;

public class WonCasinoBLL : IWonCasinoBLL
{ 
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly TpCustInfoBLL _tpCustInfoBLL;
    private readonly BettingService _bettingService;
    private readonly SettledService _settledService;
    private readonly PartitionService _partitionService;
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
        _partitionService = provider.GetRequiredService<PartitionService>();
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
        //OrderNo 由接入方自行生產 隨機8位數字
        var orderNo = GenerateRandomString(0);
        var getErrorResponse = new WonCasinoDeductResponse()
        {
            result = (int)WonCasinoErrorCodeEnum.SystemError,
            desc = WonCasinoErrorCodeEnum.SystemError.GetEnumDescription(),
            balance = 0.00m,
            orderno = orderNo
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

            switch (request.type)
            {
                case (int)WonCasinoTypeEnum.Bet:
                    request.orderno = orderNo;
                    request.TicketType = (int)BettransType.Bet;
                    request.TicketStatus = (int)TicketStatus.UnSettled;
                    break;
                case (int)WonCasinoTypeEnum.Cancel:
                    request.orderno = orderNo;
                    request.TicketType = (int)BettransType.CancelBet;
                    request.TicketStatus = (int)TicketStatus.Refund;
                    break;
                
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
                    orderno = orderNo
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
        var orderNo = GenerateRandomString(0);
        var getErrorResponse = new WonCasinoDepositResponse()
        {
            result = (int)WonCasinoErrorCodeEnum.SystemError,
            desc = WonCasinoErrorCodeEnum.SystemError.GetEnumDescription(),
            balance = 0.00m,
            orderno = orderNo
        };
        try
        {
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, request.username, _gameType);

            request.orderno = orderNo;
            var result = Deposit_WonCasino(request, out var tpAfterAmount);
            if (result > 0 || result == -1)
            {
                if (tpAfterAmount == 0 || result == -1)
                { 
                    tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.username, _gameType);
                    tpAfterAmount = tpCustBalance.Balance;
                }

                return new WonCasinoDepositResponse()
                { 
                    result = 0,
                    desc = "OK",
                    balance = tpAfterAmount,
                    orderno = orderNo
                };
            }
            
            if (result == -2)
            {
                //無單號或已處理過，直接回傳成功並且回覆當前餘額
                tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.username, _gameType);
                tpAfterAmount = tpCustBalance.Balance;
                return new WonCasinoDepositResponse()
                { 
                    result = 0,
                    desc = "OK",
                    balance = tpAfterAmount,
                };
            }
        }
        catch (Exception ex)
        {
            _log2File.Error($"{request}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName, ex);
            return getErrorResponse;
        }
        
        _log2File.Error($"result={getErrorResponse}", logName);
        return getErrorResponse;
    }

    public WonCasinoRollBackResponse RollBack(WonCasinoRollBackRequest request)
    {
        //Rollback 只有在两个情况出现  超时， HTTP没响应  (投注时，派彩时 ，撤销时）
        //触发rollback 的时候 依据 deposit / deduct 来操作 。
        //WonCasino後台可觸發
        var logName = $"{_thisTp.ToString()}_RollBack";
        var getErrorResponse = new WonCasinoRollBackResponse()
        {
            result = (int)WonCasinoErrorCodeEnum.SystemError,
            desc = WonCasinoErrorCodeEnum.SystemError.GetEnumDescription(),
            balance = 0.00m,
        };
        try
        {
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, request.username, _gameType);
            
            var result = Rollback_WonCasino(request, out var tpAfterAmount);
            if (result > 0 || result == -1)
            {
                if (tpAfterAmount == 0 || result == -1)
                { 
                    tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.username, _gameType);
                    tpAfterAmount = tpCustBalance.Balance;
                }

                return new WonCasinoRollBackResponse()
                { 
                    result = 0,
                    desc = "OK",
                    balance = tpAfterAmount,
                };
            }

            if (result == -2)
            {
                //無單號或已處理過，直接回傳成功並且回覆當前餘額
                tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.username, _gameType);
                tpAfterAmount = tpCustBalance.Balance;
                return new WonCasinoRollBackResponse()
                { 
                    result = 0,
                    desc = "OK",
                    balance = tpAfterAmount,
                };
            }
        }
        catch (Exception ex)
        {
            _log2File.Error($"{request}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName, ex);
            return getErrorResponse;
        }
        
        _log2File.Error($"result={getErrorResponse}", logName);
        return getErrorResponse;
        // return new WonCasinoRollBackResponse()
        // {
        //     result = 0,
        //     desc = "OK",
        //     balance = 100000 - Math.Abs(request.amount),
        // };
    }
    
    #region Exchange

    private int Deduct_WonCasino(WonCasinoDeductRequest bettingData_WonCasino, out decimal afterAmount)
    {
        var result = 0;
        afterAmount = 0.00m;
        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, bettingData_WonCasino.username);
        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);
        var custBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, bettingData_WonCasino.username, _gameType);

        if (tpCurrency.ExchangeRate != 0)
        {
            bettingData_WonCasino.amount *= tpCurrency.ExchangeRate;
            bettingData_WonCasino.bets.betamount *= tpCurrency.ExchangeRate;
            bettingData_WonCasino.bets.commamount *= tpCurrency.ExchangeRate;
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

        result = bettingData_WonCasino.type switch
        {
            (int)WonCasinoTypeEnum.Bet => _bettingService.DeductLC_WonCasino(tpCustInfo, bettingData_WonCasino,
                out afterAmount),
            (int)WonCasinoTypeEnum.Cancel => _settledService.DeductLC_WonCasino(tpCustInfo, bettingData_WonCasino,
                out afterAmount),
            _ => result
        };

        if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
        {
            afterAmount /= tpCurrency.ExchangeRate;
        }
        return result;
    }
    
    private int Deposit_WonCasino(WonCasinoDepositRequest settleData_WonCasino, out decimal afterAmount)
    {
        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, settleData_WonCasino.username);
        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);

        if (tpCurrency.ExchangeRate != 0)
        {
            settleData_WonCasino.amount *= tpCurrency.ExchangeRate;
            settleData_WonCasino.bets.betamount *= tpCurrency.ExchangeRate;
            settleData_WonCasino.bets.commamount *= tpCurrency.ExchangeRate;
            settleData_WonCasino.bets.winlost *= tpCurrency.ExchangeRate;
            settleData_WonCasino.bets.foregift *= tpCurrency.ExchangeRate;
        }
        
        switch (settleData_WonCasino.type)
        {
            case (int)WonCasinoTypeEnum.Cancel:
                settleData_WonCasino.TicketType = (int)BettransType.CancelBet;
                settleData_WonCasino.TicketStatus = (int)TicketStatus.Refund;
                break;
            case (int)WonCasinoTypeEnum.Settle:
            case (int)WonCasinoTypeEnum.ReSettle:
                if (settleData_WonCasino.amount > 0)
                {
                    settleData_WonCasino.TicketStatus = (int)TicketStatus.Win;
                }
                else
                {
                    settleData_WonCasino.TicketStatus = (int)TicketStatus.Lost;
                }
                settleData_WonCasino.TicketType = (int)BettransType.Payout;
                break;
        }
        
        var result = _settledService.Deposit_WonCasino(tpCustInfo, settleData_WonCasino, out afterAmount);
        if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
        {
            afterAmount /= tpCurrency.ExchangeRate;
        }
        return result;
    }
    
    private int Rollback_WonCasino(WonCasinoRollBackRequest rollbackData_WonCasino, out decimal afterAmount)
    {
        var GetSpName = "Service_Bettrans_WonCasino_Get";
        var partitionTime = DateTime.Now;
        var rollbackRequest = new RollbackWonCasino();
        var result = 0;
        
        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, rollbackData_WonCasino.username);
        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);
        
        var bettrans = _partitionService.Query<BettransWonCasino>(partitionTime, GetSpName, 
            new { id = rollbackData_WonCasino.serialnumber , username = tpCustInfo.TpUserName });
        
        //Rollback要驗notifyid+type+serialnumber以免重複處理。
        
        
        if (bettrans.All(i => i.id != long.Parse(rollbackData_WonCasino.serialnumber)))
        {
            afterAmount = 0.00m;
            return -2;
        }

        if (!VerifyNotifyId(rollbackData_WonCasino.notifyid,rollbackData_WonCasino.type.ToString(),rollbackData_WonCasino.serialnumber, bettrans))
        {
            afterAmount = 0.00m;
            return -2;
        }
        

        switch (rollbackData_WonCasino.type)
        {
            
            case (int)WonCasinoTypeEnum.Bet:
                result = Rollback_Deduct_Check(bettrans);
                if (result != 0)
                {
                    afterAmount = 0.00m;
                    return result;
                }
                rollbackRequest = RollbackWonCasinoRequest(bettrans, rollbackData_WonCasino,
                    (int)BettransType.CancelBet, (int)TicketStatus.Refund, (int)SettleFlag.EndGame);
                break;
            case(int)WonCasinoTypeEnum.Cancel:
                result = Rollback_DepositOrCancel_Check(bettrans);
                if (result != 0)
                {
                    afterAmount = 0.00m;
                    return result;
                }
                rollbackRequest = RollbackWonCasinoRequest(bettrans, rollbackData_WonCasino,
                    (int)BettransType.Payout, (int)TicketStatus.Refund, (int)SettleFlag.EndGame);
                break;
            case (int)WonCasinoTypeEnum.Settle:
                result = Rollback_DepositOrCancel_Check(bettrans);
                if (result != 0)
                {
                    afterAmount = 0.00m;
                    return result;
                }
                //Settle Rollback 要恢復投注的狀態 通知WonCasino手動再次派彩
                rollbackRequest = RollbackWonCasinoRequest(bettrans, rollbackData_WonCasino,
                    (int)BettransType.CancelBet, (int)TicketStatus.UnSettled, (int)SettleFlag.Running);
                break;
        }
        
        if (tpCurrency.ExchangeRate != 0)
        {
            rollbackRequest.amount *= tpCurrency.ExchangeRate;
        }
        
        result = _settledService.Rollback_WonCasino(tpCustInfo,rollbackRequest, out afterAmount);
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

    private bool VerifyNotifyId(string notifyId, string type,string serialNumber,IEnumerable<BettransWonCasino> bettransWonCasino)
    {
        var requestNotifyId = notifyId + type + serialNumber;
        return bettransWonCasino.All(i => i.notifyid != requestNotifyId);
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

    private int Rollback_Deduct_Check(IEnumerable<BettransWonCasino> bettransData_WonCasino)
    {
        //当收到Rollback信息中 type=1的时候，将对 Deduct进行回滚操作
        //Rollback且Type = 1是針對下注狀態，若Count超過1個就直接回傳狀態
        if (bettransData_WonCasino.Count() > 1)
        {
            return -2;
        }
        
        if (bettransData_WonCasino.All(i => i.TicketStatus == (int)TicketStatus.UnSettled 
                                            && i.TicketType == (int)BettransType.Bet
                                            && i.SettleFlag == (int)SettleFlag.Running ))
        {
            return 0;
        }
        
        return - 2;
    }
    
    private int Rollback_DepositOrCancel_Check(IEnumerable<BettransWonCasino> bettransData_WonCasino)
    {
        //当收到Rollback信息中 type=9的时候，将对 Deposit 进行回滚操作
        //Rollback Type = 7,直接算為取消結算結果，直接退還錢並直接結單，不再進行操作。
        //取最後結算狀態
        var settleBettrans =
            bettransData_WonCasino.LastOrDefault(i => i.TicketType == (int)BettransType.Payout);

        switch (settleBettrans.TicketStatus)
        {
            case (int)TicketStatus.Win:
            case (int)TicketStatus.Lost:
                return 0;
            default:
                return - 1;
        }
    }

    private RollbackWonCasino RollbackWonCasinoRequest(IEnumerable<BettransWonCasino> bettransData_WonCasino,
        WonCasinoRollBackRequest rollbackData_WonCasino,int ticketType,int ticketStatus,int settleFalg)
    {
        var result = new RollbackWonCasino();
        //拿bettrans的資料組request，拿最後狀態
        var bettransData = bettransData_WonCasino.LastOrDefault();
        result = new RollbackWonCasino
        {
            appid = rollbackData_WonCasino.appid,
            sign = rollbackData_WonCasino.sign,
            notifyid = rollbackData_WonCasino.notifyid,
            id = bettransData.id,
            amount = rollbackData_WonCasino.amount,
            type = rollbackData_WonCasino.type,
            currency =  bettransData.currency,
            orderno = bettransData.orderno,
            gameid = bettransData.gameid,
            userid = bettransData.userid,
            userstatus = bettransData.userstatus,
            betpoint = bettransData.betpoint,
            betodds = bettransData.betodds,
            betamount = bettransData.betamount,
            commamount = bettransData.commamount,
            winlost = bettransData.winlost,
            foregift = bettransData.foregift,
            gameroundid = bettransData.gameroundid,
            gameresult = bettransData.gameresult,
            gametype = bettransData.gametype,
            gamestyle = bettransData.gamestyle,
            serialnumber = bettransData.serialnumber,
            gameroundno = bettransData.gameroundno,
            gameno = bettransData.gameno,
            bettype = bettransData.bettype,
            cid = bettransData.cid,
            state = bettransData.state,
            createtime = bettransData.createtime,
            settletime =  new DateTimeOffset(DateTime.UtcNow.AddHours(8)).ToUnixTimeSeconds(),
            UTCBetTime = DateTimeOffset.FromUnixTimeSeconds(bettransData.createtime).UtcDateTime,
            UTCSettleTime = DateTimeOffset.FromUnixTimeSeconds(bettransData.settletime).UtcDateTime,
            TicketType = ticketType,
            TicketStatus = ticketStatus,
            SettleFlag = settleFalg
        };

        return result;
    }
    
    #endregion

    
}