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
    public class FaChaiWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.FaChai; }
        private Timer timer;
        private readonly string _tpName;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly PartitionService _partitionService;
        private readonly SettledBLL _settledBLL;
        private readonly ILogger<FaChaiWorker> _logger;

        public FaChaiWorker(IServiceProvider provider)
        {
            _tpName = Enum.GetName(typeof(Tp), Tp);
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _logger = provider.GetRequiredService<ILogger<FaChaiWorker>>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(Settle, GameType.FaChai_RNG, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }

        private void Settle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            const string getBettransSp = "Service_BettransRng_FaChai_Get"; // 取得下注紀錄
            //const string getWagerSp = "Service_TpWager_FaChai_Get"; // 取得Wager資料

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, GameType.FaChai_RNG);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {GameType.FaChai_RNG} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;

                foreach (var tpUnsettle in tpUnsettleList)
                {
                    try
                    {
                        var betData = _partitionService.GetUnsettleDataList<BettransRng_FaChai>(getBettransSp, tpUnsettle);

                        if (!betData.Any())
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"Settle_FaChai:bettrans not found ({(GameType)state})\r\n{JsonConvert.SerializeObject(tpUnsettle)}");
                            continue;
                        }

                        //var wagerData = _tpUnSettledBLL.GetTpUnsettleData<FCWager>(getWagerSp, new { recordID = tpUnsettle.TransId }, DataBase.Wager).FirstOrDefault();

                        //if (wagerData == null)
                        //    continue;


                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, betData.FirstOrDefault()?.MemberAccount);
                        //var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);

                        //#region 匯率轉換
                        //wagerData.bet *= tpCurrency.ExchangeRate;
                        //wagerData.winlose *= tpCurrency.ExchangeRate;
                        //#endregion

                        //if (betData.Bet != wagerData.bet || betData.NetWin != wagerData.winlose)
                        //{
                        //    LogContext.PushProperty("TP", _tpName);
                        //    _logger.LogWarning($"betData=\r\n{JsonConvert.SerializeObject(betData)}\r\nwagerData=\r\n{JsonConvert.SerializeObject(wagerData)}");
                        //    Task.Run(() =>
                        //    {
                        //        _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.FaChai_RNG, tpUnsettle.TransId, false);
                        //    });
                        //    continue;
                        //}

                        var winlostAmount = betData.Sum(x => x.NetWin);
                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = betData.Sum(x => x.Bet),
                            ValidBet = betData.Sum(x => x.Bet),
                            WinlostAmount = winlostAmount,
                            TransId = tpUnsettle.TransId,
                            GameType = GameType.FaChai_RNG,
                            TpUserName = betData.FirstOrDefault()?.MemberAccount,
                            SettledTime = betData.Max(x=>x.Ts),
                            TransTime = betData.Min(x => x.Ts),
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, betData.FirstOrDefault()?.GameID.ToString()),
                            TicketStatus = winlostAmount > 0 ? TicketStatus.Win : winlostAmount == 0 ? TicketStatus.Draw : TicketStatus.Lost,
                        };

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Slots);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.FaChai_RNG, tpUnsettle.TransId, true);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.FaChai_RNG, tpUnsettle.TransId, false);
                        });
                        SentrySdk.CaptureMessage($"FaChai Settle exception. {ex.Message} || TransId : {tpUnsettle.TransId}");
                        LogContext.PushProperty("TP", _tpName);
                        _logger.LogError(ex, $"FaChai Settle exception. {ex.Message} || TransId : {tpUnsettle.TransId}");
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
                _logger.LogError(ex, $"FaChai Settle exception. {ex.Message}");
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
