using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class AGWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.AG;
        private Timer _timerLC;
        private readonly SettledBLL _settledBll;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly TpUnSettledBLL _tpUnSettledBll;
        private readonly ILogger<AGWorker> _logger;
        private readonly string _tpName;

        public AGWorker(IServiceProvider provider)
        {
            _settledBll = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBll = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
            _tpUnSettledBll = provider.GetRequiredService<TpUnSettledBLL>();
            _logger = provider.GetRequiredService<ILogger<AGWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerLC = new Timer(DoSettleSportWork, GameType.AG_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerLC?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleSportWork(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gameType = (GameType)state;
            var logName = "AGDoSettleLiveCasinoWork";

            try
            {
                var tpUnsettleList = _tpUnSettledBll.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gameType} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var unsettled in tpUnsettleList)
                {
                    try
                    {
                        var payoutList = _settledBll.Get_PayoutList_AG(unsettled.TransId);
                        var betList = new List<BettingDataLC_AG>();
                        var transactionIDs = new List<string>();
                        if (payoutList != null)
                        {
                            var playName = payoutList.FirstOrDefault().Playname;
                            var agentCode = payoutList.FirstOrDefault().AgentCode;
                            
                            foreach (var payout in payoutList)
                            {
                                transactionIDs.AddRange(JsonConvert.DeserializeObject<IEnumerable<string>>(payout.TransactionIDs));
                            }

                            var bBetList = _settledBll.Get_BetList_AG(JsonConvert.SerializeObject(transactionIDs.Distinct()));

                            if (bBetList != null)
                            {
                                betList.AddRange(bBetList);
                            }

                            if (betList.Count > 0 && betList.Sum(x => x.Value) == payoutList.Sum(x => x.ValidBetAmount))
                            {
                                var billNoList = payoutList.Select(x => x.BillNo).ToArray();
                                var wager = _settledBll.GetSettle_TpWagerAG(JsonConvert.SerializeObject(billNoList));

                                if (wager != null)
                                {
                                    string username = playName.Substring(agentCode.Length);

                                    var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)Tp.AG, username);
                                    var tpCurrency = _thirdPartyBll.GetTpCurrency((int)Tp.AG, tpCustInfo.CurrencyId);

                                    if (tpCurrency.ExchangeRate > 1)
                                    {
                                        wager.Stake *= tpCurrency.ExchangeRate;
                                        wager.ValidBet *= tpCurrency.ExchangeRate;
                                        wager.WinLostAmount *= tpCurrency.ExchangeRate;
                                    }

                                    if (betList.Sum(x => x.Value) == wager.Stake)
                                    {
                                        var betRecordInfo = new BetRecordInfo
                                        {
                                            TpId = (int)Tp.AG,
                                            Stake = wager.Stake,
                                            ValidBet = wager.ValidBet,
                                            WinlostAmount = wager.WinLostAmount,
                                            TransId = unsettled.TransId,
                                            GameType = GameType.AG_LiveCasino,
                                            TpUserName = username,
                                            SettledTime = wager.SettleTime,
                                            TransTime = wager.TransTime,
                                            SettlementType = SettlementType.SettledForValid,
                                            GameID = wager.GameId,
                                        };

                                        var result = _settledBll.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                                        if (result == 1)
                                        {
                                            Task.Run(() =>
                                            {
                                                foreach (var transactionId in transactionIDs)
                                                {
                                                    _tpUnSettledBll.SetTpUnsettle(Tp, gameType, transactionId, isSuccess: true);
                                                }
                                                _settledBll.SetSettleFlag_TpWagerAG(JsonConvert.SerializeObject(billNoList));
                                                _settledBll.SetSettleFlag_AG(unsettled.TransId, JsonConvert.SerializeObject(transactionIDs));
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex1)
                    {
                        LogContext.PushProperty("TP", _tpName);
                        Task.Run(() =>
                        {
                            _tpUnSettledBll.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
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
                isError = true;
                _logger.LogError(ex.ToString(), logName);
            }
            finally
            {
                var dueTimeSpan = GetDueTimeSpan(forContinue, isError);
                _timerLC.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private static TimeSpan GetDueTimeSpan(bool forContinue, bool isError)
        {
            var dueTimeSpan = TimeSpan.FromMinutes(1);

            if (forContinue)
            {
                dueTimeSpan = TimeSpan.FromSeconds(1);
            }
            else if (isError)
            {
                dueTimeSpan = TimeSpan.FromMinutes(1);
            }

            return dueTimeSpan;
        }
    
    }
}
