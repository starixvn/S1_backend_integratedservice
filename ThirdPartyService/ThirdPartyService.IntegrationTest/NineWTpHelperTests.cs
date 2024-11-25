using FluentAssertions;
using NUnit.Framework;
using ThirdParty.NineW;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdPartyService.IntegrationTest
{
    [TestFixture()]
    public class NineWTpHelperTests
    {
        private TpHelper _tpHelper;
        private TpCustInfo _tpCustInfo;
        private Dictionary<string, string> _inputs;
        private IServiceProvider _provider;

        [SetUp]
        public void Setup()
        {
            //Arrange
            _tpHelper = new TpHelper(_provider);

            _tpCustInfo = new TpCustInfo()
            {
                TpId = 31,
                CustId = 249951,
                SiteId = 1,
                UserName = "hanstest01",
                TpUserName = "XNova88249951",
                TpPwd = "h5N3Bjn",
                TpCurrencyId = "2",
                ExchangeRate = 1.0000m,
                IsTest = false,
                ActStatus = 2,
                Status = 1,
                CStatus = 1,
            };

            _inputs = new Dictionary<string, string>
            {
                { "ApiUrl", "https://apiinfo.cckk77.net/api/apiWallet" },
                { "OldApiUrl", "https://test81.cckk77.net/apiWallet" },
                { "cert", "wkVwQ8sMVr252EA9" },
                { "agent", "testnvccricma" },
                { "website", "WICKETSPRO" },
            };
        }

        [Test()]
        public void CreateMemberTest()
        {
            //Act
            var actual = _tpHelper.CreateMember(_tpCustInfo, _inputs);

            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }

        [Test()]
        public void LoginTest()
        {
            //Arrange
            var appType = AppType.Desktop;
            var tpLangId = "1";
            _inputs.Add("fun", "0");

            //Act
            var actual = _tpHelper.Login(_tpCustInfo, appType, tpLangId, _inputs);

            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }

        [Test()]
        public void GetBalanceTest()
        {
            //Act
            var actual = _tpHelper.GetBalance(_tpCustInfo, _inputs);

            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }

        [TestCase(100)]
        public void TransferTest(decimal amount)
        {
            //Arrange
            var direction = Direction.ToTp;
            var transID = $"test{DateTime.Now.ToFileTime()}";

            //Act
            var actual = _tpHelper.Transfer(_tpCustInfo, _inputs, amount, direction, transID, null);

            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
            var checkResult = _tpHelper.CheckTransfer(_tpCustInfo, _inputs, amount, direction, transID, null);
            checkResult.ErrorCode.Should().Be(ErrorCode.Success);
        }

        [TestCase(1000)]
        public void TransferFlowTest(decimal amount)
        {
            //先檢查第三方餘額
            var tpuserBalance = _tpHelper.GetBalance(_tpCustInfo, _inputs);
            tpuserBalance.ErrorCode.Should().Be(ErrorCode.Success);

            Task.Delay(1000);

            //第三方有餘額先全部轉回主帳戶
            if (tpuserBalance.ErrorCode == ErrorCode.Success && tpuserBalance.Data.balance > 0)
            {
                var checkTransID = $"test{DateTime.Now.ToFileTime()}";
                var checkResult = _tpHelper.Transfer(_tpCustInfo, _inputs, tpuserBalance.Data.balance, Direction.ToMain, checkTransID, null);
                checkResult.ErrorCode.Should().Be(ErrorCode.Success);
                Task.Delay(1000);
            }

            //轉入第三方
            var toTPTransID = $"test{DateTime.Now.ToFileTime()}";
            var toTPResult = _tpHelper.Transfer(_tpCustInfo, _inputs, amount, Direction.ToTp, toTPTransID, null);
            toTPResult.ErrorCode.Should().Be(ErrorCode.Success);
            Task.Delay(1000);

            //檢查轉帳單狀態
            var checkTransferResult = _tpHelper.CheckTransfer(_tpCustInfo, _inputs, amount, Direction.ToTp, toTPTransID, null);
            checkTransferResult.ErrorCode.Should().Be(ErrorCode.Success);
            Task.Delay(1000);

            //檢查第三方餘額
            tpuserBalance = _tpHelper.GetBalance(_tpCustInfo, _inputs);
            tpuserBalance.ErrorCode.Should().Be(ErrorCode.Success);
            tpuserBalance.Data.balance.Should().Be(amount);
            Task.Delay(1000);

            //轉回一半餘額
            var toMainTransID = $"test{DateTime.Now.ToFileTime()}";
            var toMainResult = _tpHelper.Transfer(_tpCustInfo, _inputs, amount / 2, Direction.ToMain, toMainTransID, null);
            toMainResult.ErrorCode.Should().Be(ErrorCode.Success);
            Task.Delay(1000);

            //檢查轉帳單狀態
            checkTransferResult = _tpHelper.CheckTransfer(_tpCustInfo, _inputs, amount / 2, Direction.ToMain, toMainTransID, null);
            checkTransferResult.ErrorCode.Should().Be(ErrorCode.Success);
            Task.Delay(1000);

            //再次檢查第三方餘額
            tpuserBalance = _tpHelper.GetBalance(_tpCustInfo, _inputs);
            tpuserBalance.ErrorCode.Should().Be(ErrorCode.Success);
            tpuserBalance.Data.balance.Should().Be(amount / 2);

        }

        [Test()]
        public void LogoutTest()
        {
            //Act
            var actual = _tpHelper.Logout(_tpCustInfo, _inputs);

            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }

        [Test()]
        public void GetThirdIdTest()
        {
            //Act
            var actual = _tpHelper.GetThirdId(_tpCustInfo, _inputs);

            //Assert
            actual.Implemented.Should().Be(false);
        }

    }
}