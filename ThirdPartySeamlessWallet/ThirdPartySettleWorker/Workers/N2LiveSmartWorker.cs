using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers;

public class N2LiveSmartWorker : BackgroundService, ISettleWorker
{
    public Tp Tp => Tp.N2Live;
    private Timer _timerLC;
    
    private readonly SettledBLL _settledBLL;
    private readonly TpCustInfoBLL _tpCustInfoBLL;
    private readonly TpUnSettledBLL _tpUnSettledBLL;
    private readonly ILogger<N2LiveWorker> _logger;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly string _tpName;
    
    public N2LiveSmartWorker(IServiceProvider provider)
    {
        _settledBLL = provider.GetRequiredService<SettledBLL>();
        _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
        _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
        _logger = provider.GetRequiredService<ILogger<N2LiveWorker>>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _tpName = Enum.GetName(typeof(Tp), Tp);
        LogContext.PushProperty("TP", _tpName);
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timerLC = new Timer(DoSettleN2LiveWork, GameType.N2Live_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
        return Task.CompletedTask;
    }

    private void DoSettleN2LiveWork(object state)
    {
        LogContext.PushProperty("TP", _tpName);
        LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

        var forContinue = false;
        var isError = false;
        var gameType = (GameType)state;
        var tpId = (int)Tp.N2Live;
        var logName = "N2LiveSmartDoSettleLiveCasinoWork";
        
        try
        {
            var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
            if (!tpUnsettleList.Any())
            {
                throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
            }

            forContinue = tpUnsettleList.Count() == 200;
            //過濾是否為Smart
            tpUnsettleList = tpUnsettleList.Where(x => x.TransId.Length < 10).ToList();
            foreach (var tpUnsettle in tpUnsettleList)
            {
                try
                {
                    var TransId = tpUnsettle.TransId;
                    var unsettledData = _tpUnSettledBLL.GetTpUnsettleData<SettleData_N2Live_Smart>("Service_SmartBettransLC_N2Live_Unsettled_Get",
                        new { schemeid = tpUnsettle.TransId }, DataBase.Main).FirstOrDefault();
                    var wagerData = _tpUnSettledBLL.GetTpUnsettleData<SettleData_N2Live_Smart>(
                        "Service_TpSmartWagerLC_N2Live_Unsettle_Get_V2",
                        new { schemeid = tpUnsettle.TransId }, DataBase.Wager);
                    
                    var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, wagerData.FirstOrDefault().Account);
                    var tpCurrency = _thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);

                    if (wagerData.Sum(x => x.WinLostAmount) * tpCurrency.ExchangeRate != unsettledData.WinLostAmount) continue;
                    foreach (var wager in wagerData)
                    {
                        // var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, wager.Account);
                        // var tpCurrency = _thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate > 1)
                        {
                            wager.Stake *= tpCurrency.ExchangeRate;
                            wager.ValidBet *= tpCurrency.ExchangeRate;
                            wager.WinLostAmount *= tpCurrency.ExchangeRate;
                        }
                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = tpId,
                            Stake = wager.Stake,
                            ValidBet = wager.ValidBet,
                            WinlostAmount = wager.WinLostAmount,
                            TransId = wager.TransId,
                            GameType = (GameType)gameType,
                            TpUserName = unsettledData.Account,
                            SettledTime = wager.SettleTime,
                            TransTime = wager.TransTime,
                            SettlementType = SettlementType.SettledForValid,
                            ReferenceNo = wager.schemeid, // for smart bet mapping
                        };

                        _ = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);
                        _ = _settledBLL.SetSettleFlag_TpSmartWagerLC_N2Live(wager.TransId);
                    }
                    Task.Run(() =>
                    {
                        _settledBLL.SetSettleFlag_BettransSmartLC_N2Live(TransId, 1);
                        _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, true);
                    });
                    

                }
                catch (Exception ex)
                {
                    isError = true;
                    _logger.LogError(ex, $"N2LiveSmartDoSettleLiveCasinoWork:{ex.Message}" , logName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString(), logName);
            isError = true;
        }
        finally
        {
            TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);
            if (forContinue)
            {
                dueTimeSpan = TimeSpan.FromSeconds(5);
            }
            else if (isError)
            {
                dueTimeSpan = TimeSpan.FromMinutes(1);
            }

            _timerLC?.Change(dueTimeSpan, TimeSpan.Zero);
        }
    }
}