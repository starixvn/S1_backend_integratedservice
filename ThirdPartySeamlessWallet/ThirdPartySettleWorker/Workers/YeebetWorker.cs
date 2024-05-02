using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers;

internal class YeebetWorker : BackgroundService, ISettleWorker
{
    public Tp Tp => Tp.Yeebet;

    private Timer _timer;

    private readonly SettledBLL _settledBLL;
    private readonly TpUnSettledBLL _tpUnSettledBLL;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly TpCustInfoBLL _tpCustInfoBLL;
    private readonly ILogger<YeebetWorker> _logger;
    private readonly string _tpName;
    private readonly PartitionService _partitionService;

    public YeebetWorker(IServiceProvider provider)
    {
        _settledBLL = provider.GetRequiredService<SettledBLL>();
        _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
        _logger = provider.GetRequiredService<ILogger<YeebetWorker>>();
        _partitionService = provider.GetRequiredService<PartitionService>();
        _tpName = Enum.GetName(typeof(Tp), Tp);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoSettle, null, TimeSpan.Zero, TimeSpan.Zero);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private void DoSettle(object state)
    {
        LogContext.PushProperty("TP", _tpName);
        LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

        var forContinue = false;
        var isError = false;
        var gametype = GameType.Yeebet_LiveCasino;
        var tpCategory = TpCategory.LiveCasino;
        try
        {
            var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype);
            var unsettleList = tpUnsettleList.ToList();
            if (!unsettleList.Any())
            {
                throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
            }

            forContinue = unsettleList.Count() == 200;
            foreach (var tpUnsettle in unsettleList)
            {
                try
                {
                    var unsettleData = _partitionService.GetUnsettleDataList<SettleData>("Service_BettransLC_Yeebet_Unsettle_Get", tpUnsettle);

                    if (!unsettleData.Any()) continue;

                    var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettleData.FirstOrDefault().Account);
                    if (tpCustInfo == null)
                        throw new UnsettleNotFoundException($"{unsettleData.FirstOrDefault().Account} TpCustInfo not found.");

                    var wager = _settledBLL.GetSettle_TpWagerLC_Yeebet(unsettleData.FirstOrDefault().TransId).FirstOrDefault(); //serialnumber

                    if (wager == null) continue;

                    if (!CanSettle(unsettleData, wager, tpCustInfo,
                        out decimal stake, out decimal validbet, out decimal winlost)) continue;

                    var betRecordInfo = new BetRecordInfo
                    {
                        TpId = (int)Tp,
                        Stake = stake,
                        ValidBet = validbet,
                        WinlostAmount = winlost,
                        TransId = tpUnsettle.TransId, //serialnumber
                        GameType = gametype,
                        TpUserName = tpCustInfo.TpUserName,
                        SettledTime = wager.SettleTime,
                        TransTime = wager.TransTime,
                        SettlementType = SetSettlementType(wager.Status, wager.GameId),
                        TicketStatus = SetTicketStatus(wager.Status, winlost),
                        GameID = wager.GameId,
                    };

                    var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                    if (result == 1)
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, true);
                        });

                }
                catch (Exception ex)
                {
                    Task.Run(() =>
                    {
                        _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, false);
                    });
                    isError = true;
                    _logger.LogError(ex, $"TransId:{tpUnsettle.TransId} settle failed, exMessage:{ex.Message}");
                }
            }
        }
        catch (UnsettleNotFoundException ex)
        {
            _logger.LogInformation(ex, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
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

            _timer?.Change(dueTimeSpan, TimeSpan.Zero);
        }
    }

    private SettlementType SetSettlementType(int status, string gameId)
    {
        switch (status)
        {
            case (int)stateEnum.Settle:
                return _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, gameId);
            case (int)stateEnum.Bet:
                return SettlementType.UnSettled;
            case (int)stateEnum.Fail:
            case (int)stateEnum.Cancel:
            case (int)stateEnum.Abandoned:
            default:
                return SettlementType.SettledForInvalid;
        }
    }

    private TicketStatus SetTicketStatus(int status, decimal winLostAmount)
    {
        switch (status)
        {
            case (int)stateEnum.Settle:
                if (winLostAmount == 0) return TicketStatus.Draw;

                if (winLostAmount > 0) return TicketStatus.Win;

                return TicketStatus.Lost;
            case (int)stateEnum.Bet:
                return TicketStatus.UnSettled;
            case (int)stateEnum.Fail:
            case (int)stateEnum.Cancel:
            case (int)stateEnum.Abandoned:
            default:
                return TicketStatus.Refund;
        }
    }

    private bool CanSettle(IEnumerable<SettleData> bettrans, SettleData_Yeebet wager, TpCustInfo tpCustInfo,
        out decimal stake, out decimal vaildbet, out decimal winlost)
    {
        var account = bettrans.FirstOrDefault().Account;
        stake = bettrans.Sum(x => x.Stake);
        vaildbet = bettrans.Sum(x => x.ValidBet);
        winlost = bettrans.Sum(x => x.WinLostAmount);

        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
        if (tpCurrency.ExchangeRate <= 0) return false;

        // 加入幣別匯率轉換，tpWager 是原始資料
        decimal wagerStake = wager.Stake * tpCurrency.ExchangeRate;
        decimal wagerValidBet = wager.ValidBet * tpCurrency.ExchangeRate;
        decimal? wagerWinLostAmount = wager.WinLostAmount * tpCurrency.ExchangeRate;

        // 如果投注player或banker，但結果為tie，Yeebet後台的有效投注資料為0
        if (wager.Stake != 0 && wager.ValidBet == 0 && wager.WinLostAmount == 0)
            vaildbet = wagerValidBet;

        return wager.Account == account
               && wagerStake == stake
               && wagerValidBet == vaildbet
               && wagerWinLostAmount == winlost;
    }

}
