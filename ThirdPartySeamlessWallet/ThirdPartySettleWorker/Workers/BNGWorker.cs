using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class BNGWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.BNG; }
        private Timer timer;
        private readonly string _tpName;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly SettledBLL _settledBLL;
        private readonly PartitionService _partitionService;
        private readonly ILogger<BNGWorker> _logger;
        public BNGWorker(IServiceProvider provider)
        {
            _tpName = Enum.GetName(typeof(Tp), Tp);
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _logger = provider.GetRequiredService<ILogger<BNGWorker>>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(Settle, GameType.BNG_RNG, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        private void Settle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            const string getBettransSp = "Service_BettransRng_BNG_Get"; // 取得下注紀錄
            const string getWagerSp = "Service_TpWager_BNG_Get"; // 取得Wager資料

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, GameType.BNG_RNG);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {GameType.BNG_RNG} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;

                foreach (var tpUnsettle in tpUnsettleList)
                {
                    try
                    {
                        var betData = _partitionService.GetUnsettleDataList<BettingDataRNG_BNG>(getBettransSp, tpUnsettle).ToList();
                        var wagerData = _tpUnSettledBLL.GetTpUnsettleData<BNGWager>(getWagerSp, new { round_id = long.Parse(tpUnsettle.TransId) }, DataBase.Wager).ToList();

                        if (wagerData == null && wagerData.Exists(x => x.round_finished))
                            continue;

                        var tpUserName = betData.FirstOrDefault().id;
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);

                        var wagerDataBet = wagerData.Sum(x => x.bet);
                        var wagerDataWin = wagerData.Sum(x => x.win);
                        var betDataBet = betData.Sum(x => x.bet);
                        var betDataWin = betData.Sum(x => x.win);

                        #region 匯率轉換
                        wagerDataBet *= tpCurrency.ExchangeRate;
                        wagerDataWin *= tpCurrency.ExchangeRate;
                        #endregion

                        if (wagerDataBet != betDataBet || wagerDataWin != betDataWin)
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"betData=\r\n{JsonConvert.SerializeObject(betData)}\r\nwagerData=\r\n{JsonConvert.SerializeObject(wagerData)}");
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.BNG_RNG, tpUnsettle.TransId, false);
                            });
                            continue;
                        }

                        var winlostAmount = wagerDataWin - wagerDataBet;

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = wagerDataBet,
                            ValidBet = wagerDataBet,
                            WinlostAmount = winlostAmount,
                            TransId = tpUnsettle.TransId,
                            GameType = GameType.BNG_RNG,
                            TpUserName = tpUserName,
                            SettledTime = wagerData.Max(x => x.c_at),
                            TransTime = wagerData.Min(x => x.c_at),
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, wagerData.FirstOrDefault().game_id),
                            TicketStatus = winlostAmount > 0 ? TicketStatus.Win : winlostAmount == 0 ? TicketStatus.Draw : TicketStatus.Lost,
                        };

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Slots);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.BNG_RNG, tpUnsettle.TransId, true);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.BNG_RNG, tpUnsettle.TransId, false);
                        });
                        LogContext.PushProperty("TP", _tpName);
                        _logger.LogError(ex, $"BNG Settle exception. {ex.Message} || TransId : {tpUnsettle.TransId}");
                    }
                }
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", _tpName);
                _logger.LogError(ex, $"BNG Settle exception. {ex.Message}");
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }

                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
