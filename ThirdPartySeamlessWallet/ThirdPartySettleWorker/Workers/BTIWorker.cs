using System.Xml.Linq;

using Serilog.Context;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class BTIWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.BTI;
        private Timer _timerSports;
        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<BTIWorker> _logger;
        private readonly string _tpName;

        public BTIWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<BTIWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerSports = new Timer(DoSettle, GameType.BTI_Sports, TimeSpan.Zero, TimeSpan.Zero);
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
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType, 2000);
                _logger.LogWarning($"GetTpUnsettleList Count: {tpUnsettleList.Count()}");

                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{gameType} no unsettle data.");
                }

                if (tpUnsettleList.Count() == 200)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }

                foreach (var reserve_id in tpUnsettleList.Select(tpUnsettle => tpUnsettle.TransId))
                {
                    try
                    {
                        var unsettledList = _tpUnSettledBLL.GetTpUnsettleData<BTISettleData>("Service_BettransSports_BTI_Settle_Get", new { reserve_id }, DataBase.Main);
                        var debitList = _tpUnSettledBLL.GetTpUnsettleData<BTIDebitData>("Service_BettransSports_BTI_Debit_Get", new { reserve_id }, DataBase.Main);

                        var tpUserName = unsettledList.First().Account;
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);

                        //先從DebitList取得第一筆BetID
                        var betID = debitList.Any() ? debitList.First().BetID : null;
                        var gameId = debitList.Any() ? FindGameId(debitList) : string.Empty;

                        //使用BetID作為transId，如果DebitList是空的，使用unsettledList裡面的ReserveId
                        var transId = betID ?? unsettledList.First().TransId;

                        // 下注總金額，使用Reserve的Amout，只會有一筆所以用First
                        var stake = unsettledList.First().DebitAmount;
                        // 派彩總金額，使用Credit的Amout，會有一筆所以用Sum
                        var payout = unsettledList.Sum(i => i.CreditAmount);
                        // 計算有效下注 & 注單狀態是否取消(creditcustomer)
                        var validBet = CaculateValidBetAmount(unsettledList, debitList, out bool isCanceled);
                        // 注單狀態是否完成
                        var isClosed = unsettledList.Any(i => i.CurrentStatus.Equals("Closed", StringComparison.OrdinalIgnoreCase));
                        //未結算預設值
                        var winlostAmount = 0m;
                        var settlementType = SettlementType.UnSettled;
                        var ticketStatus = TicketStatus.UnSettled;

                        //設定結算值
                        if (isClosed)
                        {
                            winlostAmount = payout - stake;
                            settlementType = SettlementType.SettledForValid;
                            ticketStatus = winlostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost;
                        }

                        //Reserve.TicketStatus = 3就是下注失敗退款
                        if (unsettledList.Any(i => i.TicketStatus == (int)TicketStatus.Refund) || isCanceled)
                        {
                            settlementType = SettlementType.SettledForInvalid;
                            ticketStatus = TicketStatus.Refund;
                            winlostAmount = 0;
                        }

                        //下注流程異常，debit沒有資料，unsettledList的TicketStatus未更新，需要手動處理
                        if (betID == null && ticketStatus != TicketStatus.Refund)
                        {
                            continue;
                        }

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = stake,
                            ValidBet = validBet,
                            WinlostAmount = winlostAmount,
                            TransId = transId,
                            GameType = gameType,
                            TpUserName = tpUserName,
                            SettledTime = unsettledList.Max(i => i.SettleTime),
                            TransTime = unsettledList.Min(i => i.TransTime),
                            SettlementType = settlementType,
                            TicketStatus = ticketStatus,
                            GameID = gameId,
                            ReferenceNo = reserve_id,
                        };

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Sports);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                if (isClosed)
                                {
                                    _tpUnSettledBLL.SetSettleFlag("Service_BettransSports_BTI_Settle_Set_V2", new { account = tpUserName, transId = reserve_id, isSuccess = true });
                                }
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, reserve_id, true);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, reserve_id, false);
                        });
                        LogContext.PushProperty("TP", _tpName);
                        _logger.LogError(ex, $"BTI Settle exception. {ex.Message} || reserve_id : {reserve_id}");
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
                _logger.LogError(ex, $"BTI Settle exception. {ex.Message}");
            }
            finally
            {
                _timerSports.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private decimal CaculateValidBetAmount(IEnumerable<BTISettleData> settleList, IEnumerable<BTIDebitData> debitList, out bool isCanceled)
        {
            var validBetAmount = 0m;
             isCanceled = false;

            foreach (var debit in debitList)
            {
                //該注單號碼的下注金額
                var debitAmount = debit.amount;
                //找出該注單號碼最後結算狀態
                var settleStatus = settleList.Where(i => i.BetID == debit.BetID)
                    ?.MaxBy(i => i.seq_num)
                    ?.GetSettleStatus() ?? "";

                // 注單結算為贏 或 輸 => 有效投注額為100 %
                // 結算為 半贏 或 半輸 => 有效投注額為50 %
                // 結算為 取消 或 平局 或 提前結算 => 有效投注額為0
                validBetAmount += settleStatus switch
                {
                    "Lost" or "Won" => debitAmount * 1,
                    "Half Lost" or "Half Won" => debitAmount * 0.5m,
                    "Draw" or "Canceled" or "Cashout" => 0m,
                    _ => 0m,
                };

                // 檢查最後是否取消
                if (settleStatus.Equals("Canceled", StringComparison.OrdinalIgnoreCase))
                {
                    isCanceled = true;
                }
            }

            return validBetAmount;
        }

        private string FindGameId(IEnumerable<BTIDebitData> debitList)
        {
            try
            {
                var bodyData = debitList.FirstOrDefault()?.BodyData;
                if (bodyData == null)
                {
                    return string.Empty;
                }
                var xd = XDocument.Parse(bodyData);
                var selection = xd.Descendants("Selection").FirstOrDefault();
                return selection.Attribute("BranchID").Value;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
