using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ThirdParty.AP;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdPartyService.IntegrationTest
{
    [TestFixture()]
    public class APTpHelperTests
    {
        private TpHelper _tpHelper;
        private TpCustInfo _tpCustInfo;
        private Dictionary<string, string> _inputs;
        private IServiceProvider _provider;

        public APTpHelperTests()
        {
            var services = new ServiceCollection();
            _provider = services.BuildServiceProvider();
        }

        [SetUp]
        public void Setup()
        {
            //Arrange
            _tpHelper = new TpHelper(_provider);
            _tpCustInfo = new TpCustInfo()
            {
                TpId = 39,
                CustId = 250072,
                SiteId = 1,
                UserName = "willie1016",
                TpUserName = "nova88_willie1016",
                TpPwd = "!12345678",
                TpCurrencyId = "MYR",
                ExchangeRate = 1.0000m,
                IsTest = false,
                ActStatus = 2,
                Status = 1,
                CStatus = 1,
            };
            _inputs = new Dictionary<string, string>
            {
                { "WebUrl", "https://sit.pypc.net/web/nova88/" },
                { "SiteCode", "nova88" },
                { "LangId", "en-US" },
                { "gameId", "10001" },
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

            //Act
            var actual = _tpHelper.Login(_tpCustInfo, appType, tpLangId, _inputs);

            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }
    }
}