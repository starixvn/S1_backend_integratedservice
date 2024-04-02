using FluentAssertions;

using NUnit.Framework;

using ThirdPartySeamlessWallet.IntegrationTest.Models;
using ThirdPartySeamlessWallet.IntegrationTest.SandBox;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.SBSW;

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

        [TestCase(100, 50, 1, ExpectedResult = 50)]
        [TestCase(200, 200, 1, ExpectedResult = 0)]
        [TestCase(200, 300, 0, ExpectedResult = -100)]
        [TestCase(0, 100, 0, ExpectedResult = -100)]
        public decimal MainWallet_Test(decimal balance, decimal stake, int betResult)
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
                   
            Assert.AreEqual(betResult, result);
            return afterAmount;
        }

        [TestCase(100, 100, 50, 1, ExpectedResult = 150)]
        [TestCase(200, 100, 100, 1, ExpectedResult = 200)]        
        [TestCase(300, 100, 200, 1, ExpectedResult = 200)]
        [TestCase(0, 50, 50, 1, ExpectedResult = 0)]
        [TestCase(0, 50, 100, 0, ExpectedResult = -50)]
        [TestCase(200, 100, 400, 0, ExpectedResult = -100)]
        public decimal BonusWallet_Test(decimal balance, decimal bonusAmount, decimal stake, int betResult)
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
            Assert.AreEqual(useBonus, actualBonus);
            Assert.AreEqual(betResult, result);

            return afterAmount;
        }

        [TestCase(1000, 600, 700, 1, ExpectedResult = 900)]
        public decimal AllBet_BonusWallet_Test(decimal balance, decimal bonusAmount, decimal stake, int betResult)
        {
            var betDetail = new List<AllBetTransferDetails>()
            {
                new() {  BetNum = 1 , BetAmount = 500},
                new() {  BetNum = 2 , BetAmount = 100},
                new() {  BetNum = 3 , BetAmount = 100},
            };

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

            if (bonusId > 0)
            {
                var totalUseBonus = useBonus;
                foreach (var bet in betDetail)
                {
                    var betUseBonus = totalUseBonus >= bet.BetAmount ? bet.BetAmount : totalUseBonus;
                    _bettingService.AddTpBonus(
                        _tpId, 
                        $"{_transId}-{bet.BetNum}", 
                        _gameType, 
                        _tpCustInfo.CustId,
                        _tpCustInfo.UserName, 
                        _tpCustInfo.CurrencyId,
                        _tpCustInfo.SiteId, 
                        betUseBonus, 
                        bonusId, 
                        _bettingServiceSandbox.Trans);
                    totalUseBonus -= betUseBonus;
                }
            }


            if (result > 0)
            {
                _bettingServiceSandbox.Commit();

                var bonusLog = _bettingServiceSandbox.GetBonusLog(bonusId);
                bonusLog.ChangeBonus.Should().Be(useBonus);

                var tpSeamlessBonus = _bettingServiceSandbox.GetTpSeamlessBonus(_tpId, _transId);
                tpSeamlessBonus.Stake.Should().Be(useBonus);

                var tpSeamlessBonus1 = _bettingServiceSandbox.GetTpSeamlessBonus(_tpId, $"{_transId}-1");
                tpSeamlessBonus1.Stake.Should().Be(500);
                
                var tpSeamlessBonus2 = _bettingServiceSandbox.GetTpSeamlessBonus(_tpId, $"{_transId}-2");
                tpSeamlessBonus2.Stake.Should().Be(100);

                var tpSeamlessBonus3 = _bettingServiceSandbox.GetTpSeamlessBonus(_tpId, $"{_transId}-3");
                tpSeamlessBonus3.Should().Be(null);

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
            Assert.AreEqual(useBonus, actualBonus);
            Assert.AreEqual(betResult, result);

            return afterAmount;
        }


    }
}
