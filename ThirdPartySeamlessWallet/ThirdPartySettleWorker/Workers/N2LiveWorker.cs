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
            foreach (var tpUnsettle in tpUnsettleList)
            {
                try
                {
                    var getUnsettledData = new List<SettleData>();
                    var getWagerData = new List<SettleData_N2Live>();
                    var TransId = tpUnsettle.TransId;
                    var checkTransId = CheckTransId(TransId);
                    
                    var unsettledSPName = checkTransId ? "Service_BettransLC_N2Live_Unsettle_Get_V2" : "Service_TpSmartWagerLC_N2Live_Unsettle_Get_V2";
                    var wagerSPName = checkTransId ? "Service_TpWagerLC_N2Live_Get_V2" : "Service_TpSmartWagerLC_N2Live_Unsettle_Get_V2";
                    
                    getUnsettledData = GetN2LiveData<SettleData>(unsettledSPName, new {TransId = tpUnsettle.TransId}, DataBase.Main).ToList();
                    getWagerData = GetN2LiveData<SettleData_N2Live>(wagerSPName, new {TransId = tpUnsettle.TransId}, DataBase.Wager).ToList();
                    
                    var unsettledData = getUnsettledData.FirstOrDefault();
                    var wagerData = getWagerData.FirstOrDefault();
                    
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
                        if (checkTransId)
                        {
                            _settledBLL.SetSettleFlag_TpWagerLC_N2Live(TransId);
                            _settledBLL.SetSettleFlag_BettransLC_N2Live(TransId, 1);
                        }
                        else
                        {
                            _settledBLL.SetSettleFlag_TpSmartWagerLC_N2Live(TransId);
                        }
                        _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, true);
                    });

                }
                catch (Exception ex)
                {
                    Task.Run(() =>
                    {
                        _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, isSuccess: false);
                    });
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

    #region private

    private bool CheckTransId(string transId)
    {
        return transId.Length > 10;
    }
    
    private IEnumerable<T> GetN2LiveData<T>(string serviceName, object parameters, DataBase database)
    {
        return _tpUnSettledBLL.GetTpUnsettleData<T>(serviceName, parameters, database);
    }

    #endregion
}