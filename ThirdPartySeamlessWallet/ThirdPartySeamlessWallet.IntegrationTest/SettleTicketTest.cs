using NUnit.Framework;

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
   
        [TestCase(1, 10,TicketStatus.Win, ExpectedResult = TicketStatus.Win)]
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

        [TestCase(10, null, ExpectedResult = 10)]

        public decimal SetWinLost_Winnings_Test(decimal winlostAmount, decimal? lastWinlostAmount)
        {
            var transId = $"test-{DateTime.UtcNow.Ticks}";
            var betRecord = new BetRecordInfo()
            {
                TicketStatus = TicketStatus.Win,
                WinlostAmount = winlostAmount,
                TransId = transId,
            };

            BetRecordInfo refBetRecord = null;
            if (lastWinlostAmount.HasValue)
            {
                refBetRecord = new BetRecordInfo()
                {
                    TicketStatus = TicketStatus.Win,
                    WinlostAmount = lastWinlostAmount.Value,
                    TransId = transId,
                };
            }

            var SettleTicket = _settleTicketSandbox.CreateSettleTicket(_tpCustInfo, betRecord, refBetRecord);
            SettleTicket.SetWinLost();
            _settleTicketSandbox.Commit();

            var winningsLog = _settleTicketSandbox.GetWinningsLog(transId);

            return winningsLog.ChangeWinnings;
        }


        #endregion
    }
}
