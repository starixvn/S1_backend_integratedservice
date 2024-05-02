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
    public class SimplePlayWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.SimplePlay; }

        private Timer timerRNG;
        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly PartitionService _partitionService;
        private readonly ILogger<SimplePlayWorker> _logger;
        private readonly string _tpName;

        public SimplePlayWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<SimplePlayWorker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerRNG = new Timer(SettleRNG, GameType.SimplePlay_RNG, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerRNG?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void SettleRNG(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = (GameType)state;
            var tpCategory = TpCategory.Slots;
            var bettransGet = "Service_BettransRng_SimplePlay_Settle_Get";
            var wagerGet = "Service_TpWagerRng_SimplePlay_Settle_Get";
            var wagerSet = "Service_TpWagerRng_SimplePlay_Settle_Set";
            string logName = "Settle_SimplePlay_Rng";

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
                        var split = TransId.Split("_");
                        var gameid = split[0];
                        var username = split[1];

                        var bettrans = _partitionService.GetUnsettleDataList<SettleData_SimplePlay>(bettransGet, unsettled, 8);
                        var wager = _tpUnSettledBLL.GetTpUnsettleData<SettleData_SimplePlay>(wagerGet, new { GameId = gameid, Username = username }, DataBase.Wager).First();
                        var tpUserName = bettrans.FirstOrDefault().Account;

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate != 0)
                        {
                            wager.Stake *= tpCurrency.ExchangeRate;
                            wager.ValidBet *= tpCurrency.ExchangeRate;
                            wager.WinLostAmount *= tpCurrency.ExchangeRate;
                            wager.ProgressiveBet *= tpCurrency.ExchangeRate;
                            wager.ProgressiveWin *= tpCurrency.ExchangeRate;
                        }

                        if (wager.Stake != bettrans.Sum(x => x.Stake) || wager.WinLostAmount != bettrans.Sum(x => x.WinLostAmount))
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName} amount not match\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }

                        var stake = wager.Stake;
                        var validBet = wager.ValidBet;
                        var winLostAmount = wager.WinLostAmount;
                        var gameId = wager.GameCode.ToString();
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
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, gameId),
                            ProgressiveBet = wager.ProgressiveBet,
                            ProgressiveWin = wager.ProgressiveWin,
                            GameID = gameId,
                        };

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetSettleFlag(wagerSet, Tp, gametype, TransId, true, DataBase.Wager);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, unsettled.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"{logName} exMessage:{ex1.Message} || TransId : {unsettled.TransId}");
                    }
                }
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{logName}:{ex.Message}");
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

                timerRNG?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
