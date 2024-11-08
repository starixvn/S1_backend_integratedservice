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
    public class NextspinWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.Nextspin; }
        private Timer timerRNG;
        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<NextspinWorker> _logger;
        private readonly string _tpName;
        private readonly PartitionService _partitionService;


        public NextspinWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<NextspinWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerRNG = new Timer(DoSettleRng, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerRNG?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleRng(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = GameType.Nextspin_RNG;
            var tpCategory = TpCategory.Slots;

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype, 5000);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 1000;
                foreach (var tpUnsettle in tpUnsettleList)
                {
                    try
                    {
                        var unsettleData = _partitionService.GetUnsettleDataList<SettleDataRNG_Nextspin>("Service_BettransRng_Nextspin_Unsettle_Get", tpUnsettle).FirstOrDefault();
                        var tpWagerData = _tpUnSettledBLL.GetTpUnsettleData<SettleDataRNG_Nextspin>("Service_TpWagerRng_Nextspin_Unsettle_Get", new { tpUnsettle.TransId }, DataBase.Wager).FirstOrDefault();
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettleData.TpUserName);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCustInfo == null)
                        {
                            throw new UnsettleNotFoundException($"{unsettleData.TpUserName} TpCustInfo not found.");
                        }
                        if (!IsBettransSameAsTpWager(unsettleData, tpWagerData, tpCurrency))
                        {
                            _logger.LogWarning($"transId: {tpUnsettle.TransId} is not equal " +
                                             $"settle: {JsonConvert.SerializeObject(unsettleData)} " +
                                             $"wager: {JsonConvert.SerializeObject(tpWagerData)}");
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, isSuccess: false);
                            continue;
                        }
                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = unsettleData.Stake,
                            ValidBet = unsettleData.ValidBet,
                            WinlostAmount = unsettleData.WinLostAmount,
                            TransId = tpUnsettle.TransId,
                            GameType = gametype,
                            TpUserName = unsettleData.TpUserName,
                            SettledTime = unsettleData.SettledTime,
                            TransTime = unsettleData.TransTime,
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, unsettleData.GameId),
                            TicketStatus = CalculateTicketStatus(unsettleData.WinLostAmount),
                            GameID = unsettleData.GameId,
                            ReferenceNo = unsettleData.ReferenceId
                        };
                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, isSuccess: true);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"exMessage:{ex1.Message}");
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

                timerRNG?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private bool IsBettransSameAsTpWager(SettleDataRNG_Nextspin bettrans, SettleDataRNG_Nextspin tpWager, TpCurrency tpCurrency)
        {
            if (bettrans == null || tpWager == null || tpCurrency == null || tpCurrency.ExchangeRate <= 0) return false;
            tpWager.Stake *= tpCurrency.ExchangeRate;
            tpWager.WinLostAmount *= tpCurrency.ExchangeRate;

            var targetWinLost = tpWager.WinLostAmount;
            var targetStake = tpWager.Stake;

            var stake = bettrans.Stake;
            var winLost = bettrans.WinLostAmount;
            return stake == targetStake && winLost == targetWinLost;
        }

        private TicketStatus CalculateTicketStatus(decimal amount)
        {
            if (amount > 0)
                return TicketStatus.Win;
            else if (amount == 0)
                return TicketStatus.Draw;
            else
                return TicketStatus.Lost;
        }
    }
}
