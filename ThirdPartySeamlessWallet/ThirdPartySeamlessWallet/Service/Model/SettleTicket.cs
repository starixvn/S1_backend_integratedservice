using System.Data;
using System.Security.Policy;

using Dapper;

using K4os.Compression.LZ4.Internal;

using MySql.Data.MySqlClient;

using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;

using static ThirdPartySeamlessWallet.Controllers.EBetController;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class SettleTicket
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlTransaction _trans;
        private readonly WalletService _walletService;
        private readonly ILogger<SettledService> _logger;
        private readonly TpBetRecordExtendBLL _tpBetRecordExtendBLL;

        public SettleTicket(
            TpCustInfo tpCustInfo,
            BetRecordInfo betRecord,
            BetRecordInfo refBetRecord,
            MySqlClient mainDbClient,
            MySqlTransaction trans,
            WalletService walletService,
            ILogger<SettledService> logger,
            TpBetRecordExtendBLL tpBetRecordExtendBLL)
        {
            TpCustInfo = tpCustInfo;
            BetRecord = betRecord;
            RefBetRecord = refBetRecord;
            _mainDbClient = mainDbClient;
            _trans = trans;
            _walletService = walletService;
            _logger = logger;
            _tpBetRecordExtendBLL = tpBetRecordExtendBLL;
        }

        /// <summary>
        /// 用戶資訊
        /// </summary>
        public TpCustInfo TpCustInfo { get; set; }

        /// <summary>
        /// 本次的結算紀錄
        /// </summary>
        public BetRecordInfo BetRecord { get; set; }

        /// <summary>
        /// 上一次的結算紀錄
        /// </summary>
        private BetRecordInfo RefBetRecord { get; set; }

        /// <summary>
        /// 設定初始資料
        /// </summary>
        public SettleTicket SetData()
        {
            // 本次結算Stake為0的話，使用前一次結算的Stake
            if (BetRecord.Stake == 0 && RefBetRecord != null)
            {
                BetRecord.Stake = RefBetRecord.Stake;
            }

            // 判斷本次結算的TicketStatus
            if (BetRecord.TicketStatus != TicketStatus.Refund && BetRecord.SettlementType != SettlementType.UnSettled)
            {
                if (BetRecord.WinlostAmount > 0)
                {
                    if (BetRecord.Stake != 0)
                    {
                        BetRecord.TicketStatus = TicketStatus.Win;
                    }
                    else
                    {
                        BetRecord.TicketStatus = TicketStatus.FreeBetWin;
                    }
                }
                else if (BetRecord.WinlostAmount == 0)
                {
                    BetRecord.TicketStatus = TicketStatus.Draw;
                }
                else
                {
                    BetRecord.TicketStatus = TicketStatus.Lost;
                }
            }

            // 前一次結算有BonusId要使用前一次的BonusId
            if (RefBetRecord != null && RefBetRecord.BonusId > 0)
            {
                BetRecord.BonusId = RefBetRecord.BonusId;
            }

            return this;
        }

        /// <summary>
        /// 檢查Rebate排除遊戲
        /// </summary>
        public SettleTicket CheckRebateExcludeGame()
        {
            var isExcludeGame = _mainDbClient.QueryFirst<bool>("Service_RebateExcludeGame_Check", _trans, new
            {
                TpCustInfo.SiteId,
                TpCustInfo.CurrencyId,
                GameType = (int)BetRecord.GameType,
                GameId = BetRecord.GameID,
            });
            if (isExcludeGame)
            {
                BetRecord.SettlementType = SettlementType.SettledForValidNotRollover;
            }
            return this;
        }

        /// <summary>
        /// 把TpSeamlessBonus的Flag改成已結算
        /// </summary>
        public SettleTicket SetTpSeamlessBonusFlag()
        {
            if (BetRecord.BonusId > 0 && BetRecord.TicketStatus != TicketStatus.UnSettled)
            {
                _mainDbClient.Execute("Service_TpSeamlessBonus_Flag_Set", _trans, new
                {
                    BetRecord.TpId,
                    TransId = string.IsNullOrEmpty(BetRecord.ReferenceNo) ? BetRecord.TransId : BetRecord.ReferenceNo,
                });
            }

            return this;
        }

        /// <summary>
        /// 更新玩家輸贏總金額與BonusId輸贏總金額
        /// </summary>
        public SettleTicket SetWinLost()
        {
            decimal winnings = BetRecord.WinlostAmount > 0 ? BetRecord.WinlostAmount : 0;
            decimal losses = BetRecord.WinlostAmount < 0 ? BetRecord.WinlostAmount : 0;
            var memo = "Settled";

            // 沒有前一次的結算紀錄，直接使用本次結算的金額
            if (RefBetRecord == null && BetRecord.WinlostAmount != 0)
            {
                if (BetRecord.TicketStatus == TicketStatus.Lost || BetRecord.TicketStatus == TicketStatus.Win || BetRecord.TicketStatus == TicketStatus.FreeBetWin)
                {
                    SetWinLostAmount(winnings, losses, memo);
                    if (BetRecord.BonusId > 0)
                    {
                        SetBonusWinLost(winnings, losses, memo);
                    }
                    return this;
                }
            }
            else if (RefBetRecord != null) // 有前一次結算紀錄，依據TicketStatus進行設定
            {
                decimal refWinnings = RefBetRecord.WinlostAmount > 0 ? RefBetRecord.WinlostAmount : 0;
                decimal refLosses = RefBetRecord.WinlostAmount < 0 ? RefBetRecord.WinlostAmount : 0;

                //上次: UnSettled、Refund
                //本次:Lost、Win、FreeBetWin
                //Settled結算，直接使用本次結算的金額
                if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                    && (BetRecord.TicketStatus == TicketStatus.Lost || BetRecord.TicketStatus == TicketStatus.Win || BetRecord.TicketStatus == TicketStatus.FreeBetWin))
                {
                    SetWinLostAmount(winnings, losses, memo);
                    if (BetRecord.BonusId > 0)
                    {
                        SetBonusWinLost(winnings, losses, memo);
                    }
                    return this;
                }

                // 上次:Lost、Win、FreeBetWin
                // 本次:UnSettled、Refund
                // UnSettled、Refund結算，扣回上次結算金額    
                if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.FreeBetWin)
                    && (BetRecord.TicketStatus == TicketStatus.UnSettled || BetRecord.TicketStatus == TicketStatus.Refund))
                {
                    winnings = refWinnings * -1;
                    losses = refLosses * -1;
                    memo = BetRecord.TicketStatus == TicketStatus.UnSettled ? "UnSettled" : "Re-Settled";

                    SetWinLostAmount(winnings, losses, memo);
                    if (BetRecord.BonusId > 0)
                    {
                        SetBonusWinLost(winnings, losses, memo);
                    }
                    return this;
                }

                // 上次:Lost、Win、Draw、FreeBetWin
                // 本次:Lost、Win、FreeBetWin
                // Re-Settled結算，計算兩次差異金額
                if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw || RefBetRecord.TicketStatus == TicketStatus.FreeBetWin)
                    && (BetRecord.TicketStatus == TicketStatus.Lost || BetRecord.TicketStatus == TicketStatus.Win || BetRecord.TicketStatus == TicketStatus.FreeBetWin))
                {
                    if (refWinnings != winnings || refLosses != losses)
                    {
                        winnings = winnings - refWinnings;
                        losses = losses - refLosses;
                        memo = "Re-Settled";

                        SetWinLostAmount(winnings, losses, memo);
                        if (BetRecord.BonusId > 0)
                        {
                            SetBonusWinLost(winnings, losses, memo);
                        }
                    }
                    return this;
                }
            }

            return this;
        }

        /// <summary>
        /// Bonus AutoCancel檢查
        /// </summary>
        public SettleTicket AutoCancelCheck()
        {
            // 只要本次注單狀態不是Unsettle都會執行AutoCancel檢查
            if (BetRecord.BonusId > 0 && BetRecord.TicketStatus != TicketStatus.UnSettled)
            {
                AutoCancelSeamlessBonus();             
            }

            return this;
        }

        /// <summary>
        /// 有Bonus將流水沖銷玩家Bonus的Rollover
        /// </summary>
        public SettleTicket SetBonusRollOver()
        {
            if (BetRecord.BonusId == 0)
            {
                return this;
            }

            var turnOver = BetRecord.ValidBet ?? BetRecord.Stake;
            var memo = "Settled";

            // 沒有前一次的結算紀錄，直接使用本次結算的金額
            if (RefBetRecord == null)
            {
                if (BetRecord.SettlementType == SettlementType.SettledForValid)
                {
                    SetRollOverTurnOver(turnOver, memo);
                }
            }
            else if (RefBetRecord != null) // 有前一次結算紀錄，依據SettlementType進行設定
            {
                decimal refTurnOver = RefBetRecord.ValidBet ?? RefBetRecord.Stake;

                //上次: 不等於SettledForValid
                //本次: SettledForValid
                //Settled結算，使用本次結算金額
                if (RefBetRecord.SettlementType != SettlementType.SettledForValid && BetRecord.SettlementType == SettlementType.SettledForValid)
                {
                    SetRollOverTurnOver(turnOver, memo);
                }

                //上次: 等於SettledForValid
                //本次: UnSettled、不等於SettledForValid
                //UnSettled結算，Re-Settled結算，扣回上次結算金額
                if ((RefBetRecord.SettlementType == SettlementType.SettledForValid && BetRecord.SettlementType == SettlementType.UnSettled ) ||
                    (RefBetRecord.SettlementType == SettlementType.SettledForValid && BetRecord.SettlementType != SettlementType.SettledForValid))
                {
                    turnOver = refTurnOver * -1;     
                    memo = BetRecord.SettlementType == SettlementType.UnSettled? "UnSettled" : "Re-Settled";
                    SetRollOverTurnOver(turnOver, memo);
                }

                //上次: SettledForValid
                //本次: SettledForValid
                //Re-Settled結算，計算兩次差異金額
                if (RefBetRecord.SettlementType == SettlementType.SettledForValid && BetRecord.SettlementType == SettlementType.SettledForValid)
                {
                    if (refTurnOver != turnOver)
                    {
                        turnOver = turnOver - refTurnOver;
                        memo = "Re-Settled";
                        SetRollOverTurnOver(turnOver, memo);
                    }                
                }
            }

            return this;
        }

        /// <summary>
        /// 沒Bonus將流水沖銷玩家的提款限額，並更新玩家有效下注總金額
        /// </summary>
        public SettleTicket SetWithdrawalLimitAndEffectiveBet(GetTpSeamlessBonusType getTpSeamlessBonusType)
        {
            var turnOver = BetRecord.ValidBet ?? BetRecord.Stake;
            var memo = "Settled";

            if (BetRecord.BonusId == 0)
            {
                if (RefBetRecord == null) // 沒有前一次的結算紀錄，直接使用本次結算的金額
                {
                    if (BetRecord.TicketStatus == TicketStatus.Lost || BetRecord.TicketStatus == TicketStatus.Win || BetRecord.TicketStatus == TicketStatus.Draw)
                    {
                        SetWithdrawalLimitTurnOver(turnOver, memo);
                        SetEffectiveBetSum(turnOver);
                        return this;
                    }
                }
                else // 有前一次結算紀錄，依據TicketStatus進行設定
                {
                    decimal refTurnOver = RefBetRecord.ValidBet ?? RefBetRecord.Stake;

                    //上次: UnSettled、Refund
                    //本次:Lost、Win、Draw
                    //Settled結算，直接使用本次結算的金額
                    if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                    && (BetRecord.TicketStatus == TicketStatus.Lost || BetRecord.TicketStatus == TicketStatus.Win || BetRecord.TicketStatus == TicketStatus.Draw))
                    {
                        SetWithdrawalLimitTurnOver(turnOver, memo);
                        SetEffectiveBetSum(turnOver);
                        return this;
                    }

                    // 上次:Lost、Win、Draw
                    // 本次:UnSettled、Refund
                    // Refund、UnSettled結算，扣回上次結算金額    
                    if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw)
                        && (BetRecord.TicketStatus == TicketStatus.Refund || BetRecord.TicketStatus == TicketStatus.UnSettled))
                    {
                        turnOver = refTurnOver * -1;
                        memo = BetRecord.TicketStatus == TicketStatus.UnSettled ? "UnSettled" : "Re-Settled";
                        SetWithdrawalLimitTurnOver(turnOver, memo);
                        SetEffectiveBetSum(turnOver);
                        return this;
                    }

                    // 上次:Lost、Win、Draw
                    // 本次:Lost、Win、Draw
                    // Re-Settled結算，計算兩次差異金額
                    if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw)
                        && (BetRecord.TicketStatus == TicketStatus.Lost || BetRecord.TicketStatus == TicketStatus.Win || BetRecord.TicketStatus == TicketStatus.Draw))
                    {
                        if (refTurnOver != turnOver)
                        {
                            turnOver = turnOver - refTurnOver;
                            SetWithdrawalLimitTurnOver(turnOver, "Re-Settled");
                            SetEffectiveBetSum(turnOver);
                            return this;
                        }
                    }
                }
            }
            else
            {
                // 使用Bonus的要判斷是否有用到主錢包下注，主錢包下注的金額，第一次Settle時要沖提款限額
                var useBonus = GetUseBonus(getTpSeamlessBonusType);
                var stakeWithoutBonus = turnOver - useBonus; // 沒有使用Bonus用主錢包下注的下注的金額
                if (stakeWithoutBonus > 0 && (RefBetRecord == null || RefBetRecord.TicketStatus == TicketStatus.UnSettled))
                {
                    if (BetRecord.TicketStatus == TicketStatus.Lost || BetRecord.TicketStatus == TicketStatus.Win || BetRecord.TicketStatus == TicketStatus.Draw)
                    {
                        SetWithdrawalLimitTurnOver(stakeWithoutBonus, memo);
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// 寫入或更新結算資料
        /// </summary>
        public SettleTicket SetTpBetRecord()
        {
            _mainDbClient.Execute("Service_TpBetRecord_Set_V3", _trans, new
            {
                BetRecord.TpId,
                BetRecord.TransId,
                GameType = (int)BetRecord.GameType,
                TpCustInfo.CustId,
                TpCustInfo.UserName,
                TpCustInfo.CurrencyId,
                TpCustInfo.SiteId,
                BetRecord.TpUserName,
                BetRecord.SettledTime,
                BetRecord.TransTime,
                BetRecord.Stake,
                BetRecord.ValidBet,
                BetRecord.Revenue,
                BetRecord.WinlostAmount,
                BetRecord.ProgressiveBet,
                BetRecord.ProgressiveWin,           
                SettlementType = (int)BetRecord.SettlementType,
                TicketStatus = (int)BetRecord.TicketStatus,
                BetRecord.OriginalState,
                BetRecord.TicketKey,
                BetRecord.BonusId,
                BetRecord.GameID
            });
            return this;
        }

        /// <summary>
        /// 寫入TpBetRecordExtend資料，提供給後台BetList查詢
        /// </summary>
        public SettleTicket AddTpBetRecordExtend()
        {
            Task.Run(async () =>
            {
                try
                {
                    await _tpBetRecordExtendBLL.AddTpBetRecordExtend(BetRecord, TpCustInfo);
                }
                catch (Exception ex )
                {
                    _logger.LogError(ex, $"AddTpBetRecordExtend Exception || {JsonConvert.SerializeObject(BetRecord)}");
                }              
            });
            return this;
        }

        private int SetWinLostAmount(decimal winnings, decimal losses, string memo)
        {
            if (winnings == 0 && losses == 0)
            {
                return 0;
            }

            var p = new 
            { 
                BetRecord.TpId, 
                TpCustInfo.CustId, 
                TpCustInfo.UserName, 
                TpCustInfo.SiteId, 
                Winnings = winnings, 
                Losses = losses, 
                Memo = memo, 
                RefTransId = BetRecord.TransId
            };
            return _mainDbClient.Execute("Common_WinLostAmount_Set_V2", _trans, p);
        }

        private int SetBonusWinLost(decimal winnings, decimal losses, string memo)
        {
            if (winnings == 0 && losses == 0)
            {
                return 0;
            }

            var p = new 
            {
                BetRecord.TpId, 
                GameType = (int)BetRecord.GameType,
                TpCustInfo.CustId,
                TpCustInfo.UserName,
                TpCustInfo.SiteId,
                Winnings = winnings,
                Losses = losses,
                Memo = memo,
                BetRecord.BonusId, 
                RefTransId = BetRecord.TransId
            };
            return _mainDbClient.Execute("Service_BonusWinLost_Set_V2", _trans, p);
        }

        private int AutoCancelSeamlessBonus()
        {
            var p = new DynamicParameters(new {
                BetRecord.BonusId,
                TpCustInfo.CurrencyId,
                GameType = (int)BetRecord.GameType,
                TpCustInfo.CustId,
                TpCustInfo.UserName,
                TpCustInfo.SiteId, 
                BetRecord.TransId 
            });
            p.Add("Bonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("BonusStatus", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Service_AutoCancelSeamlessBonus_Set_V4", _trans, p);
            var bonus = p.Get<decimal?>("Bonus") ?? 0;
            var bonusStatus = p.Get<int?>("BonusStatus") ?? 0;

            if (bonusStatus == 2)
            {
                _logger.LogWarning($"AutoCancelSeamlessBonus Success: BonusId={BetRecord.BonusId}, TransId={BetRecord.TransId}, bonus={bonus}");
                if (bonus > 0)
                {
                    result = _walletService.SetBalance(TpCustInfo.CustId, TpCustInfo.SiteId, TpCustInfo.UserName, bonus, 0, TransType.Prize, $"Bonus: {Math.Abs(bonus):#,##0.##}", BetRecord.BonusId.ToString(), out _, _trans.Connection, _trans);
                }
            }

            return result;
        }

        private int SetRollOverTurnOver(decimal turnOver, string memo) 
        {
            if (turnOver == 0)
            {
                return 0;
            }

            var p = new DynamicParameters(new {
                BetRecord.TpId, 
                GameType = (int)BetRecord.GameType, 
                TpCustInfo.CustId, 
                TpCustInfo.UserName, 
                TpCustInfo.SiteId, 
                Turnover = turnOver,
                Memo = memo, 
                RefTransId = BetRecord.TransId,
            });

            p.Add("BonusId", value: BetRecord.BonusId, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            return _mainDbClient.Execute("Service_RolloverTurnover_Set_V4", _trans, p);
        }

        private decimal GetUseBonus(GetTpSeamlessBonusType getTpSeamlessBonusType)
        {
            var spName = getTpSeamlessBonusType == GetTpSeamlessBonusType.ByGameTypeAndTransId ?
               "Service_TpSeamlessBonus_UseBonus_Get" : "Service_TpSeamlessBonus_UseBonus_GetByTransID";
            var p = new DynamicParameters(new { 
                BetRecord.TpId, 
                TransId = string.IsNullOrEmpty(BetRecord.ReferenceNo) ? BetRecord.TransId : BetRecord.ReferenceNo, 
                GameType = (int)BetRecord.GameType
            });
            p.Add("BonusId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("UseBonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            _mainDbClient.Execute(spName, parameters: p);

            return p.Get<decimal>("UseBonus");
        }

        private int SetWithdrawalLimitTurnOver(decimal turnOver, string memo)
        {
            if (turnOver == 0)
            {
                return 0;
            }

            var p = new { 
                BetRecord.TpId, 
                GameType = (int)BetRecord.GameType,
                TpCustInfo.CustId,
                TpCustInfo.UserName,
                TpCustInfo.SiteId, 
                TurnOver = turnOver, 
                Memo = memo, 
                RefTransId = BetRecord.TransId,
            };
            return _mainDbClient.Execute("Common_WithdrawalLimitTurnOver_Set_V2", _trans, p);
        }

        private int SetEffectiveBetSum(decimal stake)
        {
            if (stake == 0)
            {
                return 0;
            }

            var p = new {
                TpCustInfo.CustId,
                TpCustInfo.SiteId,
                Stake = stake
            };
            return _mainDbClient.Execute("Service_EffectiveBetSum_Set", _trans, p);
        }
    }
}
