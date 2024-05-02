using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class PNGWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.PNG;
        private Timer RngTimer;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<PNGWorker> _logger;
        private readonly string _tpName;

        public PNGWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _logger = provider.GetRequiredService<ILogger<PNGWorker>>();

            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            RngTimer = new Timer(DoRngSettle, GameType.PNG_RNG, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            RngTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoRngSettle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            const GameType gameType = GameType.PNG_RNG;
            const TpCategory tpCategory = TpCategory.Slots;

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType).ToList();
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var tpUnsettle in tpUnsettleList)
                {
                    try
                    {
                        var unsettleData = _tpUnSettledBLL.GetTpUnsettleData<SettleDataRNG_PNG>("Service_BettransRng_PNG_Unsettle_Get", tpUnsettle, DataBase.Main)
                            .FirstOrDefault();

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettleData.TpUserName);
                        CheckTpCustInfoExist(tpCustInfo, unsettleData);
                        CheckUnSettleDataExist(unsettleData, tpUnsettle);

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = unsettleData.Stake,
                            ValidBet = unsettleData.ValidBet,
                            WinlostAmount = unsettleData.WinLostAmount,
                            TransId = tpUnsettle.TransId,
                            GameType = gameType,
                            TpUserName = unsettleData.TpUserName,
                            SettledTime = unsettleData.SettledTime,
                            TransTime = unsettleData.TransTime,
                            ProgressiveBet = unsettleData.ProgressiveBet,
                            ProgressiveWin = unsettleData.ProgressiveWin,
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, unsettleData.GameId),
                            TicketStatus = MapTicketStatus(unsettleData.WinLostAmount),
                            GameID = unsettleData.GameId
                        };

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, isSuccess: true);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, isSuccess: false);
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
                var dueTimeSpan = TimeSpan.FromMinutes(1);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                RngTimer?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private static void CheckTpCustInfoExist(TpCustInfo tpCustInfo, SettleDataRNG_PNG unsettleData)
        {
            if (tpCustInfo == null)
            {
                throw new UnsettleNotFoundException($"{unsettleData.TpUserName} TpCustInfo not found.");
            }
        }

        private static void CheckUnSettleDataExist(SettleDataRNG_PNG unsettleData, TpUnsettle tpUnsettle)
        {
            if (unsettleData == null)
            {
                throw new UnsettleNotFoundException($"{tpUnsettle.TransId} unsettleData not found.");
            }
        }

        private static TicketStatus MapTicketStatus(decimal amount)
        {
            return amount switch
            {
                > 0 => TicketStatus.Win,
                0 => TicketStatus.Draw,
                _ => TicketStatus.Lost
            };
        }
    }
}
