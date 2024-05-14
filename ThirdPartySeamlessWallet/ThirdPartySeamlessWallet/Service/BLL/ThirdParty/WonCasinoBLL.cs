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
        
        var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, request.username);
            
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
            
        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, request.username, _gameType);
        
        return new WonCasinoGetBalanceResponse()
        {
            result = (int)WonCasinoErrorCodeEnum.Success,
            desc = WonCasinoErrorCodeEnum.Success.GetEnumDescription(),
            balance = tpCustBalance.Balance
        };
    }

    public WonCasinoDeductResponse Deduct(WonCasinoDeductRequest request)
    {
        var logName = $"{_thisTp.ToString()}_Deduct";
        var orderNo = "99999999";
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
                    request.TicketType = (int)BettransType.Bet;
                    request.TicketStatus = (int)TicketStatus.UnSettled;
                    break;
                case (int)WonCasinoTypeEnum.Cancel:
                    request.TicketType = (int)BettransType.CancelBet;
                    request.TicketStatus = (int)TicketStatus.Refund;
                    break;
                
            }
            
            var result = Deduct_WonCasino(request, out decimal tpAffterAmount,out string dbOrderNo);

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
                    orderno = dbOrderNo
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
        var orderNo = "9999999";
        var getErrorResponse = new WonCasinoDepositResponse()
        {
            result = (int)WonCasinoErrorCodeEnum.SystemError,
            desc = WonCasinoErrorCodeEnum.SystemError.GetEnumDescription(),
            balance = 0.00m,
            orderno = orderNo
        };
        
        try
        {
            var result = Deposit_WonCasino(request, out var tpAfterAmount,out var dbOrderNo);
            if (result > 0 || result == -1)
            {
                if (tpAfterAmount == 0 || result == -1)
                { 
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.username, _gameType);
                    tpAfterAmount = tpCustBalance.Balance;
                }

                return new WonCasinoDepositResponse()
                { 
                    result = (int)WonCasinoErrorCodeEnum.Success,
                    desc = WonCasinoErrorCodeEnum.Success.GetEnumDescription(),
                    balance = tpAfterAmount,
                    orderno = dbOrderNo
                };
            }
            
            if (result == -2)
            {
                //若TicketStatus已經為 Refund = 3,/*Reject, Refund, Void, Draw*/,就不給再次結算，要直接詢問WonCasino
                //已處理過，直接回傳成功並且回覆當前餘額
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.username, _gameType);
                tpAfterAmount = tpCustBalance.Balance;
                _log2File.Info($"Won Casino Deposit : {request.serialnumber} , Already Refund , Please check with WonCasino", logName);
                return new WonCasinoDepositResponse()
                { 
                    result = (int)WonCasinoErrorCodeEnum.Success,
                    desc = WonCasinoErrorCodeEnum.Success.GetEnumDescription(),
                    balance = tpAfterAmount,
                    orderno = dbOrderNo
                };
                
            }
            
            if (result == -3)
            {
                //Deduct無單號，直接回傳成功並且回覆當前餘額
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.username, _gameType);
                tpAfterAmount = tpCustBalance.Balance;
                _log2File.Info($"Won Casino Deposit: {request.serialnumber} ,Deduct Can't Found BetId", logName);
                return new WonCasinoDepositResponse()
                { 
                    result = (int)WonCasinoErrorCodeEnum.Success,
                    desc = WonCasinoErrorCodeEnum.Success.GetEnumDescription(),
                    balance = tpAfterAmount,
                    orderno = dbOrderNo
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
        //實測後為若Deposit / Deduct，請求失敗才會發Rollback，但NotfiyId及Type會是一樣的。
        //若是NotifyId及Type與前一個function相同，則代表我方已經有此次Deposit / Deduct，則後續不再動作
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
            //Rollback 無須回傳OrderNo
            var result = Rollback_WonCasino(request, out var tpAfterAmount);
            if (result > 0 || result == -1)
            {
                if (tpAfterAmount == 0 || result == -1)
                { 
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.username, _gameType);
                    tpAfterAmount = tpCustBalance.Balance;
                }

                return new WonCasinoRollBackResponse()
                { 
                    result = (int)WonCasinoErrorCodeEnum.Success,
                    desc = WonCasinoErrorCodeEnum.Success.GetEnumDescription(),
                    balance = tpAfterAmount,
                };
            }

            if (result == -2)
            {
                //無單號或已處理過，直接回傳成功並且回覆當前餘額
                _log2File.Info($"Won Casino Rollback: {request.serialnumber} ,Already Process.", logName);
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.username, _gameType);
                tpAfterAmount = tpCustBalance.Balance;
                return new WonCasinoRollBackResponse()
                { 
                    result = (int)WonCasinoErrorCodeEnum.Success,
                    desc = WonCasinoErrorCodeEnum.Success.GetEnumDescription(),
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
    
    #region Exchange

    private int Deduct_WonCasino(WonCasinoDeductRequest bettingData_WonCasino, out decimal afterAmount, out string orderNo)
    {
        var result = 0;
        afterAmount = 0.00m;
        orderNo = "9999999";
        
        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, bettingData_WonCasino.username);
        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);
        var custBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, bettingData_WonCasino.username, _gameType);

        if (tpCurrency.ExchangeRate != 0)
        {
            bettingData_WonCasino.amount *= tpCurrency.ExchangeRate;
            bettingData_WonCasino.bets.betamount *= tpCurrency.ExchangeRate;
            bettingData_WonCasino.bets.commamount *= tpCurrency.ExchangeRate;
            bettingData_WonCasino.bets.foregift *= tpCurrency.ExchangeRate;
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
                out afterAmount, out orderNo),
            (int)WonCasinoTypeEnum.Cancel => _settledService.DeductLC_WonCasino(tpCustInfo, bettingData_WonCasino,
                out afterAmount, out orderNo),
            _ => result
        };

        if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
        {
            afterAmount /= tpCurrency.ExchangeRate;
        }
        return result;
    }
    
    private int Deposit_WonCasino(WonCasinoDepositRequest settleData_WonCasino, out decimal afterAmount,out string orderNo)
    {
        orderNo = "9999999";
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
        
        var result = _settledService.Deposit_WonCasino(tpCustInfo, settleData_WonCasino, out afterAmount,out orderNo);
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
        var checkTypeResult = 0;
        
        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, rollbackData_WonCasino.username);
        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);
        
        var bettrans = _partitionService.Query<BettransWonCasino>(partitionTime, GetSpName, 
            new { id = rollbackData_WonCasino.serialnumber , username = tpCustInfo.TpUserName });

        switch (rollbackData_WonCasino.type)
        {
            case (int)WonCasinoTypeEnum.Bet:
                result = Rollback_Deduct_Check(bettrans,rollbackData_WonCasino);
                switch (result)
                {
                    case -2:
                        afterAmount = 0.00m;
                        return result;
                    case 0:
                        rollbackRequest = RollbackWonCasinoRequest(bettrans, rollbackData_WonCasino,
                            (int)BettransType.CancelBet, (int)TicketStatus.Refund, (int)SettleFlag.EndGame);
                        break;
                    case 1:
                        rollbackRequest = DeductRollbackRequest(rollbackData_WonCasino,tpCustInfo,
                            (int)BettransType.CancelBet, (int)TicketStatus.Refund, (int)SettleFlag.EndGame);
                        break;
                }
                break;
            case(int)WonCasinoTypeEnum.Cancel:
                result = Rollback_DepositOrCancel_Check(bettrans,rollbackData_WonCasino);
                if (result != 0)
                {
                    afterAmount = 0.00m;
                    return result;
                }
                rollbackRequest = RollbackWonCasinoRequest(bettrans, rollbackData_WonCasino,
                    (int)BettransType.Payout, (int)TicketStatus.Refund, (int)SettleFlag.EndGame);
                break;
            case (int)WonCasinoTypeEnum.Settle:
                result = Rollback_DepositOrCancel_Check(bettrans,rollbackData_WonCasino);
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
            //rollbackRequest.betamount *= tpCurrency.ExchangeRate;
            //rollbackRequest.commamount *= tpCurrency.ExchangeRate;
            //rollbackRequest.winlost *= tpCurrency.ExchangeRate;
            //rollbackRequest.foregift *= tpCurrency.ExchangeRate;
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

    private bool VerifyNotifyId(string notifyId, string type,string serialNumber,IEnumerable<BettransWonCasino> bettransWonCasino)
    {
        var requestNotifyId = notifyId + type + serialNumber;
        return bettransWonCasino.All(i => i.notifyid != requestNotifyId);
    }

    private int Rollback_Deduct_Check(IEnumerable<BettransWonCasino> bettransData_WonCasino,WonCasinoRollBackRequest rollbackData_WonCasino)
    {
        //当收到Rollback信息中 type=1的时候，将对 Deduct进行回滚操作
        //Rollback且Type = 1是針對下注狀態，若Count超過1個就直接回傳狀態
        if (bettransData_WonCasino.Count() > 1)
        {
            return -2;
        }

        //若bettransData_WonCasino無資料為空 回1;
        if (bettransData_WonCasino.Count() == 0) return 1;
        
        if (bettransData_WonCasino.All(i => i.TicketStatus == (int)TicketStatus.UnSettled
                                            && i.TicketType == (int)BettransType.Bet
                                            && i.SettleFlag == (int)SettleFlag.Running))
        {
            //在驗金額，若相加為0 = 退還下注金額
            return bettransData_WonCasino.LastOrDefault().amount + rollbackData_WonCasino.amount == 0 ? 0 : -2;
        }

        return -2;
    }
    
    private int Rollback_DepositOrCancel_Check(IEnumerable<BettransWonCasino> bettransData_WonCasino,WonCasinoRollBackRequest rollbackData_WonCasino)
    {
        //当收到Rollback信息中 type=9的时候，将对 Deposit 进行回滚操作
        //Rollback Type = 7,直接算為取消結算結果，直接退還錢並直接結單，不再進行操作。
        //取最後狀態，不一定是結算
        var lastBettrans = bettransData_WonCasino.LastOrDefault();

        if (lastBettrans == null)
        {
            return -2;
        }

        switch (lastBettrans.TicketType)
        {
            case (int)BettransType.Bet:
            case (int)BettransType.Payout when lastBettrans.TicketStatus == (int)TicketStatus.Win || lastBettrans.TicketStatus == (int)TicketStatus.Lost:
                return 0;
            case (int)BettransType.Payout:
                return -1;
            default:
                return -2;
        }
    }

    private RollbackWonCasino RollbackWonCasinoRequest(IEnumerable<BettransWonCasino> bettransData_WonCasino,
        WonCasinoRollBackRequest rollbackData_WonCasino,int ticketType,int ticketStatus,int settleFlag)
    {
        var result = new RollbackWonCasino();
        //拿bettrans的資料組request，拿最後狀態
        var bettransData = bettransData_WonCasino.LastOrDefault();
        result = new RollbackWonCasino
        {
            appid = rollbackData_WonCasino.appid,
            sign = rollbackData_WonCasino.sign,
            notifyid = rollbackData_WonCasino.notifyid + "rollback",
            id = bettransData.id,
            amount = rollbackData_WonCasino.amount,
            type = rollbackData_WonCasino.type,
            currency =  bettransData.currency,
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
            settletime =  bettransData.settletime ?? new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
            UTCBetTime = DateTimeOffset.FromUnixTimeSeconds(bettransData.createtime).UtcDateTime,
            UTCSettleTime = DateTimeOffset.FromUnixTimeSeconds((long)bettransData.settletime).UtcDateTime,
            TicketType = ticketType,
            TicketStatus = ticketStatus,
            SettleFlag = settleFlag
        };

        return result;
    }

    private RollbackWonCasino DeductRollbackRequest(WonCasinoRollBackRequest rollbackData_WonCasino,TpCustInfo tpCustInfo,int ticketType,int ticketStatus,int settleFlag)
    {
        //由於Deduct的Rollback沒有前資料，只能自行組一組並寫進DB，以免錯誤。
        //下注並未成功所以amount直接為0，除非下注成功在rollback才有金額。
        var result = new RollbackWonCasino();
        //拿bettrans的資料組request，拿最後狀態
        result = new RollbackWonCasino
        {
            appid = rollbackData_WonCasino.appid,
            sign = rollbackData_WonCasino.sign,
            notifyid = rollbackData_WonCasino.notifyid + rollbackData_WonCasino.type + rollbackData_WonCasino.serialnumber +"rollback",
            id = int.Parse(rollbackData_WonCasino.serialnumber),
            amount = 0.00m,
            type = rollbackData_WonCasino.type,
            currency =  tpCustInfo.TpCurrencyId,
            gameid = 0,
            userid = 0,
            userstatus = 1,
            betpoint = "NA",
            betodds = 0.00m,
            betamount = rollbackData_WonCasino.amount,
            commamount = 0.00m,
            winlost = 0.00m,
            foregift = 0.00m,
            gameroundid =0,
            gameresult = rollbackData_WonCasino.errmsg,
            gametype = 0,
            gamestyle = 0,
            serialnumber = rollbackData_WonCasino.serialnumber,
            gameroundno = 0,
            gameno = 0,
            bettype = 0,
            cid = 0,
            state = (int)WonCasinoStateEnum.Void,
            //由於Deduct的Rollback request只有給單號，故createTime為Now - 10秒作為TransTime
            createtime = new DateTimeOffset(DateTime.UtcNow.AddSeconds(-10)).ToUnixTimeSeconds(),
            settletime =  new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
            UTCBetTime = DateTime.UtcNow.AddSeconds(-10),
            UTCSettleTime = DateTime.UtcNow,
            TicketType = ticketType,
            TicketStatus = ticketStatus,
            SettleFlag = settleFlag
        };
        
        return result;
    }
    
    #endregion

    
}