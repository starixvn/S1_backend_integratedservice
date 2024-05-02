using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    internal class TFGWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.TFG; }
        private Timer timer;
        private readonly string _tpName;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly SettledBLL _settledBLL;
        private readonly ILogger<TFGWorker> _logger;
        public TFGWorker(IServiceProvider provider)
        {
            _tpName = Enum.GetName(typeof(Tp), Tp);
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _logger = provider.GetRequiredService<ILogger<TFGWorker>>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(SettleEsports, GameType.TFG_Esports, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
        private void SettleEsports(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            const string getBettransSp = "Service_BettransSports_TFG_Check"; // 取得下注紀錄
            const string getSettleSp = "Service_BettransSports_TFG_Settle_Get"; // 取得未結算的注單
            const string getWagerSp = "Service_TpWagerTFG_Get"; // 取得Wager資料

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, GameType.TFG_Esports);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {GameType.TFG_Esports} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;

                foreach (var tpUnsettle in tpUnsettleList)
                {
                    try
                    {
                        var betData = _tpUnSettledBLL.GetTpUnsettleData<Check_TFG>(getBettransSp, new { id = tpUnsettle.TransId }, DataBase.Main).FirstOrDefault();
                        var settleData = _tpUnSettledBLL.GetTpUnsettleData<TFGSettle>(getSettleSp, new { id = tpUnsettle.TransId }, DataBase.Main);
                        var wagerData = _tpUnSettledBLL.GetTpUnsettleData<WagerTFG>(getWagerSp, new { id = tpUnsettle.TransId }, DataBase.Wager).FirstOrDefault();
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, betData.loginName);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);

                        //未結算預設值
                        var winlostAmount = 0m;
                        var settlementType = SettlementType.UnSettled;
                        var ticketStatus = TicketStatus.UnSettled;
                        var transTime = betData.unix_date_created;
                        var settledTime = betData.unix_settlement_datetime;


                        //設定結算值
                        if (settleData.Any())
                        {
                            if (wagerData == null)
                                continue;

                            #region 匯率轉換
                            wagerData.amount *= tpCurrency.ExchangeRate;
                            wagerData.earnings *= tpCurrency.ExchangeRate;
                            #endregion

                            decimal? earnings = null;
                            earnings = wagerData.result_status switch
                            {
                                "WIN" => settleData.Sum(x => x.amount),
                                "LOSS" => -betData.amount,
                                "DRAW" => betData.amount,
                                "CANCELLED" => 0,
                                "PENDING " => null
                            };

                            winlostAmount = settleData.Sum(x => x.amount) - betData.amount;
                            settlementType = SettlementType.SettledForValid;
                            ticketStatus = GetTicketStatus(wagerData.result_status);
                            if (wagerData.result_status == "CANCELLED")
                            {
                                settlementType = SettlementType.SettledForInvalid;
                            }
                            transTime = wagerData.unix_date_created;
                            settledTime = wagerData.unix_settlement_datetime;
                            if (!(betData.amount == wagerData.amount && (wagerData.earnings == null || wagerData.earnings == earnings)))
                            {
                                LogContext.PushProperty("TP", _tpName);
                                _logger.LogError($"betData=\r\n{JsonConvert.SerializeObject(betData)}\r\nwagerData=\r\n{JsonConvert.SerializeObject(wagerData)}");
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.TFG_Esports, tpUnsettle.TransId, false);
                                });
                                continue;
                            }
                        }

                        if (betData.TicketStatus == (int)TicketStatus.Refund)
                        {
                            settlementType = SettlementType.SettledForInvalid;
                            ticketStatus = TicketStatus.Refund;
                        }

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = betData.amount,
                            ValidBet = betData.amount,
                            WinlostAmount = winlostAmount,
                            TransId = tpUnsettle.TransId,
                            GameType = GameType.TFG_Esports,
                            TpUserName = betData.loginName,
                            SettledTime = settledTime,
                            TransTime = transTime,
                            SettlementType = settlementType,
                            TicketStatus = ticketStatus,
                        };

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Esports);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.TFG_Esports, tpUnsettle.TransId, true);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.TFG_Esports, tpUnsettle.TransId, false);
                        });
                        LogContext.PushProperty("TP", _tpName);
                        _logger.LogError(ex, $"TFG Settle exception. {ex.Message} || TransId : {tpUnsettle.TransId}");
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
                _logger.LogError(ex, $"TFG Settle exception. {ex.Message}");
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromSeconds(30);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }

                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private TicketStatus GetTicketStatus(string resultStatus)
        {
            return resultStatus switch
            {
                "WIN" => TicketStatus.Win,
                "LOSS" => TicketStatus.Lost,
                "DRAW" => TicketStatus.Draw,
                "CANCELLED" => TicketStatus.Refund
            };
        }
    }
}
