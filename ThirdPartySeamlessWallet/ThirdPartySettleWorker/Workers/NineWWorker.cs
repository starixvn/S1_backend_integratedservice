using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class NineWWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.NineW;
        private Timer _timerSports;
        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<NineWWorker> _logger;
        private readonly string _tpName;
        private readonly TpUnSettledBLL _tpUnSettledBLL;

        public NineWWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<NineWWorker>>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerSports = new Timer(DoSettle, GameType.NineW_Sports, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerSports?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettle(object state)
        {
            var gameType = (GameType)state;
            var dueTimeSpan = TimeSpan.FromMinutes(1);

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                _logger.LogWarning($"GetTpUnsettleList Count: {tpUnsettleList.Count()}");

                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{gameType} no unsettle data.");
                }

                if (tpUnsettleList.Count() == 200)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }

                foreach (var transId in tpUnsettleList.Select(tpUnsettle => tpUnsettle.TransId))
                {
                    var spName = "Service_TpWager9W_Settle_Set";
                    var unsettled = _tpUnSettledBLL.GetTpUnsettleData<NineWSettleData>("Service_TpWager9W_Unsettle_Get_V2", new { TransId = transId }, DataBase.Wager).FirstOrDefault();

                    try
                    {
                        if (unsettled == null)
                        {
                            continue;
                        }

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettled.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate > 0)
                        {
                            unsettled.Stake *= tpCurrency.ExchangeRate;
                            unsettled.ValidBet *= tpCurrency.ExchangeRate;
                            unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                        }

                        var isSettled = unsettled.BetStatus.Equals("Settled", StringComparison.OrdinalIgnoreCase)
                            || unsettled.BetStatus.Equals("Voided", StringComparison.OrdinalIgnoreCase);
                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = unsettled.Stake,
                            ValidBet = unsettled.ValidBet,
                            WinlostAmount = isSettled ? unsettled.WinLostAmount : 0,
                            TransId = unsettled.TransId,
                            GameType = gameType,
                            TpUserName = unsettled.Account,
                            SettledTime = unsettled.SettleTime,
                            TransTime = unsettled.TransTime,
                            SettlementType = isSettled ? SettlementType.SettledForValid : SettlementType.UnSettled,
                            GameID = unsettled.GameId,
                        };

                        if (isSettled && unsettled.BetStatus.Equals("Voided", StringComparison.OrdinalIgnoreCase))
                        {
                            betRecordInfo.TicketStatus = TicketStatus.Refund;
                            betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                        }

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Sports);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetSettleFlag(spName, unsettled.TransId, dataBase: DataBase.Wager);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetSettleFlag(spName, unsettled.TransId, isSuccess: false, DataBase.Wager);
                        });
                        LogContext.PushProperty("TP", _tpName);
                        _logger.LogError(ex, $"{JsonConvert.SerializeObject(unsettled)}");
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
                _logger.LogError(ex, $"NineW Settle exception.");
            }
            finally
            {
                _timerSports.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }

    public class NineWSettleData : SettleData
    {
        public string BetStatus { get; set; }
    }
}
