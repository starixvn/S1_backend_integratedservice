using NUnit.Framework;
using System.Collections.Generic;
using ThirdPartyCore.Model;
using FluentAssertions;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.TCG.Tests
{
    [TestFixture()]
    public class TCGTpHelperTests
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
                TpId = 33,
                CustId = 249951,
                SiteId = 1,
                UserName = "hanstest01",
                TpUserName = "XNova88249951",
                TpPwd = "h5N3Bjn",
                TpCurrencyId = "MYR",
                ExchangeRate = 1.0000m,
                IsTest = false,
                ActStatus = 2,
                Status = 1,
                CStatus = 1,
            };

            _inputs = new Dictionary<string, string>
            {
                { "GameUrl", "https://tcg.nova88asia.com/" },
                { "ApiUrl", "http://www.connect6play.com/doBusiness.do" },
                { "homeUrl", "https://www.nova88.net/" },
                { "MerchantCode_MYR", "xpnovamyr" },
                { "DESKey_MYR", "dHJoyySB" },
                { "SHA256Key_MYR", "70tOVKomTbE5booC" },
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
            _inputs.Add("gameType", "54");
            _inputs.Add("GameId", "0");

            //Act
            var actual = _tpHelper.Login(_tpCustInfo, appType, tpLangId, _inputs);

            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }

    }
}