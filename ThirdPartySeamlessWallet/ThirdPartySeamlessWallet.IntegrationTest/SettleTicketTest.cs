using ExpectedObjects;

using FluentAssertions;

using NUnit.Framework;

using ThirdPartySeamlessWallet.IntegrationTest.Models;
using ThirdPartySeamlessWallet.IntegrationTest.SandBox;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using TpSeamlessBonus = ThirdPartySeamlessWallet.Service.Model.TpSeamlessBonus;



namespace ThirdPartySeamlessWallet.IntegrationTest
{
    public class SettleTicketTest
    {
        private TpCustInfo _tpCustInfo;
        private SettleTicketSandbox _settleTicketSandbox = new();

        [SetUp]
        public void Setup()
        {
            _tpCustInfo = new TpCustInfo()
            {
                CustId = 249951,
                SiteId = 1,
                UserName = "hanstest01",
                CurrencyId = 2,
                TpUserName = "Nova88_249951",
                TpPwd = "h5N3Bjn",
                TpCurrencyId = "2",
            };
        }

        #region SetData

        [TestCase(10, 10, ExpectedResult = 10)]
        [TestCase(10, 5, ExpectedResult = 10)]
        [TestCase(0, 5, ExpectedResult = 5, Description = "本次結算stake為0，使用上一次的結算結果")]
        [TestCase(10, null, ExpectedResult = 10)]
        public decimal SetData_Stake_Test(decimal stake, decimal? lastStake)
        {
            var betRecord = new BetRecordInfo()
            {
                Stake = stake,
            };

            BetRecordInfo refBetRecord = null;
            if (lastStake.HasValue)
            {
                refBetRecord = new BetRecordInfo()
                {
                    Stake = lastStake.Value,
                };
            }

            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, refBetRecord);
            SettleTicket.SetData();
            return SettleTicket.BetRecord.Stake;
        }

