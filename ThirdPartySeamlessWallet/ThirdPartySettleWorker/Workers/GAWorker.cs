using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class GAWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.GA; }
        private Timer timer;
        private readonly string _tpName;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly SettledBLL _settledBLL;
        private readonly PartitionService _partitionService;
        private readonly ILogger<GAWorker> _logger;
        public GAWorker(IServiceProvider provider)
        {
            _tpName = Enum.GetName(typeof(Tp), Tp);
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _logger = provider.GetRequiredService<ILogger<GAWorker>>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(Settle, GameType.GA_CockFighting, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        private void Settle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            const string getBettransSp = "Service_Bettrans_GA_Get"; // 取得下注紀錄
            const string getJackpotSp = "Service_Bettrans_GA_Jackpot_Get"; // 取得jackpot紀錄
            const string getWagerSp = "Service_TpWager_GA_Get"; // 取得Wager資料

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, GameType.GA_CockFighting);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {GameType.GA_CockFighting} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;

                foreach (var tpUnsettle in tpUnsettleList)
                {
                    try
                    {
                        var betData = _partitionService.GetUnsettleDataList<SettleDataGA>(getBettransSp, tpUnsettle).ToList();
                        string tpUserName = string.Empty;

                        var betRecordInfo = new BetRecordInfo();

                        if (!betData.Any())
                        {
                            var jackpotData = _partitionService.GetUnsettleDataList<SettleDataGA>(getJackpotSp, tpUnsettle).FirstOrDefault();
                            if (jackpotData == null)
                            {
                                _logger.LogWarning($"{tpUnsettle.TransId} betData is null");
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.GA_CockFighting, tpUnsettle.TransId, false);
                                });
                                continue;
                            }
                            tpUserName = jackpotData.member_ref_id;

                            betRecordInfo.TpId = (int)Tp;
                            betRecordInfo.Stake = 0;
                            betRecordInfo.ValidBet = 0;
                            betRecordInfo.WinlostAmount = 0;
                            betRecordInfo.TransId = tpUnsettle.TransId;
                            betRecordInfo.GameType = GameType.GA_CockFighting;
                            betRecordInfo.TpUserName = tpUserName;
                            betRecordInfo.SettledTime = jackpotData.unix_settled_date_time;
                            betRecordInfo.TransTime = jackpotData.unix_created_date_time;
                            betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                            betRecordInfo.TicketStatus = TicketStatus.FreeBetWin;
                            betRecordInfo.ProgressiveWin = jackpotData.amount;
                        }
                        else
                        {
                            var wagerData = _tpUnSettledBLL.GetTpUnsettleData<GAWager>(getWagerSp, new { sid = tpUnsettle.TransId }, DataBase.Wager).ToList().FirstOrDefault();
                            if (wagerData == null)
                            {
                                _logger.LogWarning($"{tpUnsettle.TransId} wagerData is null");
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.GA_CockFighting, tpUnsettle.TransId, false);
                                });
                                continue;
                            }

                            tpUserName = betData.FirstOrDefault().member_ref_id;
                            decimal betDataBet = 0;
                            decimal betDataWin = 0;

                            var GoSettle = wagerData.status switch
                            {
                                GAStatus.TicketIsSettled => TicketIsSettled(),
                                GAStatus.TicketIsVoided => TicketIsVoided(),
                                _ => false
                            };

                            if (!GoSettle)
                                continue;

                            bool TicketIsSettled()
                            {
                                betDataBet = betData.Find(x => x.type == (int)GAType.Bet).bet_amount;
                                decimal anteAmount = betData.Find(x => x.type == (int)GAType.Bet).ante_amount;
                                var payoutData = betData.Where(x => (x.type == (int)GAType.TicketWinnings) || (x.type == (int)GAType.TicketLoss) || (x.type == (int)GAType.TicketDraw)).OrderByDescending(x => x.unix_settled_date_time).FirstOrDefault();
                                betDataWin = payoutData.amount;

                                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);
                                var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                                wagerData.win_loss_amount *= tpCurrency.ExchangeRate;

                                if (payoutData.win_loss_amount != wagerData.win_loss_amount)
                                {
                                    _logger.LogWarning($"{tpUnsettle.TransId} win_loss_amount is not true payoutData:{payoutData.win_loss_amount}, wagerData:{wagerData.win_loss_amount}");
                                    return false;
                                }
                                else
                                {
                                    betRecordInfo.TpId = (int)Tp;
                                    betRecordInfo.Stake = betDataBet;
                                    betRecordInfo.ValidBet = betDataBet - anteAmount;
                                    betRecordInfo.WinlostAmount = betDataWin - betDataBet;
                                    betRecordInfo.TransId = tpUnsettle.TransId;
                                    betRecordInfo.GameType = GameType.GA_CockFighting;
                                    betRecordInfo.TpUserName = tpUserName;
                                    betRecordInfo.SettledTime = payoutData.unix_settled_date_time;
                                    betRecordInfo.TransTime = payoutData.unix_created_date_time;
                                    betRecordInfo.SettlementType = SettlementType.SettledForValid;
                                    betRecordInfo.TicketStatus = payoutData.type == (int)GAType.TicketWinnings ? TicketStatus.Win : payoutData.type == (int)GAType.TicketDraw ? TicketStatus.Draw : TicketStatus.Lost;
                                    betRecordInfo.ProgressiveBet = anteAmount;

                                    if (betRecordInfo.TicketStatus == TicketStatus.Draw && string.Equals(wagerData.result, "draw", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // 所有没有任何输赢的投注将不被计算在Valid Stake
                                        betRecordInfo.ValidBet = 0;
                                        betRecordInfo.ProgressiveBet = 0;
                                    }

                                    return true;
                                }
                            }

                            bool TicketIsVoided()
                            {
                                var voidData = betData.FirstOrDefault(x => x.type == (int)GAType.Void);

                                betDataBet = betData.Find(x => x.type == (int)GAType.Bet).bet_amount;

                                betRecordInfo.TpId = (int)Tp;
                                betRecordInfo.Stake = betDataBet;
                                betRecordInfo.ValidBet = 0;
                                betRecordInfo.WinlostAmount = 0;
                                betRecordInfo.GameType = GameType.GA_CockFighting;
                                betRecordInfo.TpUserName = tpUserName;
                                betRecordInfo.SettledTime = voidData.unix_settled_date_time;
                                betRecordInfo.TransTime = voidData.unix_created_date_time;
                                betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                                betRecordInfo.TicketStatus = TicketStatus.Refund;
                                return true;
                            }
                        }

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Cockfighting);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.GA_CockFighting, tpUnsettle.TransId, true);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.GA_CockFighting, tpUnsettle.TransId, false);
                        });
                        LogContext.PushProperty("TP", _tpName);
                        _logger.LogError(ex, $"GA Settle exception. {ex.Message} || TransId : {tpUnsettle.TransId}");
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
                _logger.LogError(ex, $"GA Settle exception. {ex.Message}");
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }

                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
