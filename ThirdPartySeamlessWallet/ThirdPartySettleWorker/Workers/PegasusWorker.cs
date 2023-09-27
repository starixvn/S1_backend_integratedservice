using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class PegasusWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.Pegasus;
        private Timer _timerRng;
        private readonly SettledBLL _settledBll;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly TpUnSettledBLL _tpUnSettledBll;
        private readonly PartitionService _partitionService;
        private readonly IPegasusBLL _pegasusBll;
        private readonly ILogger<PegasusWorker> _logger;
        private readonly string _tpName;

        public PegasusWorker(IServiceProvider provider)
        {
            _settledBll = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBll = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
            _tpUnSettledBll = provider.GetRequiredService<TpUnSettledBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _pegasusBll = provider.GetRequiredService<IPegasusBLL>();
            _logger = provider.GetRequiredService<ILogger<PegasusWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerRng = new Timer(DoSettleRngWork, GameType.Pegasus_RNG, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerRng?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleRngWork(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp;
            var gameType = (GameType)state;
            var category = TpCategory.Slots;
            var betTransGet = "Service_BettransRng_Pegesus_Settle_Get";
            var tpWagerGet = "Service_TpWagerRng_Pegasus_Unsettle_Get";
            var tpWagerSet = "Service_TpWagerRng_Pegasus_Settle_Set";
            var logName = "PegasusDoSettleRngWork";

            try
            {
                var tpUnsettleList = _tpUnSettledBll.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gameType} Round no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var unsettled in tpUnsettleList)
                {
                    try
                    {
                        var settleTrans = _partitionService.GetUnsettleDataList<SettleTransaction_Pegasus>(betTransGet, unsettled).FirstOrDefault();
                        if (settleTrans is null)
                        {
                            continue;
                        }
                        
                        var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache(thisTp, settleTrans.PlayerUniqueID);
                        var tpCurrency = _thirdPartyBll.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency == null)
                        {
                            _logger.LogError($"{logName} {settleTrans.PlayerUniqueID} TpCurrency not found.");
                            continue;
                        }

                        var tpWagerData = _tpUnSettledBll.GetTpUnsettleData<WagerPegasus>(tpWagerGet, new { settleTrans.TransactionNo }, DataBase.Wager).FirstOrDefault();
                        if (tpWagerData == null) continue;
                        
                        if (tpWagerData.BetAmount == settleTrans.BetAmount && tpWagerData.PayoutAmount == settleTrans.PayoutAmount)
                        {
                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = settleTrans.BetAmount,
                                ValidBet = settleTrans.ValidAmount,
                                WinlostAmount = settleTrans.PayoutAmount,
                                TransId = settleTrans.TransactionNo,
                                GameType = gameType,
                                TpUserName = settleTrans.PlayerUniqueID,
                                SettledTime = new DateTimeOffset(settleTrans.SettlementTime).ToUnixTimeSeconds(),
                                TransTime = new DateTimeOffset(settleTrans.CreateTime).ToUnixTimeSeconds(),
                                SettlementType = _thirdPartyBll.GetGameCasinoSettlementType((int)Tp, settleTrans.GameID),
                                GameID = settleTrans.GameID
                            };
                            
                            var result = _settledBll.Settled(tpCustInfo, betRecordInfo, category);
                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBll.SetTpUnsettle(Tp, gameType, settleTrans.TransactionNo, isSuccess: true);
                                    _ = _pegasusBll.SetSettleFlag_TpWager_Pegasus(tpWagerSet, settleTrans.TransactionNo);
                                });
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
                _logger.LogError(ex.ToString(), logName);
                isError = true;
            }
            finally
            {
                var dueTimeSpan = GetDueTimeSpan(forContinue, isError);
                _timerRng.Change(dueTimeSpan, TimeSpan.Zero);
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
