using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class V8Worker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.V8;
        // 正式切換後版本移除
        private Timer timerSettleV8;
        private Timer timer;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<V8Worker> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly PartitionService _partitionService;
        private readonly Tools _tools;
        private readonly string _tpName;
        private readonly int[] _kindId = {
            220, 600, 610, 620, 630,
            680, 830, 860, 890, 930,
            3101, 3890, 8100, 8540, 8600,
            8700, 1101, 1102
        };

        public V8Worker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<V8Worker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _tools = provider.GetRequiredService<Tools>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 正式切換後版本移除
            timerSettleV8 = new Timer(DoSettleV8Work, null, TimeSpan.Zero, TimeSpan.Zero);
            // 統一使用一款執行，後續再依退水遊戲區分
            timer = new Timer(DoSettleV8, GameType.V8_Poker, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerSettleV8?.Change(Timeout.Infinite, 0);
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        // 正式切換後版本移除
        private void DoSettleV8Work(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.V8;
            var logName = "Settle_V8_PK";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = _settledBLL.GetUnsettledListPK_V8();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var account = unsettled.Account;
                            if (account.IndexOf("_") > -1)
                            {
                                account = account[(account.IndexOf("_") + 1)..];
                            }

                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, account);
                            var tpCurrency = _thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                            if (tpCurrency.ExchangeRate > 1)
                            {
                                unsettled.Stake *= tpCurrency.ExchangeRate;
                                unsettled.ValidBet *= tpCurrency.ExchangeRate;
                                unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                                unsettled.Revenue *= tpCurrency.ExchangeRate;
                            }

                            // revenue games
                            GameType gameType;
                            if (_kindId.Contains(unsettled.KindID))
                            {
                                gameType = GameType.V8_Poker;
                            }
                            else
                            {
                                gameType = GameType.V8_Poker_None;
                            }

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.ValidBet,
                                Revenue = unsettled.Revenue,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.GameID,
                                GameType = gameType,
                                TpUserName = account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
                                SettlementType = SettlementType.SettledForValid,
                                GameID = unsettled.KindID.ToString(),
                            };
                            var result = _settledBLL.Settled_Without_Bonus(tpCustInfo, betRecordInfo);
                            _settledBLL.SetSettleFlag_TpWagerPK_V8(unsettled.GameID);
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            _logger.LogError($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", logName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                _logger.LogError(ex.ToString(), logName);
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

                timerSettleV8.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleV8(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            // 統一使用一款處理worker執行
            var gametype = (GameType)state;
            var bettransGet = "Service_BettransPK_V8_Settle_Get";
            var bettransSet = "Service_BettransPK_V8_Settle_Set";
            var wagerGet = "Service_TpWagerPK_V8_Settle_Get";
            var wagerSet = "Service_TpWagerPK_V8_Settle_Set_V2";
            string logName = $"Settle_V8";


            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gametype} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var unsettled in tpUnsettleList)
                {
                    try
                    {
                        string TransId = unsettled.TransId;

                        var bettrans = _partitionService.GetUnsettleDataList<SettleData>(bettransGet, unsettled);
                        if (!bettrans.Any())
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogError($"bettrans not found ({gametype})\r\n{JsonConvert.SerializeObject(unsettled)}\r\n", logName);
                            continue;
                        }

                        var wager = _tpUnSettledBLL.GetTpUnsettleData<SettleDataPK_V8>(wagerGet, new { TransId = TransId }, DataBase.Wager).FirstOrDefault();
                        if (wager == null)
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogError($"wager not found ({gametype})\r\n{JsonConvert.SerializeObject(unsettled)}\r\n", logName);
                            continue;
                        }

                        var tpUserName = bettrans.FirstOrDefault().Account;

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate != 0)
                        {
                            wager.Stake *= tpCurrency.ExchangeRate;
                            wager.ValidBet *= tpCurrency.ExchangeRate;
                            wager.Revenue *= tpCurrency.ExchangeRate;
                            wager.WinLostAmount *= tpCurrency.ExchangeRate;
                        }

                        if (_tools.Floor(wager.WinLostAmount, 2) != _tools.Floor(bettrans.Sum(x => x.WinLostAmount), 2))
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogError($"amount not match\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettled)}\r\n", logName);
                            continue;
                        }

                        // revenue games
                        var realGameType = gametype;
                        if (_kindId.Contains(wager.KindID))
                        {
                            realGameType = GameType.V8_Poker;
                        }
                        else
                        {
                            realGameType = GameType.V8_Poker_None;
                        }

                        var stake = wager.Stake;
                        var validBet = wager.ValidBet;
                        var winLostAmount = wager.WinLostAmount;
                        var revenue = wager.Revenue;
                        var gameId = wager.KindID.ToString();
                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = stake,
                            ValidBet = validBet,
                            Revenue = revenue,
                            WinlostAmount = winLostAmount,
                            TransId = TransId,
                            GameType = realGameType,
                            TpUserName = tpUserName,
                            SettledTime = wager.SettleTime,
                            TransTime = wager.TransTime,
                            SettlementType = SettlementType.SettledForValid,
                            GameID = gameId,
                        };

                        var result = _settledBLL.Settled_Without_Bonus(tpCustInfo, betRecordInfo);
                        if (result == 1)
                        {
                            SetSettleFlag(wagerSet, bettransSet, gametype, betRecordInfo);
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, unsettled.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"exMessage:{ex1.Message} || TransId : {unsettled.TransId}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", _tpName);
                _logger.LogError(ex.ToString(), logName);
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

                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void SetSettleFlag(string wagerSet, string bettransSet, GameType gametype, BetRecordInfo betRecordInfo)
        {
            _ = _tpUnSettledBLL.SetSettleFlag(wagerSet, Tp, gametype, betRecordInfo.TransId, true, DataBase.Wager);
            _ = _settledBLL.SetSettleFlagOfMonthlyBettrans(bettransSet, betRecordInfo);
        }
    }
}
