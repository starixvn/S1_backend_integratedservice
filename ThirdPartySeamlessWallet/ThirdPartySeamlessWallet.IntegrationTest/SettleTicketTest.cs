using ExpectedObjects;

using FluentAssertions;

using NUnit.Framework;

using ThirdPartySeamlessWallet.IntegrationTest.Models;
using ThirdPartySeamlessWallet.IntegrationTest.SandBox;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

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
                TpUserName = "XNova88249951",
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

        #region SetWinLost

        [TestCase("Settled", 5, TicketStatus.Win, null, TicketStatus.UnSettled, 5, Description = "正常結算")]
        [TestCase(null, 0, TicketStatus.Win, null, TicketStatus.UnSettled, null, Description = "winlost為0不需更新")]
        [TestCase("Settled", 5, TicketStatus.Win, 5, TicketStatus.UnSettled, 5, Description = "正常結算")]
        [TestCase("Settled", 5, TicketStatus.Win, 10, TicketStatus.Refund, 5, Description = "正常結算")]

        [TestCase("Re-Settled", 5, TicketStatus.Refund, 10, TicketStatus.Win, -10, Description = "Refund結算，扣回上次結算金額")]
        [TestCase("UnSettled", 5, TicketStatus.UnSettled, 10, TicketStatus.Win, -10, Description = "UnSettled結算，扣回上次結算金額")]
        [TestCase("Re-Settled", 5, TicketStatus.Refund, 10, TicketStatus.FreeBetWin, -10, Description = "Refund結算，扣回上次結算金額")]
        [TestCase("UnSettled", 5, TicketStatus.UnSettled, 10, TicketStatus.FreeBetWin, -10, Description = "UnSettled結算，扣回上次結算金額")]

        [TestCase("Re-Settled", 5, TicketStatus.Win, 10, TicketStatus.Win,  -5, Description = "ReSettled結算，計算兩次差異金額")]
        [TestCase("Re-Settled", 10, TicketStatus.Win, 5, TicketStatus.Win, 5, Description = "ReSettled結算，計算兩次差異金額")]
        [TestCase("Re-Settled", 5, TicketStatus.Win, 10, TicketStatus.FreeBetWin, -5, Description = "ReSettled結算，計算兩次差異金額")]
        [TestCase("Re-Settled", 10, TicketStatus.Win, 5, TicketStatus.FreeBetWin, 5, Description = "ReSettled結算，計算兩次差異金額")]
        [TestCase("Re-Settled", 5, TicketStatus.FreeBetWin, 10, TicketStatus.FreeBetWin,    -5, Description = "ReSettled結算，計算兩次差異金額")]
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
        [TestCase("Settled", -5, TicketStatus.Win, -5, TicketStatus.UnSettled,  -5, Description = "正常結算")]
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
                bonusLossesLog.ChangeLosses.Should().Be(Math.Abs(expected.Value));
                bonusLossesLog.Memo.Should().Be(memo);
            }
            else
            {
                bonusLossesLog.Should().Be(expected);
            }

        }


        #endregion
    }
}
