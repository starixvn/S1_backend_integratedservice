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
    public class AMBWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.AMB;

        private Timer _timerRng;
        private Timer _timerPoker;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<AMBWorker> _logger;
        private readonly string _tpName;
        private readonly PartitionService _partitionService;

        public AMBWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<AMBWorker>>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerRng = new Timer(DoSettleRng, GameType.AMB_RNG, TimeSpan.Zero, TimeSpan.Zero);
            _timerPoker = new Timer(DoSettlePoker, GameType.AMB_Poker, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerRng?.Change(Timeout.Infinite, 0);
            _timerPoker?.Change(Timeout.Infinite, 0);
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
            var logName = "Settle_AMB_RNG";
            var bettransGet = "Service_BettransRng_AMB_Settle_Get";
            var wagerGet = "Service_TpWagerRng_AMB_Settle_Get";

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
                        var unsettleData = _partitionService.GetUnsettleDataList<SettleDataRNG_AMB>(bettransGet, tpUnsettle).FirstOrDefault();
                        if (unsettleData == null)
                        {
                            _logger.LogWarning($"{logName}:bettrans not found ({gametype})\r\n{JsonConvert.SerializeObject(tpUnsettle)}");
                            continue;
                        }

                        var wager = _tpUnSettledBLL.GetTpUnsettleData<SettleData_AMB>(wagerGet, new { _TransId = tpUnsettle.TransId, _TpUserName = unsettleData.Account }, DataBase.Wager).FirstOrDefault();
                        if (wager == null)
                        {
                            _logger.LogWarning($"{logName}:wager not found ({gametype})\r\n{JsonConvert.SerializeObject(tpUnsettle)}");
                            continue;
                        }

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettleData.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate != 0)
                        {
                            wager.Stake *= tpCurrency.ExchangeRate;
                            wager.WinLostAmount *= tpCurrency.ExchangeRate;
                            wager.ValidBet *= tpCurrency.ExchangeRate;
                        }

                        if (string.Equals(unsettleData.Categories, "fishing", StringComparison.OrdinalIgnoreCase))
                        {
                            unsettleData.Stake = unsettleData.Bet;
                            unsettleData.WinLostAmount = unsettleData.WinLost;
                        }

                        if (string.Equals(unsettleData.Categories, "slot", StringComparison.OrdinalIgnoreCase) && unsettleData.Stake == 0m && unsettleData.WinLostAmount == 0m && unsettleData.Bet > 0)
                        {
                            unsettleData.Stake = unsettleData.Bet;
                            unsettleData.WinLostAmount = unsettleData.WinLost;
                        }

                        if (wager.Stake != unsettleData.Stake || wager.WinLostAmount != unsettleData.WinLostAmount)
                        {
                            _logger.LogError($"{logName}:amount not match\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettleData)}");
                            continue;
                        }

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = unsettleData.Stake,
                            ValidBet = wager.ValidBet,
                            WinlostAmount = unsettleData.WinLostAmount,
                            TransId = tpUnsettle.TransId,
                            GameType = gametype,
                            TpUserName = unsettleData.Account,
                            SettledTime = unsettleData.SettleTime,
                            TransTime = unsettleData.TransTime,
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, unsettleData.GameId),
                            TicketStatus = SetTicketStatus(unsettleData.WinLostAmount),
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

        private void DoSettlePoker(object state)
        {
            var forContinue = false;
            var isError = false;
            var gameType = (GameType)state;
            var logName = "Settle_AMB_Poker";
            var bettransGet = "Service_BettransPK_AMB_Settle_Get";
            var wagerGet = "Service_TpWagerPK_AMB_Settle_Get";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                //forContinue
                forContinue = tpUnsettleList.Count() == 200;

                foreach (var ticket in tpUnsettleList)
                {
                    var getUnsettledList = _tpUnSettledBLL.GetTpUnsettleData<SettleData_AMB>(bettransGet, new { _TransId = ticket.TransId }, DataBase.Main);
                    if (getUnsettledList == null || !getUnsettledList.Any())
                    {
                        _logger.LogWarning($"{logName}:bettrans not found ({gameType})\r\n{JsonConvert.SerializeObject(ticket)}");
                        continue;
                    }

                    var tpUserName = getUnsettledList.FirstOrDefault().Account;
                    var roundid = ticket.TransId.Replace($"_{tpUserName}", "");

                    var wager = _tpUnSettledBLL.GetTpUnsettleData<SettleData_AMB>(wagerGet, new { _TransId = roundid, _TpUserName = tpUserName }, DataBase.Wager).FirstOrDefault();
                    if (wager == null)
                    {
                        _logger.LogWarning($"{logName}:wager not found ({gameType})\r\n{JsonConvert.SerializeObject(ticket)}");
                        continue;
                    }

                    try
                    {
                        var unsettled = getUnsettledList.FirstOrDefault();
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettled.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);

                        decimal stake = unsettled.Stake;
                        decimal validBet = unsettled.ValidBet;
                        decimal winLostAmount = unsettled.WinLostAmount;
                        decimal revenue = (decimal)unsettled.Revenue;

                        if (tpCurrency.ExchangeRate != 0)
                        {
                            wager.Stake *= tpCurrency.ExchangeRate;
                            wager.WinLostAmount *= tpCurrency.ExchangeRate;
                            wager.ValidBet *= tpCurrency.ExchangeRate;
                        }

                        if (wager.Stake != stake || wager.WinLostAmount != winLostAmount)
                        {
                            _logger.LogWarning($"{logName}:amount not match\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = stake,
                            ValidBet = wager.ValidBet,
                            Revenue = revenue,
                            WinlostAmount = winLostAmount,
                            TransId = unsettled.TransId,
                            GameType = gameType,
                            TpUserName = unsettled.Account,
                            SettledTime = unsettled.SettleTime,
                            TransTime = unsettled.TransTime,
                            SettlementType = SettlementType.SettledForValid,
                            GameID = unsettled.GameId,
                        };

                        if (unsettled.IsRefund == 1)
                        {
                            betRecordInfo.TicketStatus = TicketStatus.Refund;
                        }

                        var result = _settledBLL.Settled_Without_Bonus(tpCustInfo, betRecordInfo);

                        if (result == 1)
                        {
                            _ = _settledBLL.SetSettleFlag_BettransPK_AMB(unsettled.TransId);
                            _ = _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, unsettled.TransId, true);
                        }

                    }
                    catch (Exception ex1)
                    {
                        LogContext.PushProperty("TP", _tpName);
                        isError = true;
                        _logger.LogError(ex1, $"{logName}:{JsonConvert.SerializeObject(ticket)}");
                    }
                }

            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", _tpName);
                _logger.LogError(ex, $"{logName}:{ex.Message}");
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                _timerPoker?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private static TicketStatus SetTicketStatus(decimal winLostAmount)
        {
            if (winLostAmount == 0) return TicketStatus.Draw;

            if (winLostAmount > 0) return TicketStatus.Win;

            return TicketStatus.Lost;
        }
    }
}
