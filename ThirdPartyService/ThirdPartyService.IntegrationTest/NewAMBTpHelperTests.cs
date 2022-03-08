using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using ThirdPartyCore.Model;
using ThirdParty.NewAMB;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.NewAMB.Tests
{
    [TestFixture()]
    public class NewAMBTpHelperTests
    {
        private TpHelper _tpHelper;
        private TpCustInfo _tpCustInfo;
        private Dictionary<string, string> _inputs;
        
        [SetUp]
        public void Setup()
        {
            //Arrange
            _tpHelper = new TpHelper();
            _tpCustInfo = new TpCustInfo()
            {
                TpId = 42,
                CustId = 250764,
                SiteId = 1,
                UserName = "hulkchou4",
                TpUserName = "DNova88_250764",
                TpCurrencyId = "4",
                ExchangeRate = 1.0000m,
                IsTest = false,
                ActStatus = 2,
                Status = 1,
                CStatus = 1,
            };

            _inputs = new Dictionary<string, string>
            {
                {"Agent", "nova88thbtestaa"},
                {"ApiUrl", "https://uat.ambslot-api.com"},
                {"SecretKey", "1b15232c9933c8d031a68a98d14a2333"}
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
            var tpLangId = "th";
            _inputs.Add("gameId", "ANGRYBIRD");
            _inputs.Add("homeUrl", "http://www.n88dev.com");

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