using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers;

public class N2LiveWorker : BackgroundService, ISettleWorker
{
    public Tp Tp => Tp.N2Live;
    private Timer _timerLC;
    
    private readonly SettledBLL _settledBLL;
    private readonly TpCustInfoBLL _tpCustInfoBLL;
    private readonly TpUnSettledBLL _tpUnSettledBLL;
    private readonly ILogger<N2LiveWorker> _logger;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly string _tpName;
    
    public N2LiveWorker(IServiceProvider provider)
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
        var logName = "N2LiveDoSettleLiveCasinoWork";
        
        try
        {
            var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
            if (!tpUnsettleList.Any())
            {
                throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
            }
            
            forContinue = tpUnsettleList.Count() == 200;
            //過濾是否為Smart
            tpUnsettleList = tpUnsettleList.Where(x => x.TransId.Length > 10).ToList();
            foreach (var tpUnsettle in tpUnsettleList)
            {
                try
                {
                    var TransId = tpUnsettle.TransId;
                    var unsettledData = _tpUnSettledBLL.GetTpUnsettleData<SettleData>("Service_BettransLC_N2Live_Unsettle_Get_V2",
                        new { tpUnsettle.TransId }, DataBase.Main).FirstOrDefault();
                    var wagerData = _tpUnSettledBLL.GetTpUnsettleData<SettleData_N2Live>("Service_TpWagerLC_N2Live_Get_V2",
                        new { tpUnsettle.TransId }, DataBase.Wager).FirstOrDefault();
                    
                    var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, wagerData.Account);
                    var tpCurrency = _thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
                    if (tpCurrency.ExchangeRate > 1)
                    {
                        wagerData.Stake *= tpCurrency.ExchangeRate;
                        wagerData.ValidBet *= tpCurrency.ExchangeRate;
                        wagerData.WinLostAmount *= tpCurrency.ExchangeRate;
                    }

                    if (wagerData.Stake != unsettledData.Stake || wagerData.WinLostAmount != unsettledData.WinLostAmount) continue;
                    var betRecordInfo = new BetRecordInfo
                    {
                        TpId = tpId,
                        Stake = wagerData.Stake,
                        ValidBet = wagerData.ValidBet,
                        WinlostAmount = wagerData.WinLostAmount,
                        TransId = unsettledData.TransId,
                        GameType = (GameType)gameType,
                        TpUserName = unsettledData.Account,
                        SettledTime = wagerData.SettleTime,
                        TransTime = wagerData.TransTime,
                        SettlementType = SettlementType.SettledForValid,
                        ReferenceNo = wagerData.betid, // for fix cant mapping bonusid when data fixed can remove
                        GameID = wagerData.GameId,
                    };

                    _ = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                    Task.Run(() =>
                    {
                        _settledBLL.SetSettleFlag_TpWagerLC_N2Live(TransId);
                        _settledBLL.SetSettleFlag_BettransLC_N2Live(TransId, 1);
                        _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, true);
                    });

                }
                catch (Exception ex)
                {
                    isError = true;
                    _logger.LogError(ex, $"N2LiveDoSettleLiveCasinoWork:{ex.Message}" , logName);

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