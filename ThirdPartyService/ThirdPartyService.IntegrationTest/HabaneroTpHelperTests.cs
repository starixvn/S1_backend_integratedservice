using FluentAssertions;
using NUnit.Framework;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.HB.Tests
{
    [TestFixture()]
    public class HabaneroTpHelperTests
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
                TpId = 40,
                CustId = 250032,
                SiteId = 1,
                UserName = "hulkchou",
                TpUserName = "XNova88249951",
                TpCurrencyId = "2",
                ExchangeRate = 1.0000m,
                IsTest = false,
                ActStatus = 2,
                Status = 1,
                CStatus = 1,
            };

            _inputs = new Dictionary<string, string>
            {
                { "APIKey", "4797B756-8A77-4643-8DE5-2E4719203B46" },
                { "APIUrl", "https://ws-test.insvr.com" },
                { "BrandId", "11f9e966-8379-ea11-a94c-281878584619" },
                { "LaunchUrl", "https://app-test.insvr.com/go.ashx" },
            };
        }

        [Test]
        public void CreateMemberTest()
        {
            //Act
            var actual = _tpHelper.CreateMember(_tpCustInfo, _inputs);

            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }

        [Test]
        public void LoginTest()
        {
            //Arrange
            var appType = AppType.Desktop;
            var tpLangId = "cs";
            _inputs.Add("gameId", "SGGoldRush");

            //Act
            var actual = _tpHelper.Login(_tpCustInfo, appType, tpLangId, _inputs);

            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }

        [Test]
        public void GetBalanceTest()
        {
            var actual = _tpHelper.GetBalance(_tpCustInfo, _inputs);

            actual.ErrorCode.Should().Be(ErrorCode.Fail);
        }
    }
}