using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.KY;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class KYWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.KY;
        private Timer timer;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<KYWorker> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly PartitionService _partitionService;
        private readonly Tools _tools;
        private readonly string _tpName;
        // TODO: check which kind IDs and skiped kind ID are contained
        private readonly int[] _kindId =
        {
            220, 600, 610, 620, 630,
            830, 860, 890, 930
        };
        private readonly int[] _skipKindId =
        {
            510
        };

        public KYWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<KYWorker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _tools = provider.GetRequiredService<Tools>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 統一使用一款執行，後續再依退水遊戲區分
            timer = new Timer(DoSettleKY, GameType.KY_Poker, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleKY(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            // 統一使用一款處理worker執行
            var gametype = (GameType)state;

            var bettransGet = "Service_BettransPK_KY_Settle_Get";
            var bettransSet = "Service_BettransPK_KY_Settle_Set";
            var wagerGet = "Service_TpWagerPK_KY_Settle_Get";
            var wagerSet = "Service_TpWagerPK_KY_Settle_Set";
            string logName = $"Settle_KY";

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
                    var skip = false;
                    try
                    {
                        string TransId = unsettled.TransId;

                        var wager = _tpUnSettledBLL.GetTpUnsettleData<SettleDataPK_KY>(wagerGet, new { TransId = TransId }, DataBase.Wager).FirstOrDefault();
                        if (wager == null)
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName}:wager not found ({gametype})\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }
                        if (_skipKindId.Contains(wager.KindID))
                        {
                            // TODO: 排除捕魚不比對
                            skip = true;
                        }

                        var bettrans = _partitionService.GetUnsettleDataList<SettleData>(bettransGet, unsettled);
                        if (!bettrans.Any() && !skip)
                        {
                            LogContext.PushProperty("TP", _tpName);
                            if (wager != null && wager.Stake == 0m && wager.ValidBet == 0m && wager.WinLostAmount == 0m)
                            {
                                _ = _tpUnSettledBLL.SetSettleFlag(wagerSet, Tp, gametype, TransId, true, DataBase.Wager);
                                _logger.LogInformation($"{logName}:wager amount 0 no need settle\r\n{JsonConvert.SerializeObject(wager)}");
                            }
                            else
                            {
                                _logger.LogInformation($"{logName}:bettrans not found ({gametype})\r\n{JsonConvert.SerializeObject(unsettled)}");
                            }
                            continue;
                        }

                        var tpUserName = (skip) ? wager.Account.Split("_")[1] : bettrans.FirstOrDefault().Account;
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate != 0)
                        {
                            wager.Stake *= tpCurrency.ExchangeRate;
                            wager.ValidBet *= tpCurrency.ExchangeRate;
                            wager.Revenue *= tpCurrency.ExchangeRate;
                            wager.WinLostAmount *= tpCurrency.ExchangeRate;
                        }

                        if (!CheckAmount(bettrans, wager, skip))
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName}:amount not match\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }

                        // TODO: revenue games
                        var realGameType = gametype;
                        if (_kindId.Contains(wager.KindID))
                        {
                            realGameType = GameType.KY_Poker;
                        }
                        else
                        {
                            realGameType = GameType.KY_Poker_None;
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
                    catch (Exception ex)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, unsettled.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex, $"exMessage:{ex.Message} || TransId : {unsettled.TransId}");
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
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }

                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        // TODO: When does the similar(duplicated with V8) function will be shared from other namespace or other class?
        private void SetSettleFlag(string wagerSet, string bettransSet, GameType gametype, BetRecordInfo betRecordInfo)
        {
            _ = _tpUnSettledBLL.SetSettleFlag(wagerSet, Tp, gametype, betRecordInfo.TransId, true, DataBase.Wager);
            _ = _settledBLL.SetSettleFlagOfMonthlyBettrans(bettransSet, betRecordInfo);
        }

        // TODO: When does the similar(duplicated with V8) function will be shared from other namespace or other class?
        private bool CheckAmount(IEnumerable<SettleData> bettrans, SettleDataPK_KY wager, bool skip)
        {
            if (skip)
            {
                return true;
            }
            var dWagerWinLost = _tools.Floor(wager.WinLostAmount, 2);
            var dBettransWinLost = _tools.Floor(bettrans.Sum(x => x.WinLostAmount), 2);
            var dDiffWinLost = Math.Abs(dWagerWinLost - dBettransWinLost);
            return dDiffWinLost <= 0.5m;
        }
    }
}
