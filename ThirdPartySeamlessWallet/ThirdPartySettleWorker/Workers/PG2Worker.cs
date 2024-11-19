using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers;

public class PG2Worker : BackgroundService, ISettleWorker
{
    public Tp Tp => Tp.PG2;

    private Timer _timerRng;

    private readonly SettledBLL _settledBLL;
    private readonly TpUnSettledBLL _tpUnSettledBLL;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly TpCustInfoBLL _tpCustInfoBLL;
    private readonly ILogger<PG2Worker> _logger;
    private readonly string _tpName;
    private readonly PartitionService _partitionService;

    public PG2Worker(IServiceProvider provider)
    {
        _settledBLL = provider.GetRequiredService<SettledBLL>();
        _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
        _logger = provider.GetRequiredService<ILogger<PG2Worker>>();
        _partitionService = provider.GetRequiredService<PartitionService>();
        _tpName = Enum.GetName(typeof(Tp), Tp);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timerRng = new Timer(DoSettleRng, GameType.PG2_RNG, TimeSpan.Zero, TimeSpan.Zero);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _timerRng?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private void DoSettleRng(object state)
    {
        LogContext.PushProperty("TP", _tpName);
        LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

        var forContinue = false;
        var isError = false;
        var gametype = (GameType)state;
        var tpCategory = TpCategory.Slots;
        var logName = "Settle_PG2_RNG";
        var bettransGet = "Service_BettransRng_PG2_Settle_Get";

        try
        {
            var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype);
            var unsettleList = tpUnsettleList.ToList();
            if (!unsettleList.Any())
            {
                throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
            }

            forContinue = unsettleList.Count == 200;

            foreach (var tpUnsettle in unsettleList)
            {
                try
                {
                    var unsettleData = _tpUnSettledBLL.GetTpUnsettleData<SettleDataRNG_PG2>(bettransGet, new { TransId = tpUnsettle.TransId},DataBase.Main).First();
                    if (unsettleData == null)
                    {
                        _logger.LogWarning($"{logName}:bettrans not found ({gametype})\r\n{JsonConvert.SerializeObject(tpUnsettle)}");
                        continue;
                    }

                    var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettleData.Account);

                    var betRecordInfo = new BetRecordInfo
                    {
                        TpId = (int)Tp,
                        Stake = unsettleData.Stake,
                        ValidBet = unsettleData.ValidBet,
                        WinlostAmount = unsettleData.WinLostAmount,
                        TransId = tpUnsettle.TransId,
                        GameType = gametype,
                        TpUserName = unsettleData.Account,
                        SettledTime = unsettleData.SettleTime,
                        TransTime = unsettleData.TransTime,
                        SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, unsettleData.GameId),
                        GameID = unsettleData.GameId,
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

            _timerRng?.Change(dueTimeSpan, TimeSpan.Zero);
        }
    }
}