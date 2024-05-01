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
    internal class PlayTechWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.PlayTech; }

        private Timer _timerRNG;
        private Timer _timerLC;
        private readonly Tools _tools;
        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly PartitionService _partitionService;
        private readonly ILogger<PlayTechWorker> _logger;
        private readonly string _tpName;
        private readonly int _partitionCheckDay = 15;

        public PlayTechWorker(IServiceProvider provider)
        {
            _tools = provider.GetRequiredService<Tools>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<PlayTechWorker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerRNG = new Timer(DoSettlePlayTech, GameType.PlayTech_RNG, TimeSpan.Zero, TimeSpan.Zero);
            _timerLC = new Timer(DoSettlePlayTech, GameType.PlayTech_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerRNG?.Change(Timeout.Infinite, 0);
            _timerLC?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettlePlayTech(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = (GameType)state;
            var tpCategory = (gametype == GameType.PlayTech_RNG) ? TpCategory.Slots : TpCategory.LiveCasino;
            var cate = (gametype == GameType.PlayTech_RNG) ? "Rng" : "LC";
            // Service_BettransRng_PlayTech_Settle_Get、Service_BettransLC_PlayTech_Settle_Get
            var bettransGet = $"Service_Bettrans{cate}_PlayTech_Settle_Get";
            // Service_BettransRng_PlayTech_Settle_Set、Service_BettransLC_PlayTech_Settle_Set
            var bettransSet = $"Service_Bettrans{cate}_PlayTech_Settle_Set";
            var wagerGet = "Service_TpWager_PlayTech_Settle_Get";
            var wagerSet = "Service_TpWager_PlayTech_Settle_Set";
            // Settle_PlayTech_Rng、Settle_PlayTech_LC
            string logName = $"Settle_PlayTech_{cate}";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype, 5000);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gametype} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 5000;
                foreach (var unsettled in tpUnsettleList)
                {
                    try
                    {
                        string TransId = unsettled.TransId;

                        var bettrans = _partitionService.GetUnsettleDataList<SettleData>(bettransGet, unsettled, _partitionCheckDay);
                        if (!bettrans.Any())
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName}:bettrans not found ({gametype})\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }
                        var wager = _tpUnSettledBLL.GetTpUnsettleData<SettleData_PlayTech>(wagerGet, new { TransId = TransId }, DataBase.Wager).FirstOrDefault();
                        if (wager == null)
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName}:wager not found ({gametype})\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }
                        var tpUserName = bettrans.FirstOrDefault().Account;

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate != 0)
                        {
                            wager.Stake *= tpCurrency.ExchangeRate;
                            wager.ValidBet *= tpCurrency.ExchangeRate;
                            wager.CancelBet *= tpCurrency.ExchangeRate;
                            wager.WinLostAmount *= tpCurrency.ExchangeRate;
                            wager.ProgressiveBet *= tpCurrency.ExchangeRate;
                            wager.ProgressiveWin *= tpCurrency.ExchangeRate;
                        }

                        if (wager.Stake != bettrans.Sum(x => x.Stake) || wager.WinLostAmount != bettrans.Sum(x => x.WinLostAmount))
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName}:amount not match\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }

                        var stake = wager.Stake;
                        var validBet = wager.ValidBet;
                        var winLostAmount = wager.WinLostAmount;
                        var gameId = wager.GameId.ToString();

                        /*
                         * 對於 BRL 貨幣
                         * 必須使用 PTN 遊戲代碼才能存取遊戲
                         * 測試環境和正式環境使用的遊戲程式碼不同
                         */
                        if (tpCurrency.TpCurrencyId == "BRL" && gameId.StartsWith("ptn_", StringComparison.OrdinalIgnoreCase))
                        {
                            // 去除ptn_，讓rebate計算可以使用SlotGame原資料設定
                            gameId = gameId.Substring(4);
                        }

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = stake,
                            ValidBet = validBet,
                            WinlostAmount = winLostAmount,
                            TransId = TransId,
                            GameType = gametype,
                            TpUserName = tpUserName,
                            SettledTime = wager.SettleTime,
                            TransTime = wager.TransTime,
                            SettlementType = gametype == GameType.PlayTech_RNG ? _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, gameId) : SettlementType.SettledForValid,
                            ProgressiveBet = wager.ProgressiveBet,
                            ProgressiveWin = wager.ProgressiveWin,
                            GameID = gameId,
                        };

                        if (wager.Stake == 0 && wager.ValidBet == 0 && wager.CancelBet != 0)
                        {
                            // 因無法查看下注細單，故不結算至TpBetRecord
                            SetSettleFlag(wagerSet, bettransSet, gametype, betRecordInfo);
                            continue;
                        }

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
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
                TimeSpan dueTimeSpan = TimeSpan.FromSeconds(30);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                switch (gametype)
                {
                    case GameType.PlayTech_RNG:
                        _timerRNG?.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    case GameType.PlayTech_LiveCasino:
                        _timerLC?.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void SetSettleFlag(string wagerSet, string bettransSet, GameType gametype, BetRecordInfo betRecordInfo)
        {
            _ = _tpUnSettledBLL.SetSettleFlag(wagerSet, Tp, gametype, betRecordInfo.TransId, true, DataBase.Wager);
            _ = _settledBLL.SetSettleFlagOfMonthlyBettrans(bettransSet, betRecordInfo);
        }
    }
}
