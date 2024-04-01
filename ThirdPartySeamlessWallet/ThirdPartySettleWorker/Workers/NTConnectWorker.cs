using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class NTConnectWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.NTConnect;
        
        private Timer _timer;
        
        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly PartitionService _partitionService;
        private readonly ILogger<NTConnectWorker> _logger;
        private readonly string _tpName;
        
        public NTConnectWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _logger = provider.GetRequiredService<ILogger<NTConnectWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoNTConnectSettle, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoNTConnectSettle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gameType = GameType.NTConnect_Games;
            var tpCategory = TpCategory.Games;
            var logName = $"Settle_NTConnect";
            var getSP = $"Service_Bettrans_93Connect_Settle_Get";
            var setSP = $"Service_Bettrans_93Connect_Settle_Set";
            var dueTimeSpan = TimeSpan.FromMinutes(1);
            
            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                //forContinue
                forContinue = tpUnsettleList.Count() == 200;

                foreach (var wager in tpUnsettleList)
                {
                    var getUnsettledList = _partitionService.GetUnsettleDataList<SettleData93Connect>(getSP, wager);
                    if(getUnsettledList == null || !getUnsettledList.Any())
                    {
                        throw new UnsettleNotFoundException($"{_tpName} - NTConnect Can fount TransId : {wager.TransId}.");
                    }
                    try
                    {
                        
                        var unsettled = getUnsettledList.FirstOrDefault(x => x.TransId == wager.TransId);
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettled.Account);
                        
                        // 因為93Connect Wager API抓回來的VND跟IDR抓回來的金額已經是加成過的
                        // 就不用特意在處理 若後續有變更在把註解拿掉。
                        // var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        // if (tpCurrency.ExchangeRate > 1)
                        // {
                        //     unsettled.Stake *= tpCurrency.ExchangeRate;
                        //     unsettled.ValidBet *= tpCurrency.ExchangeRate;
                        //     unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                        // }

                        var transType = unsettled.Status switch
                        {
                            "C" => SettlementType.SettledForInvalid,
                            "R" => SettlementType.UnSettled,
                            _ => SettlementType.SettledForValid
                        };
                        
                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = unsettled.Stake,
                            TpUserName = unsettled.Account,
                            ValidBet = unsettled.ValidBet,
                            WinlostAmount = unsettled.WinLostAmount,
                            TransId = unsettled.TransId,
                            GameType = gameType,
                            SettledTime = unsettled.SettleTime,
                            TransTime = unsettled.TransTime,
                            SettlementType = transType,
                            GameID = unsettled.GameId,
                        };

                        if (betRecordInfo.SettlementType == SettlementType.SettledForInvalid)
                        {
                            betRecordInfo.TicketStatus = TicketStatus.Refund;
                        }

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                                
                            Task.Run(() =>
                            {
                                _ = _settledBLL.SetSettleFlagOfMonthlyBettrans(setSP, betRecordInfo);
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: true);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        isError = true;
                        _logger.LogError(ex1, $"{logName} exMessage:{ex1.Message} || TransId : {wager.TransId}");
                    }
                }
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }
            catch (Exception ex)
            {
                isError = true;
                _logger.LogError(ex, $"{logName} exMessage:{ex.Message}");
            }
            finally
            {
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(10);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }
                _timer?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        
        #region Model
        
        private class SettleData93Connect : SettleData
        {
            public string Status { get; set; }
        }
        
        public class NTConnectWager
        {
            public string ticket { get; set; }
            public string player { get; set; }
            public DateTime bet_date { get; set; }
            public string gamecode { get; set; }
            public string game { get; set; }
            public string drawno { get; set; }
            public decimal bet_amount { get; set; }
            public decimal bet_odd { get; set; }
            public string bet_typegroup { get; set; }
            public string bet_type { get; set; }
            public decimal winlosso { get; set; }
            public string status { get; set; }
            public DateTime LocalBetTime { get; set; }
            public DateTime CreateTime { get; set; }
        }
        
        #endregion

    }
}