using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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


        public TCGWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<TCGWorker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
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
            var forContinue = false;
            var isError = false;

            var gametype = (GameType)state;
            var tpCategory = TpCategory.Lottery;

            var type = gametype == GameType.TCG_SEA_Lottery ? "SEA" : "VN";
            var unsettleSP = $"Service_TpWagerTCG_{type}_Unsettle_Get";
            var setSP = $"Service_TpWagerTCG_{type}_Settle_Set";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettledList<TCGSettleData>(unsettleSP, DataBase.Wager);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var unsettled in tpUnsettleList)
                {
                    try
                    {
                        //cancel update success
                        if ((gametype == GameType.TCG_SEA_Lottery && unsettled.betStatus == 3)
                            || (gametype == GameType.TCG_VN_Lottery && unsettled.betStatus == 17))
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, unsettled.TransId, true);
                        }

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettled.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate > 0)
                        {
                            unsettled.Stake *= tpCurrency.ExchangeRate;
                            unsettled.ValidBet *= tpCurrency.ExchangeRate;
                            unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                        }

                        //settle amount check
                        var  chcekData = _tpUnSettledBLL.GetTpUnsettleData<TCGCheckData>("Service_BettransLottery_TCG_Check",
                           new { TpUserName = unsettled.Account, unsettled.OrderNum }, DataBase.Main)?.FirstOrDefault();
                        if (chcekData == null 
                            || chcekData.Stake != unsettled.Stake 
                            || unsettled.WinLostAmount != (chcekData.Payout - chcekData.Stake))
                        {
                            continue;
                        }

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = unsettled.Stake,
                            ValidBet = unsettled.ValidBet,
                            WinlostAmount = unsettled.WinLostAmount,
                            TransId = unsettled.TransId,
                            GameType = gametype,
                            TpUserName = unsettled.Account,
                            SettledTime = unsettled.SettleTime,
                            TransTime = DateTimeOffset.FromUnixTimeSeconds(unsettled.TransTime).AddHours(-8).ToUnixTimeSeconds(),
                            SettlementType = SettlementType.SettledForValid,
                            TicketStatus = unsettled.WinLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost
                        };
                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            _tpUnSettledBLL.SetSettleFlag(setSP,
                            new
                            {
                                betOrderNo = unsettled.TransId,
                                orderNum = unsettled.OrderNum,
                                username = unsettled.Account,
                                isSuccess = true
                            }, dataBase: DataBase.Wager);
                        }
                    }
                    catch (Exception ex1)
                    {
                        _tpUnSettledBLL.SetSettleFlag(setSP,
                        new
                        {
                            betOrderNo = unsettled.TransId,
                            orderNum = unsettled.OrderNum,
                            username = unsettled.Account,
                            isSuccess = false
                        }, dataBase: DataBase.Wager);
                        isError = true;
                        _logger.LogError(ex1, $"exMessage:{ex1.Message} ||¡@{JsonConvert.SerializeObject(unsettled)}");
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
                    dueTimeSpan = TimeSpan.FromSeconds(15);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                _ = gametype == GameType.TCG_SEA_Lottery ?
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
}
