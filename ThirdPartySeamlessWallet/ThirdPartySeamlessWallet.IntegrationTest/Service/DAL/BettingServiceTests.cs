using FluentAssertions;

using NUnit.Framework;

using ThirdPartySeamlessWallet.IntegrationTest.Models;
using ThirdPartySeamlessWallet.IntegrationTest.SandBox;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.IntegrationTest.Service.DAL
{
    public class BettingServiceTests
    {
        private readonly BettingService _bettingService;
        private BettingServiceSandbox _bettingServiceSandbox = new();

        public BettingServiceTests()
        {
            _bettingService = new BettingService(_bettingServiceSandbox.ServiceProvider);
        }

        private TpCustInfo _tpCustInfo;

        private int _tpId;
        private string _transId;
        private int _gameType;

        [SetUp]
        public void Setup()
        {
            var now = DateTime.Now;
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
            _tpId = 41;
            _gameType = (int)GameType.SBSW_Sports;
            _transId = $"test-{now.ToFileTime()}";
        }

        [TestCase(100, 50, ExpectedResult = 1)]
        [TestCase(200, 200, ExpectedResult = 1)]
        [TestCase(200, 300, ExpectedResult = 0)]
        [TestCase(0, 100, ExpectedResult = 0)]
        public int MainWallet_Test(decimal balance, decimal stake)
        {
            var amount = Math.Abs(stake) * -1;
            int bonusId = 100;
            _bettingServiceSandbox.BeginTransaction();
            
            _bettingServiceSandbox.SetCustWallet(_tpCustInfo.CustId, balance);
            _bettingServiceSandbox.CancelBonus(bonusId);

            var result = _bettingService.PlaceBet(
                tpCustInfo: _tpCustInfo,
                amount: amount,
                tpId: _tpId,
                tpTransId: _transId,
                gameType: _gameType,
                refId: _transId,
                afterAmount: out decimal afterAmount,
                bonusId: ref bonusId,
                useBonus: out decimal _,
                conn: _bettingServiceSandbox.Conn,
                trans: _bettingServiceSandbox.Trans,
                memo: " test"
                );

            

            if (result > 0)
            {
                _bettingServiceSandbox.Commit();
                var financialLog = _bettingServiceSandbox.GetFinancialLog(_transId);
                financialLog.ChangeAmount.Should().Be(stake);
            }
            else
            {
                _bettingServiceSandbox.Rollback();
            }
                   
            Assert.AreEqual(afterAmount, balance - stake);
            return result;
        }

        [TestCase(100, 100, 50, ExpectedResult = 1)]
        [TestCase(200, 100, 100, ExpectedResult = 1)]        
        [TestCase(300, 100, 200, ExpectedResult = 1)]
        [TestCase(0, 50, 50, ExpectedResult = 1)]
        [TestCase(0, 50, 100, ExpectedResult = 0)]
        [TestCase(200, 100, 400, ExpectedResult = 0)]
        public int BonusWallet_Test(decimal balance, decimal bonusAmount, decimal stake)
        {
            var amount = Math.Abs(stake) * -1;
            int bonusId = 100;
            _bettingServiceSandbox.BeginTransaction();

            _bettingServiceSandbox.SetCustWallet(_tpCustInfo.CustId, balance);
            _bettingServiceSandbox.ResetBonusStatusAndBonusAmount(bonusId, bonusAmount);

            var result = _bettingService.PlaceBet(
                tpCustInfo: _tpCustInfo,
                amount: amount,
                tpId: _tpId,
                tpTransId: _transId,
                gameType: _gameType,
                refId: _transId,
                afterAmount: out decimal afterAmount,
                bonusId: ref bonusId,
                useBonus: out decimal useBonus,
                conn: _bettingServiceSandbox.Conn,
                trans: _bettingServiceSandbox.Trans,
                memo: " test"
                );

            if (result > 0)
            {
                _bettingServiceSandbox.Commit();

                var bonusLog = _bettingServiceSandbox.GetBonusLog(bonusId);
                bonusLog.ChangeBonus.Should().Be(useBonus);

                var tpSeamlessBonus = _bettingServiceSandbox.GetTpSeamlessBonus(_tpId, _transId);
                tpSeamlessBonus.Stake.Should().Be(useBonus);

                if (stake > bonusAmount)
                {
                    var financialLog = _bettingServiceSandbox.GetFinancialLog(_transId);
                    financialLog.ChangeAmount.Should().Be(stake - useBonus);
                }
            }
            else
            {
                _bettingServiceSandbox.Rollback();
            }

            var actualBonusId = 100;
            var actualBonus = stake > bonusAmount ? bonusAmount : stake;
            Assert.AreEqual(bonusId, actualBonusId);
            Assert.AreEqual(afterAmount, balance + bonusAmount - stake);
            Assert.AreEqual(useBonus, actualBonus);         

            return result;
        }


    }
}
