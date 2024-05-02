using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers;

internal class BGWorker : BackgroundService, ISettleWorker
{
    public Tp Tp => Tp.BG;

    private Timer _timer;

    private readonly SettledBLL _settledBLL;
    private readonly TpUnSettledBLL _tpUnSettledBLL;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly TpCustInfoBLL _tpCustInfoBLL;
    private readonly ILogger<BGWorker> _logger;
    private readonly string _tpName;
    private readonly PartitionService _partitionService;

    public BGWorker(IServiceProvider provider)
    {
        _settledBLL = provider.GetRequiredService<SettledBLL>();
        _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
        _logger = provider.GetRequiredService<ILogger<BGWorker>>();
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
        var gameType = GameType.BG_LiveCasino;
        var tpCategory = TpCategory.LiveCasino;

        const string logName = "SettleBG";
        const string getBettransSp = "Service_BettransLC_BG_Unsettle_Get"; // 取得未結算的注單-Bettrans
        const string getWagerSp = "Service_TpWagerLC_BG_Get";              // 取得未結算的注單-Wager

        try
        {
            var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
            var unsettleList = tpUnsettleList.ToList().OrderBy(x => x.TransId);
            if (!unsettleList.Any())
            {
                throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
            }

            forContinue = unsettleList.Count() == 200;
            foreach (var tpUnsettle in unsettleList)
            {
                try
                {
                    var settleData = _partitionService.GetUnsettleDataList<SettleData>(getBettransSp, tpUnsettle).FirstOrDefault();
                    if (settleData == null) continue;
                    var tpWagerData = _tpUnSettledBLL.GetTpUnsettleData<SettleData_BG>(getWagerSp, new { _orderId = tpUnsettle.TransId }, DataBase.Wager).FirstOrDefault();
                    if (tpWagerData == null) continue;

                    var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, settleData.Account);
                    if (tpCustInfo == null)
                    {
                        _logger.LogWarning($"{logName} {settleData.Account} TpCustInfo not found.");
                        continue;
                    }

                    var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                    if (tpCurrency == null)
                    {
                        _logger.LogWarning($"{logName} {settleData.Account} TpCurrency not found.");
                        continue;
                    }

                    if (!IsBettransSameAsTpWager(settleData, tpWagerData, tpCurrency))
                    {
                        _logger.LogWarning($"transId: {tpUnsettle.TransId} is not equal " +
                        $"settle: {JsonConvert.SerializeObject(settleData)} " +
                        $"wager: {JsonConvert.SerializeObject(tpWagerData)}");
                        continue;
                    }

                    var betRecordInfo = new BetRecordInfo
                    {
                        TpId = (int)Tp,
                        Stake = settleData.Stake,
                        ValidBet = settleData.ValidBet,
                        WinlostAmount = settleData.WinLostAmount,
                        TransId = tpUnsettle.TransId, //orderId
                        GameType = gameType,
                        TpUserName = tpCustInfo.TpUserName,
                        SettledTime = tpWagerData.SettleTime,
                        TransTime = tpWagerData.TransTime,
                        SettlementType = SetSettlementType(tpWagerData.Status, tpWagerData.GameId),
                        TicketStatus = SetTicketStatus(tpWagerData.Status),
                        GameID = tpWagerData.GameId
                    };

                    var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                    if (result == 1)
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, true);
                        });

                }
                catch (Exception ex)
                {
                    Task.Run(() =>
                    {
                        _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, false);
                    });
                    isError = true;
                    _logger.LogError(ex, $"{logName} TransId:{tpUnsettle.TransId} settle failed, exMessage:{ex.Message}");
                }
            }
        }
        catch (UnsettleNotFoundException ex)
        {
            _logger.LogInformation(ex, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{logName} exMessage:{ex.Message}");
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
            case (int)BGOrderStatusEnum.NotExist:
            case (int)BGOrderStatusEnum.UnSettle:
                return SettlementType.UnSettled;
            case (int)BGOrderStatusEnum.SettledLose:
            case (int)BGOrderStatusEnum.SettledDraw:
            case (int)BGOrderStatusEnum.SettledWin:
                return SettlementType.SettledForValid;
            case (int)BGOrderStatusEnum.Cancel:
            case (int)BGOrderStatusEnum.Expired:
            case (int)BGOrderStatusEnum.SystemCancel:
            default:
                return SettlementType.SettledForInvalid;
        }
    }

    private TicketStatus SetTicketStatus(int status)
    {
        switch (status)
        {
            case (int)BGOrderStatusEnum.NotExist:
            case (int)BGOrderStatusEnum.UnSettle:
                return TicketStatus.UnSettled;
            case (int)BGOrderStatusEnum.SettledLose:
                return TicketStatus.Lost;
            case (int)BGOrderStatusEnum.SettledDraw:
                return TicketStatus.Draw;
            case (int)BGOrderStatusEnum.SettledWin:
                return TicketStatus.Win;
            case (int)BGOrderStatusEnum.Cancel:
            case (int)BGOrderStatusEnum.Expired:
            case (int)BGOrderStatusEnum.SystemCancel:
            default:
                return TicketStatus.Refund;
        }
    }

    private bool IsBettransSameAsTpWager(SettleData bettrans, SettleData_BG wager, TpCurrency tpCurrency)
    {
        if (tpCurrency.ExchangeRate <= 0) return false;

        // 加入幣別匯率轉換，tpWager 是原始資料
        decimal wagerStake = (wager.Stake * -1) * tpCurrency.ExchangeRate;
        decimal wagerValidBet = wager.ValidBet * tpCurrency.ExchangeRate;
        decimal? wagerWinLostAmount = wager.WinLostAmount * tpCurrency.ExchangeRate;

        // 如果投注player或banker，但結果為tie，Yeebet後台的有效投注資料為0
        //if (wager.Stake != 0 && wager.ValidBet == 0 && wager.WinLostAmount == 0)
        //    vaildbet = wagerValidBet;

        return wager.Account == bettrans.Account
               && wagerStake == bettrans.Stake
               && wagerValidBet == bettrans.ValidBet
               && wagerWinLostAmount == bettrans.WinLostAmount;
    }

}
