using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class TCGWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.TCG; }

        private Timer timerSettleTCGSEA;
        private Timer timerSettleTCGVN;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<TCGWorker> _logger;
        private readonly string _tpName;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly MySqlClient _mainDbClient;


        public TCGWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<TCGWorker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerSettleTCGSEA = new Timer(DoSettleTCGWork, GameType.TCG_SEA_Lottery, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleTCGVN = new Timer(DoSettleTCGWork, GameType.TCG_VN_Lottery, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerSettleTCGSEA?.Change(Timeout.Infinite, 0);
            timerSettleTCGVN?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void DoSettleTCGWork(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            var gameType = (GameType)state;
            var tpCategory = TpCategory.Lottery;
            var dueTimeSpan = TimeSpan.FromMinutes(1);

            var type = gameType == GameType.TCG_SEA_Lottery ? "SEA" : "VN";
            var getSP = $"Service_TpWagerTCG_{type}_Unsettle_Get_V2";
            var setSP = $"Service_TpWagerTCG_{type}_Settle_Set_V2";
            var setCancelSP = $"Service_TpWagerTCG_{type}_Cancel_Get";
            
            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{gameType} no unsettle data.");
                }

                if (tpUnsettleList.Count() == 200)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(15);
                }


                foreach (var transId in tpUnsettleList.Select(tpUnsettle => tpUnsettle.TransId))
                {
                    try
                    {
                        var unsettled = _tpUnSettledBLL.GetTpUnsettleData<TCGSettleData>(getSP, new { TransId = transId }, DataBase.Wager).FirstOrDefault();
                        var isSettle = unsettled.betStatus != 0;

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettled.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        var referenceNo = string.Empty;

                        var settlementType = SettlementType.UnSettled;
                        decimal winLostAmount = 0;
                        var ticketStatus = TicketStatus.UnSettled;
                        
                        if (isSettle)
                        {
                            //cancel update success
                            if ((gameType == GameType.TCG_SEA_Lottery && unsettled.betStatus == 3)
                                || (gameType == GameType.TCG_VN_Lottery && unsettled.betStatus == 17))
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, unsettled.TransId, true);
                                continue;
                            }

                            //settle amount check
                            var checkData = _tpUnSettledBLL.GetTpUnsettleData<TCGCheckData>("Service_BettransLottery_TCG_Check",
                               new { TpUserName = unsettled.Account, unsettled.OrderNum }, DataBase.Main)?.FirstOrDefault();
                            if (checkData == null)
                            {
                                _logger.LogWarning($"TCG Bettrans empty || TransId : {transId}  OrderNum: {unsettled.OrderNum}");
                                continue;
                            }

                            var payout = Math.Round(checkData.Payout / tpCurrency.ExchangeRate, 2, MidpointRounding.AwayFromZero);
                            var stake = Math.Round(checkData.Stake / tpCurrency.ExchangeRate, 2, MidpointRounding.AwayFromZero);
                            if (unsettled.Stake != Math.Round(checkData.Stake / tpCurrency.ExchangeRate, 2, MidpointRounding.AwayFromZero)
                                || unsettled.WinLostAmount != (payout - stake))
                            {
                                _logger.LogWarning($"TCG settle amount check fail || TransId : {transId}  OrderNum: {unsettled.OrderNum}");
                                continue;
                            }
                            referenceNo = _mainDbClient.QueryFirst<string>("Service_BettransLottery_TCG_ReferenceNo_Get", new { TpUserName = unsettled.Account, unsettled.OrderNum });
                            
                            settlementType = SettlementType.SettledForValid;
                            winLostAmount = unsettled.WinLostAmount;
                            ticketStatus = winLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost;
                        }
                        else
                        {
                            var checkCancelData = _tpUnSettledBLL.GetTpUnsettleData<TCGCancelCheckData>("Service_BettransLottery_TCG_CancelCheck", 
                                new { TpUserName = unsettled.Account, unsettled.OrderNum }, DataBase.Main)?.FirstOrDefault();
                            
                            if (gameType == GameType.TCG_VN_Lottery)
                            {
                                var checkWagerCancelData = _tpUnSettledBLL.GetTpUnsettleData<TCGCancelWagerData>(setCancelSP, 
                                    new { TpUserName = unsettled.Account, unsettled.OrderNum }, DataBase.Wager)?.FirstOrDefault();
                                
                                if (checkWagerCancelData is not null && checkWagerCancelData.PlanBetAmount != unsettled.Stake)
                                {
                                    _logger.LogWarning($"TCG cancel stake check fail || TransId : {transId}  OrderNum: {unsettled.OrderNum}");
                                    continue;
                                }
                                
                                if (checkCancelData is not null &&
                                    checkWagerCancelData is not null &&
                                    checkCancelData.TicketType == (int) BettransType.CancelBet &&
                                    checkCancelData.TicketStatus == (int) TicketStatus.Refund)
                                {
                                    settlementType = SettlementType.SettledForInvalid;
                                    ticketStatus = TicketStatus.Refund;
                                    isSettle = true;
                                }
                            }
                            else
                            {
                                if (checkCancelData is not null &&
                                    checkCancelData.TicketType == (int) BettransType.CancelBet &&
                                    checkCancelData.TicketStatus == (int) TicketStatus.Refund)
                                {
                                    settlementType = SettlementType.SettledForInvalid;
                                    ticketStatus = TicketStatus.Refund;
                                    isSettle = true;
                                }
                            }
                        }

                        if (tpCurrency.ExchangeRate > 0)
                        {
                            unsettled.Stake *= tpCurrency.ExchangeRate;
                            unsettled.ValidBet *= tpCurrency.ExchangeRate;
                            unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                        }

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = unsettled.Stake,
                            ValidBet = unsettled.ValidBet,
                            WinlostAmount = winLostAmount,
                            TransId = unsettled.TransId,
                            GameType = gameType,
                            TpUserName = unsettled.Account,
                            SettledTime = unsettled.SettleTime,
                            TransTime = DateTimeOffset.FromUnixTimeSeconds(unsettled.TransTime).AddHours(-8).ToUnixTimeSeconds(),
                            SettlementType = settlementType,
                            GameID = unsettled.GameId,
                            ReferenceNo = referenceNo,
                            TicketStatus = ticketStatus
                        };
                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            if (isSettle)
                            {
                                _tpUnSettledBLL.SetSettleFlag(setSP,
                                new
                                {
                                    betOrderNo = unsettled.TransId,
                                    isSuccess = true
                                }, dataBase: DataBase.Wager);
                            }
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, transId, true);
                        }
                    }
                    catch (Exception ex1)
                    {
                        _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, transId, false);
                        _logger.LogError(ex1, $"exMessage:{ex1.Message} || TransId : {transId}");
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
            }
            finally
            {
                _ = gameType == GameType.TCG_SEA_Lottery ?
                timerSettleTCGSEA?.Change(dueTimeSpan, TimeSpan.Zero) :
                timerSettleTCGVN?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }

    public class TCGCheckData
    {
        public decimal Stake { get; set; }
        public decimal Payout { get; set; }
    }
    
    public class TCGCancelCheckData
    {
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
    }
    
    public class TCGCancelWagerData
    {
        public string OrderNum { get; set; }
        public string Username { get; set; }
        public decimal PlanBetAmount { get; set; }
        public int CancelType { get; set; }
    }
}
