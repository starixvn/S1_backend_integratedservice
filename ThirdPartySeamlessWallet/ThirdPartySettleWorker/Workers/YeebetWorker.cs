using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;
using ThirdPartySeamlessWallet.Service.DAL;

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
                    var unsettleData = _partitionService.GetUnsettleDataList<SettleData>("Service_BettransLC_Yeebet_Unsettle_Get", tpUnsettle).FirstOrDefault();
                    if (unsettleData != null)
                    {
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettleData.Account);
                        if (tpCustInfo == null)
                            throw new UnsettleNotFoundException($"{unsettleData.Account} TpCustInfo not found.");

                        //從這裡開始
                        var wager = _settledBLL.GetSettle_TpWagerLC_Yeebet(unsettleData.TransId); //serialnumber

                        if (wager == null || !wager.Any()) continue;

                        if (!CanSettle(unsettleData, wager.First(), tpCustInfo,
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
                            SettledTime = wager.First().SettleTime,
                            TransTime = wager.First().TransTime,
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, unsettleData.GameId),
                            TicketStatus = SetTicketStatus(unsettleData.WinLostAmount)
                        };

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, true);
                            });
                        }
                    }
                }
                catch (Exception ex1)
                {
                    Task.Run(() =>
                    {
                        _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, false);
                    });
                    isError = true;
                    _logger.LogError(ex1, $"TransId:{tpUnsettle.TransId} settle failed, exMessage:{ex1.Message}");
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

    private TicketStatus SetTicketStatus(decimal winLostAmount)
    {
        if (winLostAmount == 0) return TicketStatus.Draw;

        if (winLostAmount > 0) return TicketStatus.Win;

        return TicketStatus.Lost;
    }

    private bool CanSettle(SettleData bettrans, SettleData_Yeebet wager, TpCustInfo tpCustInfo,
        out decimal stake, out decimal vaildbet, out decimal winlost)
    {
        stake = bettrans.Stake; vaildbet = bettrans.ValidBet; winlost = bettrans.WinLostAmount;

        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
        if (tpCurrency.ExchangeRate <= 0) return false;

        // 加入幣別匯率轉換，tpWager 是原始資料
        decimal wagerStake = wager.Stake * tpCurrency.ExchangeRate;
        decimal wagerValidBet = wager.ValidBet * tpCurrency.ExchangeRate;
        decimal? wagerWinLostAmount = wager.WinLostAmount * tpCurrency.ExchangeRate;


        if (wager.Stake != 0 && wager.ValidBet == 0 && wager.WinLostAmount == 0)
        {
            // 如果投注player或banker，但結果為tie，Yeebet後台的有效投注資料為0
            vaildbet = wagerValidBet;
            return wager.Account == bettrans.Account
               && wagerStake == bettrans.Stake
               && wagerWinLostAmount == bettrans.WinLostAmount;
        }

        return wager.Account == bettrans.Account
               && wagerStake == bettrans.Stake
               && wagerValidBet == bettrans.ValidBet
               && wagerWinLostAmount == bettrans.WinLostAmount;
    }

}