        [TestCase(1, 10, TicketStatus.Win, ExpectedResult = TicketStatus.Win)]
        [TestCase(1, 10, TicketStatus.UnSettled, ExpectedResult = TicketStatus.Win, Description = "TicketStatus用預設值")]
        [TestCase(1, 10, TicketStatus.Lost, ExpectedResult = TicketStatus.Win, Description = "TicketStatus錯誤")]
        [TestCase(0, 10, TicketStatus.Win, ExpectedResult = TicketStatus.FreeBetWin, Description = "stake為0，TicketStatus = FreeBetWin")]
        [TestCase(0, 10, TicketStatus.Lost, ExpectedResult = TicketStatus.FreeBetWin, Description = "stake為0，TicketStatus = FreeBetWin")]
        [TestCase(1, 0, TicketStatus.Lost, ExpectedResult = TicketStatus.Draw, Description = "winlostAmount為0，TicketStatus = Draw")]
        [TestCase(1, 0, TicketStatus.Win, ExpectedResult = TicketStatus.Draw, Description = "winlostAmount為0，TicketStatus = Draw")]
        [TestCase(1, -1, TicketStatus.Lost, ExpectedResult = TicketStatus.Lost)]
        [TestCase(1, -1, TicketStatus.Win, ExpectedResult = TicketStatus.Lost)]
        [TestCase(1, -1, TicketStatus.Refund, ExpectedResult = TicketStatus.Refund)]
        [TestCase(1, -1, TicketStatus.UnSettled, ExpectedResult = TicketStatus.Lost)]
        public TicketStatus SetData_TicketStatus_Test(decimal stake, decimal winlostAmount, TicketStatus ticketStatus)
        {
            var betRecord = new BetRecordInfo()
            {
                Stake = stake,
                WinlostAmount = winlostAmount,
                TicketStatus = ticketStatus,
                SettlementType = SettlementType.SettledForValid
            };

            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, null);
            SettleTicket.SetData();
            return SettleTicket.BetRecord.TicketStatus;
        }

        [TestCase(0, 123, ExpectedResult = 123, Description = "使用上一次結算的BonusId")]
        [TestCase(123, 456, ExpectedResult = 456, Description = "使用上一次結算的BonusId")]
        [TestCase(123, 0, ExpectedResult = 123)]
        [TestCase(0, 0, ExpectedResult = 0)]
        [TestCase(10, 0, ExpectedResult = 10)]
        [TestCase(10, null, ExpectedResult = 10)]
        public int SetData_BonusId_Test(int bonusId, int? lastBonusId)
        {
            var betRecord = new BetRecordInfo()
            {
                BonusId = bonusId,
            };

            BetRecordInfo refBetRecord = null;
            if (lastBonusId.HasValue)
            {
                refBetRecord = new BetRecordInfo()
                {
                    BonusId = lastBonusId.Value,
                };
            }

            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, refBetRecord);
            SettleTicket.SetData();
            return SettleTicket.BetRecord.BonusId;
        }

        #endregion

        #region CheckRebateExcludeGame

        [TestCase(6, 11, "200046", ExpectedResult = SettlementType.SettledForValidNotRollover)]
        [TestCase(6, 11, "fourguardians", ExpectedResult = SettlementType.SettledForValidNotRollover)]
        [TestCase(6, 11, "test-fourguardians", ExpectedResult = SettlementType.SettledForValid)]
        [TestCase(6, 11, "test", ExpectedResult = SettlementType.SettledForValid)]
        [TestCase(6, 11, "123", ExpectedResult = SettlementType.SettledForValid)]
        public SettlementType CheckRebateExcludeGame_Test(int tpId, int gameType, string gameID)
        {
            var transId = $"test-{DateTime.UtcNow.Ticks}";
            var betRecord = new BetRecordInfo()
            {
                SettlementType = SettlementType.SettledForValid,
                TicketStatus = TicketStatus.Lost,
                WinlostAmount = -100,
                TransId = transId,
                TpId = tpId,
                GameType = (GameType)gameType,
                GameID = gameID,
            };

            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, null);
            SettleTicket.CheckRebateExcludeGame();
            _settleTicketSandbox.Commit();

            return SettleTicket.BetRecord.SettlementType;
        }

        #endregion

        #region SetWinLost

        [TestCase("Settled", 5, TicketStatus.Win, null, TicketStatus.UnSettled, 5, Description = "正常結算")]
        [TestCase(null, 0, TicketStatus.Win, null, TicketStatus.UnSettled, null, Description = "winlost為0不需更新")]
        [TestCase("Settled", 5, TicketStatus.Win, 5, TicketStatus.UnSettled, 5, Description = "正常結算")]
        [TestCase("Settled", 5, TicketStatus.Win, 10, TicketStatus.Refund, 5, Description = "正常結算")]

        [TestCase("Re-Settled", 5, TicketStatus.Refund, 10, TicketStatus.Win, -10, Description = "Refund結算，扣回上次結算金額")]
        [TestCase("UnSettled", 5, TicketStatus.UnSettled, 10, TicketStatus.Win, -10, Description = "UnSettled結算，扣回上次結算金額")]
        [TestCase("Re-Settled", 5, TicketStatus.Refund, 10, TicketStatus.FreeBetWin, -10, Description = "Refund結算，扣回上次結算金額")]
        [TestCase("UnSettled", 5, TicketStatus.UnSettled, 10, TicketStatus.FreeBetWin, -10, Description = "UnSettled結算，扣回上次結算金額")]

        [TestCase("Re-Settled", 5, TicketStatus.Win, 10, TicketStatus.Win, -5, Description = "ReSettled結算，計算兩次差異金額")]
        [TestCase("Re-Settled", 10, TicketStatus.Win, 5, TicketStatus.Win, 5, Description = "ReSettled結算，計算兩次差異金額")]
        [TestCase("Re-Settled", 5, TicketStatus.Win, 10, TicketStatus.FreeBetWin, -5, Description = "ReSettled結算，計算兩次差異金額")]
        [TestCase("Re-Settled", 10, TicketStatus.Win, 5, TicketStatus.FreeBetWin, 5, Description = "ReSettled結算，計算兩次差異金額")]
        [TestCase("Re-Settled", 5, TicketStatus.FreeBetWin, 10, TicketStatus.FreeBetWin, -5, Description = "ReSettled結算，計算兩次差異金額")]
        [TestCase("Re-Settled", 10, TicketStatus.FreeBetWin, 5, TicketStatus.FreeBetWin, 5, Description = "ReSettled結算，計算兩次差異金額")]
        public void SetWinLost_Winnings_Test(string memo, decimal winlostAmount, TicketStatus ticketStatus, decimal? lastWinlostAmount, TicketStatus lastTicketStatus, decimal? expected)
        {
            var transId = $"test-{DateTime.UtcNow.Ticks}";
            var betRecord = new BetRecordInfo()
            {
                TicketStatus = ticketStatus,
                WinlostAmount = winlostAmount,
                TransId = transId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
                BonusId = 100,
            };

            BetRecordInfo refBetRecord = null;
            if (lastWinlostAmount.HasValue)
            {
                refBetRecord = new BetRecordInfo()
                {
                    TicketStatus = lastTicketStatus,
                    WinlostAmount = lastWinlostAmount.Value,
                    TransId = transId,
                    TpId = 41,
                    GameType = GameType.SBSW_Sports,
                    BonusId = 100,
                };
            }

            var custWinLost = _settleTicketSandbox.GetCustWinLost(_tpCustInfo.CustId);
            var custBonusWinLoss = _settleTicketSandbox.GetCustBonusWinLoss(betRecord.BonusId);

            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, refBetRecord);
            SettleTicket.SetWinLost();
            _settleTicketSandbox.Commit();

            if (expected.HasValue)
            {
                var assertCustWinLost = _settleTicketSandbox.GetCustWinLost(_tpCustInfo.CustId);
                var assertCustBonusWinLoss = _settleTicketSandbox.GetCustBonusWinLoss(betRecord.BonusId);

                assertCustWinLost.Winnings.Should().Be(custWinLost.Winnings + expected.Value);
                assertCustBonusWinLoss.Winnings.Should().Be(custBonusWinLoss.Winnings + expected.Value);
            }

            var winningsLog = _settleTicketSandbox.GetWinningsLog(transId);
            if (winningsLog != null)
            {
                winningsLog.ChangeWinnings.Should().Be(expected.Value);
                winningsLog.Memo.Should().Be(memo);
            }
            else
            {
                winningsLog.Should().Be(expected);
            }

            var bonusWinningsLog = _settleTicketSandbox.GetBonusWinningsLog(transId);
            if (bonusWinningsLog != null)
            {
                //輸贏Log分開，所以SP有加上ABS
                bonusWinningsLog.ChangeWinnings.Should().Be(Math.Abs(expected.Value));
                bonusWinningsLog.Memo.Should().Be(memo);
            }
            else
            {
                bonusWinningsLog.Should().Be(expected);
            }
        }

        [TestCase("Settled", -5, TicketStatus.Lost, null, TicketStatus.UnSettled, -5, Description = "正常結算")]
        [TestCase(null, 0, TicketStatus.Win, null, TicketStatus.UnSettled, null, Description = "winlost為0不需更新")]
        [TestCase("Settled", -5, TicketStatus.Win, -5, TicketStatus.UnSettled, -5, Description = "正常結算")]
        [TestCase("Settled", -5, TicketStatus.Win, -10, TicketStatus.Refund, -5, Description = "正常結算")]

        [TestCase("Re-Settled", 5, TicketStatus.Refund, -10, TicketStatus.Lost, 10, Description = "Refund結算，扣回上次結算金額")]
        [TestCase("UnSettled", 5, TicketStatus.UnSettled, -10, TicketStatus.Lost, 10, Description = "UnSettled結算，扣回上次結算金額")]

        [TestCase("Re-Settled", -5, TicketStatus.Lost, -10, TicketStatus.Lost, 5, Description = "ReSettled結算，計算兩次差異金額")]
        [TestCase("Re-Settled", -10, TicketStatus.Lost, -5, TicketStatus.Lost, -5, Description = "ReSettled結算，計算兩次差異金額")]
        public void SetWinLost_Losses_Test(string memo, decimal winlostAmount, TicketStatus ticketStatus, decimal? lastWinlostAmount, TicketStatus lastTicketStatus, decimal? expected)
        {
            var transId = $"test-{DateTime.UtcNow.Ticks}";
            var betRecord = new BetRecordInfo()
            {
                TicketStatus = ticketStatus,
                WinlostAmount = winlostAmount,
                TransId = transId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
                BonusId = 100,
            };

            BetRecordInfo refBetRecord = null;
            if (lastWinlostAmount.HasValue)
            {
                refBetRecord = new BetRecordInfo()
                {
                    TicketStatus = lastTicketStatus,
                    WinlostAmount = lastWinlostAmount.Value,
                    TransId = transId,
                    TpId = 41,
                    GameType = GameType.SBSW_Sports,
                    BonusId = 100,
                };
            }

            var custWinLost = _settleTicketSandbox.GetCustWinLost(_tpCustInfo.CustId);
            var custBonusWinLoss = _settleTicketSandbox.GetCustBonusWinLoss(betRecord.BonusId);

            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, refBetRecord);
            SettleTicket.SetWinLost();
            _settleTicketSandbox.Commit();

            if (expected.HasValue)
            {
                var assertCustWinLost = _settleTicketSandbox.GetCustWinLost(_tpCustInfo.CustId);
                var assertCustBonusWinLoss = _settleTicketSandbox.GetCustBonusWinLoss(betRecord.BonusId);

                assertCustWinLost.Losses.Should().Be(custWinLost.Losses + expected.Value);
                assertCustBonusWinLoss.Losses.Should().Be(custBonusWinLoss.Losses + expected.Value);
            }

            var lossesLog = _settleTicketSandbox.GetLossesLog(transId);
            if (lossesLog != null)
            {
                lossesLog.ChangeLosses.Should().Be(expected.Value);
                lossesLog.Memo.Should().Be(memo);
            }
            else
            {
                lossesLog.Should().Be(expected);
            }

            var bonusLossesLog = _settleTicketSandbox.GetBonusLossesLog(transId);
            if (bonusLossesLog != null)
            {
                //輸贏Log分開，所以SP有加上ABS
                bonusLossesLog.ChangeLosses.Should().Be(Math.Abs(expected.Value));
                bonusLossesLog.Memo.Should().Be(memo);
            }
            else
            {
                bonusLossesLog.Should().Be(expected);
            }

        }

        #endregion

        #region AutoCancelCheck

        [TestCase(10, 1, Description = "bonus高於5，不用Cancel")]
        [TestCase(1, 2, Description = "bonus低於5，Auto Cancel，退款1元")]
        [TestCase(2, 2, Description = "bonus低於5，Auto Cancel，退款2元")]
        [TestCase(0, 2, Description = "bonus等於5，Auto Cancel，無退款")]
        public void AutoCancelCheck_Test(decimal bonusAmount, int? expectedStatus)
        {
            var transId = $"test-{DateTime.UtcNow.Ticks}";
            var betRecord = new BetRecordInfo()
            {
                TicketStatus = TicketStatus.Lost,
                WinlostAmount = -100,
                TransId = transId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
                BonusId = 100,
            };

            // 初始化Bonus，指定剩餘Bonus金額
            _settleTicketSandbox.ResetBonusStatusAndBonusAmount(betRecord.BonusId, bonusAmount);

            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, null);
            SettleTicket.AutoCancelCheck();
            _settleTicketSandbox.Commit();

            // 檢查Bonus狀態符合預期
            if (expectedStatus.HasValue)
            {
                var custBonus = _settleTicketSandbox.GetCustBonus(betRecord.BonusId);
                custBonus.Status.Should().Be(expectedStatus.Value);

                var bonusInfo = _settleTicketSandbox.GetBonusInfo(betRecord.BonusId);
                bonusInfo.Status.Should().Be(expectedStatus.Value);

                var custBonusRollover = _settleTicketSandbox.GetCustBonusRollover(betRecord.BonusId);
                custBonusRollover.Status.Should().Be(expectedStatus.Value);
            }

            // Bonus Cance
            if (expectedStatus.HasValue && expectedStatus.Value == 2)
            {
                //檢查Cancel Log
                var bonusLog = _settleTicketSandbox.GetBonusLog(betRecord.BonusId);
                bonusLog.ChangeBonus.Should().Be(bonusAmount);

                // 有餘額要檢查退款紀錄
                if (bonusAmount > 0)
                {
                    var financialLog = _settleTicketSandbox.GetFinancialLog(betRecord.BonusId);
                    financialLog.ChangeAmount.Should().Be(bonusAmount);
                }
            }
        }


        [Test(Description = "先測試有未結算單不用Cancel，把注單改結算後正常Cancel")]
        public void UnSettle_AutoCancelCheck_Test()
        {
            long ticks = DateTime.UtcNow.Ticks;
            var unSettleTransId = $"test-{ticks - 1}";
            var transId = $"test-{ticks}";

            var betRecord = new BetRecordInfo()
            {
                TicketStatus = TicketStatus.Lost,
                WinlostAmount = -100,
                TransId = transId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
                BonusId = 100,
            };

            // 初始化Bonus，指定剩餘Bonus金額
            _settleTicketSandbox.ResetBonusStatusAndBonusAmount(betRecord.BonusId, 0);
            // 移除相同BonusId的結算紀錄
            _settleTicketSandbox.ResetTpBetRecord(betRecord.TpId, _tpCustInfo.CustId, betRecord.BonusId);
            // 新增一筆同BonusId的未結算紀錄
            var testRecord = new BetRecordInfo()
            {
                TicketStatus = TicketStatus.UnSettled,
                SettlementType = SettlementType.UnSettled,
                Stake = 100,
                ValidBet = 100,
                WinlostAmount = 0,
                TransId = unSettleTransId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
                BonusId = 100,
            };
            _settleTicketSandbox.SetTpBetRecord(_tpCustInfo, testRecord);

            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, null);
            SettleTicket.AutoCancelCheck();
            _settleTicketSandbox.Commit();

            // 檢查Bonus狀態為未取消
            var custBonus = _settleTicketSandbox.GetCustBonus(betRecord.BonusId);
            custBonus.Status.Should().Be(1);

            var bonusInfo = _settleTicketSandbox.GetBonusInfo(betRecord.BonusId);
            bonusInfo.Status.Should().Be(1);

            var custBonusRollover = _settleTicketSandbox.GetCustBonusRollover(betRecord.BonusId);
            custBonusRollover.Status.Should().Be(1);


            //更新為結算狀態
            testRecord.TicketStatus = TicketStatus.Win;
            testRecord.SettlementType = SettlementType.SettledForValid;
            _settleTicketSandbox.SetTpBetRecord(_tpCustInfo, testRecord);

            _settleTicketSandbox.BeginTransaction();
            SettleTicket.AutoCancelCheck();
            _settleTicketSandbox.Commit();

            // 檢查Bonus狀態為取消
            custBonus = _settleTicketSandbox.GetCustBonus(betRecord.BonusId);
            custBonus.Status.Should().Be(2);

            bonusInfo = _settleTicketSandbox.GetBonusInfo(betRecord.BonusId);
            bonusInfo.Status.Should().Be(2);

            custBonusRollover = _settleTicketSandbox.GetCustBonusRollover(betRecord.BonusId);
            custBonusRollover.Status.Should().Be(2);
        }

        #endregion

        #region SetBonusRollOver

        [TestCase("Settled", 10, SettlementType.SettledForValid, null, SettlementType.UnSettled, 10, Description = "正常結算")]
        [TestCase(null, 0, SettlementType.SettledForValid, null, SettlementType.UnSettled, null, Description = "stake為0不需更新")]
        [TestCase("Settled", 10, SettlementType.SettledForValid, 5, SettlementType.SettledForValidNotRollover, 10, Description = "正常結算")]
        [TestCase("Settled", 10, SettlementType.SettledForValid, 5, SettlementType.SettledForInvalid, 10, Description = "正常結算")]
        [TestCase("Settled", 10, SettlementType.SettledForValid, 5, SettlementType.Cheating, 10, Description = "正常結算")]

        [TestCase("Re-Settled", 50, SettlementType.SettledForValidNotRollover, 100, SettlementType.SettledForValid, -100, Description = "Refund結算，扣回上次結算金額")]
        [TestCase("UnSettled", 5, SettlementType.UnSettled, 10, SettlementType.SettledForValid, -10, Description = "UnSettled結算，扣回上次結算金額")]

        [TestCase("Re-Settled", 50, SettlementType.SettledForValid, 100, SettlementType.SettledForValid, -50, Description = "ReSettled結算，計算兩次差異金額")]
        [TestCase("Re-Settled", 30, SettlementType.SettledForValid, 10, SettlementType.SettledForValid, 20, Description = "ReSettled結算，計算兩次差異金額")]                
        public void BonusRollOver_Test(string memo, decimal stake, SettlementType settlementType, decimal? lastStake, SettlementType lastSettlementType, decimal? expected)
        {
            var transId = $"test-{DateTime.UtcNow.Ticks}";
            var betRecord = new BetRecordInfo()
            {
                TicketStatus = TicketStatus.Lost,
                SettlementType = settlementType,
                Stake = stake,
                ValidBet = stake,
                WinlostAmount = -100,
                TransId = transId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
                BonusId = 100,
            };

            BetRecordInfo refBetRecord = null;
            if (lastStake.HasValue)
            {
                refBetRecord = new BetRecordInfo()
                {
                    TicketStatus = TicketStatus.Lost,
                    SettlementType = lastSettlementType,
                    Stake = lastStake.Value,
                    ValidBet = lastStake.Value,
                    WinlostAmount = -100,
                    TransId = transId,
                    TpId = 41,
                    GameType = GameType.SBSW_Sports,
                    BonusId = 100,
                };
            }


            if (memo == "Re-Settled" || memo == "UnSettled")
            {
                // 需要先新增第一次的流水紀錄
                _settleTicketSandbox.CreateTurnOverLog(refBetRecord.ValidBet.Value, 2, _tpCustInfo, betRecord);
            }
            // 初始化Bonus，指定剩餘Bonus金額
            _settleTicketSandbox.ResetBonusStatusAndBonusAmount(betRecord.BonusId, 100);

            var custBonusRollover = _settleTicketSandbox.GetCustBonusRollover(betRecord.BonusId);

            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, refBetRecord);
            SettleTicket.SetBonusRollOver();
            _settleTicketSandbox.Commit();

            var turnOverLog = _settleTicketSandbox.GetLastTurnOverLog(transId);
            if (expected.HasValue)
            {
                // 檢查CustBonusRollover的Turnover正確
                var assertCustBonusRollover = _settleTicketSandbox.GetCustBonusRollover(betRecord.BonusId);
                assertCustBonusRollover.Turnover.Should().Be(custBonusRollover.Turnover + expected.Value);

                // 檢查TurnOverLog的紀錄有增加          
                turnOverLog.ChangeTurnOver.Should().Be(expected.Value);//SP有加上ABS
                turnOverLog.Memo.Should().Be(memo);
            }
            else
            {
                turnOverLog.Should().Be(expected);
            }
        }

        [Test(Description = "UnSettled結算，但第一次沒有新增流水，第二次結算不可以扣回")]
        public void BonusRollOver_Fullfill_UnSettled_Check_Test()
        {
            var transId = $"test-{DateTime.UtcNow.Ticks}";
            var betRecord = new BetRecordInfo()
            {
                TicketStatus = TicketStatus.UnSettled,
                SettlementType = SettlementType.UnSettled,
                Stake = 100,
                ValidBet = 100,
                WinlostAmount = -100,
                TransId = transId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
                BonusId = 100,
            };

            var refBetRecord = new BetRecordInfo()
            {
                TicketStatus = TicketStatus.Win,
                SettlementType = SettlementType.SettledForValid,
                Stake = 100,
                ValidBet = 100,
                WinlostAmount = -100,
                TransId = transId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
                BonusId = 100,
            };


            // 將Bonus狀態改成fullfill
            _settleTicketSandbox.SetBonusStatus(betRecord.BonusId, 4);

            var custBonusRollover = _settleTicketSandbox.GetCustBonusRollover(betRecord.BonusId);


            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, refBetRecord);
            SettleTicket.SetBonusRollOver();
            _settleTicketSandbox.Commit();


            // 檢查TurnOverLog的紀錄沒有增加     
            var turnOverLog = _settleTicketSandbox.GetLastTurnOverLog(transId);
            turnOverLog.Should().Be(null);

            // 檢查CustBonusRollover的Turnover正確
            var assertCustBonusRollover = _settleTicketSandbox.GetCustBonusRollover(betRecord.BonusId);
            assertCustBonusRollover.Turnover.Should().Be(custBonusRollover.Turnover + 0);
        }

        [TestCase(30, 50, 20, Description = "UnSettled結算，但第一次新增部分流水，第二次結算只能扣回第一次的流水金額")]
        [TestCase(30, 50, 10, Description = "UnSettled結算，但第一次新增部分流水，第二次結算只能扣回第一次的流水金額")]
        public void BonusRollOver_UnSettled_Turnover_Check_Test(decimal stake, decimal lastStake, decimal expected)
        {
            var transId = $"test-{DateTime.UtcNow.Ticks}";
            var betRecord = new BetRecordInfo()
            {
                TicketStatus = TicketStatus.UnSettled,
                SettlementType = SettlementType.UnSettled,
                Stake = stake,
                ValidBet = stake,
                WinlostAmount = -100,
                TransId = transId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
                BonusId = 100,
            };

            var refBetRecord = new BetRecordInfo()
            {
                TicketStatus = TicketStatus.Win,
                SettlementType = SettlementType.SettledForValid,
                Stake = lastStake,
                ValidBet = lastStake,
                WinlostAmount = -100,
                TransId = transId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
                BonusId = 100,
            };

            // 初始化Bonus，指定剩餘Bonus turnover
            var bonusAmount = lastStake * 2;
            var rollover = lastStake * 5;
            var turnover = rollover - expected; //設定turnover差多少達到rollover目標
            _settleTicketSandbox.ResetBonus(betRecord.BonusId, bonusAmount, rollover, turnover);
            var custBonusRollover = _settleTicketSandbox.GetCustBonusRollover(betRecord.BonusId);          

            // 需要先新增第一次的流水紀錄，第一次只拿到10
            _settleTicketSandbox.CreateTurnOverLog(expected, 2, _tpCustInfo, betRecord);

           
            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, refBetRecord);
            SettleTicket.SetBonusRollOver();
            _settleTicketSandbox.Commit();


            // 檢查TurnOverLog的紀錄有增加
            var lastTurnover = expected * -1;
            var turnOverLog = _settleTicketSandbox.GetLastTurnOverLog(transId);       
            turnOverLog.ChangeTurnOver.Should().Be(lastTurnover);

            // 檢查CustBonusRollover的Turnover正確
            var assertCustBonusRollover = _settleTicketSandbox.GetCustBonusRollover(betRecord.BonusId);
            assertCustBonusRollover.Turnover.Should().Be(custBonusRollover.Turnover + lastTurnover);
        }

        #endregion

        #region SetWithdrawalLimitAndEffectiveBet

        [TestCase("Settled", 10, TicketStatus.Win, null, TicketStatus.Refund, 10, Description = "正常結算")]
        [TestCase("Settled", 10, TicketStatus.Win, null, TicketStatus.UnSettled, 10, Description = "正常結算")]
        [TestCase("Settled", 0, TicketStatus.Win, null, TicketStatus.UnSettled, null, Description = "stake為0不需更新")]

        [TestCase("Settled", 10, TicketStatus.Lost, 10, TicketStatus.UnSettled, 10, Description = "正常結算")]
        [TestCase("Settled", 10, TicketStatus.Draw, 10, TicketStatus.Refund, 10, Description = "正常結算")]

        [TestCase("Re-Settled", 10, TicketStatus.Refund, 20, TicketStatus.Lost, -20, Description = "Refund的Re-Settled")]
        [TestCase("Re-Settled", 10, TicketStatus.Refund, 20, TicketStatus.Win, -20, Description = "Refund的Re-Settled")]
        [TestCase("Re-Settled", 10, TicketStatus.Refund, 20, TicketStatus.Draw, -20, Description = "Refund的Re-Settled")]

        [TestCase("UnSettled", 10, TicketStatus.UnSettled, 30, TicketStatus.Lost, -30, Description = "UnSettled結算")]
        [TestCase("UnSettled", 10, TicketStatus.UnSettled, 30, TicketStatus.Win, -30, Description = "UnSettled結算")]
        [TestCase("UnSettled", 10, TicketStatus.UnSettled, 30, TicketStatus.Draw, -30, Description = "UnSettled結算")]

        [TestCase("Re-Settled", 50, TicketStatus.Lost, 40, TicketStatus.Lost, 10, Description = "Re-Settled結算")]
        [TestCase("Re-Settled", 60, TicketStatus.Win, 40, TicketStatus.Win, 20, Description = "Re-Settled結算")]
        [TestCase("Re-Settled", 30, TicketStatus.Draw, 20, TicketStatus.Draw, 10, Description = "Re-Settled結算")]
        public void WithdrawalLimitAndEffectiveBet_Test(string memo, decimal stake, TicketStatus ticketStatus, decimal? lastStake, TicketStatus lastTicketStatus, decimal? expected)
        {
            var transId = $"test-{DateTime.UtcNow.Ticks}";
            var betRecord = new BetRecordInfo()
            {
                TicketStatus = ticketStatus,
                SettlementType = SettlementType.SettledForValid,
                Stake = stake,
                ValidBet = stake,
                WinlostAmount = -100,
                TransId = transId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
            };

            BetRecordInfo refBetRecord = null;
            if (lastStake.HasValue)
            {
                refBetRecord = new BetRecordInfo()
                {
                    TicketStatus = lastTicketStatus,
                    SettlementType = SettlementType.SettledForValid,
                    Stake = lastStake.Value,
                    ValidBet = lastStake.Value,
                    WinlostAmount = -100,
                    TransId = transId,
                    TpId = 41,
                    GameType = GameType.SBSW_Sports,
                };
            }

            _settleTicketSandbox.ResetCustWithdrawalLimit(_tpCustInfo.CustId);
            if (memo == "Re-Settled" || memo == "UnSettled")
            {
                // 需要先新增第一次的流水紀錄
                _settleTicketSandbox.CreateTurnOverLog(refBetRecord.ValidBet.Value, 1, _tpCustInfo, betRecord);
            }


            var custWithdrawalLimit = _settleTicketSandbox.GetCustWithdrawalLimit(_tpCustInfo.CustId);
            var custTransSummary = _settleTicketSandbox.GetCustTransSummary(_tpCustInfo.CustId);

            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, refBetRecord);
            SettleTicket.SetWithdrawalLimitAndEffectiveBet(GetTpSeamlessBonusType.ByTransId);
            _settleTicketSandbox.Commit();


            // 檢查TurnOverLog的紀錄有增加
            var turnOverLog = _settleTicketSandbox.GetLastTurnOverLog(transId);
            if (expected.HasValue)
            {
                // 檢查CustWithdrawalLimit的Turnover正確
                var assertCustWithdrawalLimit = _settleTicketSandbox.GetCustWithdrawalLimit(_tpCustInfo.CustId);
                assertCustWithdrawalLimit.TurnOver.Should().Be(custWithdrawalLimit.TurnOver + expected.Value);

                // 檢查EffectiveBetSum的ValidBet正確
                var assertCustTransSummary = _settleTicketSandbox.GetCustTransSummary(_tpCustInfo.CustId);
                assertCustTransSummary.EffectiveBetSum.Should().Be(custTransSummary.EffectiveBetSum + expected.Value);

                turnOverLog.ChangeTurnOver.Should().Be(expected.Value);
                turnOverLog.Memo.Should().Be(memo);
            }
            else
            {
                turnOverLog.Should().Be(expected);
            }
        }


        [TestCase("Settled", 100, 5, 10, TicketStatus.Win, null, TicketStatus.UnSettled, 5, Description = "正常結算")]
        [TestCase("Settled", 100, 20, 100, TicketStatus.Win, 10, TicketStatus.UnSettled, 80, Description = "正常結算")]
        [TestCase("Re-Settled結算", 100, 10, 100, TicketStatus.Win, 20, TicketStatus.Win, null, Description = "Re-Settled，不行沖銷提款限額")]

        public void WithdrawalLimitAndEffectiveBet_Bonus_Test(string memo, int bonusId, decimal bonusAmount, decimal stake, TicketStatus ticketStatus, decimal? lastStake, TicketStatus lastTicketStatus, decimal? expected)
        {
            var transId = $"test-{DateTime.UtcNow.Ticks}";
            var betRecord = new BetRecordInfo()
            {
                TicketStatus = ticketStatus,
                SettlementType = SettlementType.SettledForValid,
                Stake = stake,
                ValidBet = stake,
                WinlostAmount = -100,
                TransId = transId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
                BonusId = bonusId
            };

            BetRecordInfo refBetRecord = null;
            if (lastStake.HasValue)
            {
                refBetRecord = new BetRecordInfo()
                {
                    TicketStatus = lastTicketStatus,
                    SettlementType = SettlementType.SettledForValid,
                    Stake = lastStake.Value,
                    ValidBet = lastStake.Value,
                    WinlostAmount = -100,
                    TransId = transId,
                    TpId = 41,
                    GameType = GameType.SBSW_Sports,
                    BonusId = bonusId
                };
            }

            _settleTicketSandbox.ResetCustWithdrawalLimit(_tpCustInfo.CustId);
            _settleTicketSandbox.AddTpSeamlessBonus(new TpSeamlessBonus()
            {
                PromotionCategoryId = 9,
                TpId = 41,
                TransId = transId,
                GameType = (int)GameType.SBSW_Sports,
                CustId = _tpCustInfo.CustId,
                UserName = _tpCustInfo.UserName,
                CurrencyId = _tpCustInfo.CurrencyId,
                SiteId = _tpCustInfo.SiteId,
                Stake = bonusAmount,
                BonusId = bonusId,
            });

            var custWithdrawalLimit = _settleTicketSandbox.GetCustWithdrawalLimit(_tpCustInfo.CustId);

            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, refBetRecord);
            SettleTicket.SetWithdrawalLimitAndEffectiveBet(GetTpSeamlessBonusType.ByTransId);
            _settleTicketSandbox.Commit();


            // 檢查TurnOverLog的紀錄有增加
            var turnOverLog = _settleTicketSandbox.GetLastTurnOverLog(transId);
            if (expected.HasValue)
            {
                // 檢查CustWithdrawalLimit的Turnover正確
                var assertCustWithdrawalLimit = _settleTicketSandbox.GetCustWithdrawalLimit(_tpCustInfo.CustId);
                assertCustWithdrawalLimit.TurnOver.Should().Be(custWithdrawalLimit.TurnOver + expected.Value);

                turnOverLog.ChangeTurnOver.Should().Be(expected.Value);
                turnOverLog.Memo.Should().Be(memo);
            }
            else
            {
                turnOverLog.Should().Be(expected);
            }
        }

        [Test(Description = "UnSettled結算，但第一次沒有新增流水，第二次結算不可以扣回")]
        public void WithdrawalLimit_Fullfill_UnSettled_Check_Test()
        {
            var transId = $"test-{DateTime.UtcNow.Ticks}";
            var betRecord = new BetRecordInfo()
            {
                TicketStatus = TicketStatus.UnSettled,
                SettlementType = SettlementType.UnSettled,
                Stake = 100,
                ValidBet = 100,
                WinlostAmount = -100,
                TransId = transId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
            };

            var refBetRecord = new BetRecordInfo()
            {
                TicketStatus = TicketStatus.Win,
                SettlementType = SettlementType.SettledForValid,
                Stake = 100,
                ValidBet = 100,
                WinlostAmount = -100,
                TransId = transId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
            };

            //直接設定提款限額已沖滿
            _settleTicketSandbox.SetCustWithdrawalLimit(_tpCustInfo.CustId, 100,100);

            var custWithdrawalLimit = _settleTicketSandbox.GetCustWithdrawalLimit(_tpCustInfo.CustId);

            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, refBetRecord);
            SettleTicket.SetWithdrawalLimitAndEffectiveBet(GetTpSeamlessBonusType.ByTransId);
            _settleTicketSandbox.Commit();


            // 檢查TurnOverLog的紀錄沒有增加     
            var turnOverLog = _settleTicketSandbox.GetLastTurnOverLog(transId);
            turnOverLog.Should().Be(null);

            // 檢查CustWithdrawalLimit的Turnover正確
            var assertCustWithdrawalLimit = _settleTicketSandbox.GetCustWithdrawalLimit(_tpCustInfo.CustId);
            assertCustWithdrawalLimit.TurnOver.Should().Be(custWithdrawalLimit.TurnOver + 0);
        }

        [TestCase(30, 50, 20, Description = "UnSettled結算，但第一次新增部分流水，第二次結算只能扣回第一次的流水金額")]
        public void WithdrawalLimit_UnSettled_Turnover_Check_Test(decimal stake, decimal lastStake, decimal expected)
        {
            var transId = $"test-{DateTime.UtcNow.Ticks}";
            var betRecord = new BetRecordInfo()
            {
                TicketStatus = TicketStatus.UnSettled,
                SettlementType = SettlementType.UnSettled,
                Stake = stake,
                ValidBet = stake,
                WinlostAmount = -100,
                TransId = transId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
            };

            var refBetRecord = new BetRecordInfo()
            {
                TicketStatus = TicketStatus.Win,
                SettlementType = SettlementType.SettledForValid,
                Stake = lastStake,
                ValidBet = lastStake,
                WinlostAmount = -100,
                TransId = transId,
                TpId = 41,
                GameType = GameType.SBSW_Sports,
            };

            // 需要先新增第一次的流水紀錄
            _settleTicketSandbox.CreateTurnOverLog(expected, 1, _tpCustInfo, betRecord);

            //設定withdrawalLimit、turnover
            var withdrawalLimit = lastStake * 2;
            var turnover = withdrawalLimit - expected; //設定turnover差多少達到rollover目標
            _settleTicketSandbox.SetCustWithdrawalLimit(_tpCustInfo.CustId, withdrawalLimit, turnover);

            var custWithdrawalLimit = _settleTicketSandbox.GetCustWithdrawalLimit(_tpCustInfo.CustId);

            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, refBetRecord);
            SettleTicket.SetWithdrawalLimitAndEffectiveBet(GetTpSeamlessBonusType.ByTransId);
            _settleTicketSandbox.Commit();


            // 檢查TurnOverLog的紀錄沒有增加     
            var lastTurnover = expected * -1;
            var turnOverLog = _settleTicketSandbox.GetLastTurnOverLog(transId);
            turnOverLog.ChangeTurnOver.Should().Be(lastTurnover);

            // 檢查CustWithdrawalLimit的Turnover正確
            var assertCustWithdrawalLimit = _settleTicketSandbox.GetCustWithdrawalLimit(_tpCustInfo.CustId);
            assertCustWithdrawalLimit.TurnOver.Should().Be(custWithdrawalLimit.TurnOver + lastTurnover);
        }

        #endregion

        #region TpBetRecord

        [Test(Description = "測試結算紀錄正常寫入，二次結算有正常更新")]
        public void SetTpBetRecord_Test()
        {
            var tpId = 41;
            var transId = $"test-{DateTime.UtcNow.Ticks}";
            var gameType = GameType.SBSW_Sports;
            var nowUnix = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

            var unSettled = new BetRecordInfo()
            {
                TpId = tpId,
                TransId = transId,
                GameType = gameType,
                TpUserName = _tpCustInfo.TpUserName,
                SettledTime = nowUnix,
                TransTime = nowUnix,
                Stake = 1,
                ValidBet = 2,
                Revenue = 3,
                WinlostAmount = 4,
                ProgressiveBet = 5,
                ProgressiveWin = 6,
                SettlementType = SettlementType.UnSettled,
                TicketStatus = TicketStatus.UnSettled,
                OriginalState = "unSettled",
                TicketKey = 123,
                BonusId = 100,
                GameID = "unSettled-GameID"
            };
            var unSettledTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, unSettled, null);
            unSettledTicket.SetTpBetRecord();
            _settleTicketSandbox.Commit();

            //檢查寫入的未結算紀錄
            var unSettledbetRecord = _settleTicketSandbox.GetTpBetRecord(tpId, transId, (int)gameType);
            unSettledbetRecord.TpId.Should().Be(tpId);
            unSettledbetRecord.TransId.Should().Be(transId);
            unSettledbetRecord.GameType.Should().Be(gameType);
            unSettledbetRecord.TpUserName.Should().Be(_tpCustInfo.TpUserName);
            unSettledbetRecord.SettledTime.Should().Be(nowUnix);
            unSettledbetRecord.TransTime.Should().Be(nowUnix);
            unSettledbetRecord.Stake.Should().Be(1);
            unSettledbetRecord.ValidBet.Should().Be(2);
            unSettledbetRecord.Revenue.Should().Be(3);
            unSettledbetRecord.WinlostAmount.Should().Be(4);
            unSettledbetRecord.ProgressiveBet.Should().Be(5);
            unSettledbetRecord.ProgressiveWin.Should().Be(6);
            unSettledbetRecord.SettlementType.Should().Be(SettlementType.UnSettled);
            unSettledbetRecord.TicketStatus.Should().Be(TicketStatus.UnSettled);
            unSettledbetRecord.OriginalState.Should().Be("unSettled");
            unSettledbetRecord.TicketKey.Should().Be(123);
            unSettledbetRecord.BonusId.Should().Be(100);
            unSettledbetRecord.GameID.Should().Be("unSettled-GameID");


            //更新結算紀錄
            var settled = new BetRecordInfo()
            {
                TpId = tpId,
                TransId = transId,
                GameType = gameType,
                TpUserName = _tpCustInfo.TpUserName,
                SettledTime = nowUnix + 100,
                TransTime = nowUnix + 100,
                Stake = 11,
                ValidBet = 22,
                Revenue = 33,
                WinlostAmount = 44,
                ProgressiveBet = 55,
                ProgressiveWin = 66,
                SettlementType = SettlementType.SettledForValid,
                TicketStatus = TicketStatus.Win,
                OriginalState = "Settled",
                TicketKey = 123456,
                BonusId = 1000,
                GameID = "Settled-GameID"
            };

            var settleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, settled, null);
            settleTicket.SetTpBetRecord();
            _settleTicketSandbox.Commit();

            //檢查二次結算紀錄
            var settledbetRecord = _settleTicketSandbox.GetTpBetRecord(tpId, transId, (int)gameType);
            settledbetRecord.TpId.Should().Be(tpId);
            settledbetRecord.TransId.Should().Be(transId);
            settledbetRecord.GameType.Should().Be(gameType);
            settledbetRecord.TpUserName.Should().Be(_tpCustInfo.TpUserName);
            settledbetRecord.SettledTime.Should().Be(nowUnix + 100);
            settledbetRecord.TransTime.Should().Be(nowUnix + 100);
            settledbetRecord.Stake.Should().Be(11);
            settledbetRecord.ValidBet.Should().Be(22);
            settledbetRecord.Revenue.Should().Be(33);
            settledbetRecord.WinlostAmount.Should().Be(44);
            settledbetRecord.ProgressiveBet.Should().Be(55);
            settledbetRecord.ProgressiveWin.Should().Be(66);
            settledbetRecord.SettlementType.Should().Be(SettlementType.SettledForValid);
            settledbetRecord.TicketStatus.Should().Be(TicketStatus.Win);
            settledbetRecord.OriginalState.Should().Be("Settled");
            settledbetRecord.TicketKey.Should().Be(123456);
            settledbetRecord.BonusId.Should().Be(1000);
            settledbetRecord.GameID.Should().Be("Settled-GameID");
        }

        #endregion

        #region SetTpSeamlessBonusFlag

        [TestCase(TicketStatus.UnSettled, ExpectedResult = 0)]
        [TestCase(TicketStatus.Win, ExpectedResult = 1)]
        [TestCase(TicketStatus.Lost, ExpectedResult = 1)]
        public int SetTpSeamlessBonusFlag_Test(TicketStatus ticketStatus)
        {
            var transId = $"test-{DateTime.UtcNow.Ticks}";
            var bonusId = 100;
            var stake = 100;
            var tpId = 41;
            var gameType = GameType.SBSW_Sports;
            var betRecord = new BetRecordInfo()
            {
                SettlementType = SettlementType.SettledForValid,
                TicketStatus = ticketStatus,
                WinlostAmount = -100,
                TransId = transId,
                TpId = tpId,
                GameType = gameType,
                Stake = stake,
                BonusId = bonusId,
            };
            _settleTicketSandbox.AddTpSeamlessBonus(new TpSeamlessBonus()
            {
                PromotionCategoryId = 9,
                TpId = tpId,
                TransId = transId,
                GameType = (int)gameType,
                CustId = _tpCustInfo.CustId,
                UserName = _tpCustInfo.UserName,
                CurrencyId = _tpCustInfo.CurrencyId,
                SiteId = _tpCustInfo.SiteId,
                Stake = stake,
                BonusId = bonusId,
            });


            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, null);
            SettleTicket.SetTpSeamlessBonusFlag();
            _settleTicketSandbox.Commit();

            var flag = _settleTicketSandbox.GetTpSeamlessBonusFlag(tpId, transId);
            return flag;
        }

        #endregion

    }
}
