using NUnit.Framework;
using System.Collections.Generic;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using System;
using static ThirdParty.SBSW.TpHelper;

namespace ThirdParty.SBSW.Tests
{
    [TestFixture()]
    public class SBSWTpHelperTests
    {
        private TpHelper _tpHelper;
        private TpCustInfo _tpCustInfo;
        private Dictionary<string, string> _inputs;
        private string tpLangId = "ch";

        [SetUp]
        public void Setup()
        {
            //Arrange
            _tpHelper = new TpHelper();

            _tpCustInfo = new TpCustInfo()
            {
                TpId = 41,
                CustId = 250713,
                SiteId = 1,
                UserName = "hanstest001",
                TpUserName = "DNova88_250713",
                TpPwd = "hKr82sJ",
                TpCurrencyId = "MYR",
                ExchangeRate = 1.0000m,
                IsTest = false,
                ActStatus = 2,
                Status = 1,
                CStatus = 1,
            };

            _inputs = new Dictionary<string, string>
            {
                { "ApiUrl", "http://k024tsa.db5688.com/api" },
                { "VendorId", "aakx8ncolf" },
                { "homeUrl","homeUrl"},
                { "signupUrl","signupUrl"},
                { "LoginUrl","LoginUrl"},
            };

        }

        [TestCase("ApiUrl", ExpectedResult = "ApiUrl")]
        public string SabaConfig_Test(string apiUrl)
        {
            _inputs = new Dictionary<string, string>
            {
                { "ApiUrl", apiUrl},
                { "VendorId", "aakx8ncolf" },
            };

            var config = new SabaConfig(_inputs);
            return config.ApiUrl;
        }

        [TestCase(AppType.Desktop, ExpectedResult = ErrorCode.Success)]
        [TestCase(AppType.Mobile, ExpectedResult = ErrorCode.Success)]
        [TestCase(AppType.Native, ExpectedResult = ErrorCode.Fail)]
        public ErrorCode FunGame_LoginTest(AppType appType)
        {
            var actual = _tpHelper.Login(new TpCustInfo(), appType, tpLangId, _inputs);
            Console.WriteLine(actual.Message);
            return actual.ErrorCode;
        }

        [TestCase(AppType.Desktop, ExpectedResult = ErrorCode.Fail)]
        [TestCase(AppType.Mobile, ExpectedResult = ErrorCode.Fail)]
        [TestCase(AppType.Native, ExpectedResult = ErrorCode.Fail)]
        public ErrorCode FunGame_WAP_LoginTest(AppType appType)
        {
            _inputs.Add("device", "wap");
            var actual = _tpHelper.Login(new TpCustInfo(), appType, tpLangId, _inputs);
            return actual.ErrorCode;
        }

        [TestCase(AppType.Desktop, "1", ExpectedResult = ErrorCode.Success)]
        [TestCase(AppType.Mobile, "1", ExpectedResult = ErrorCode.Success)]
        [TestCase(AppType.Native, "1", ExpectedResult = ErrorCode.Success)]
        [TestCase(AppType.Desktop, "43", ExpectedResult = ErrorCode.Success)]
        [TestCase(AppType.Mobile, "43", ExpectedResult = ErrorCode.Success)]
        [TestCase(AppType.Native, "43", ExpectedResult = ErrorCode.Success)]
        [TestCase(AppType.Desktop, "997", ExpectedResult = ErrorCode.Success)]
        [TestCase(AppType.Mobile, "997", ExpectedResult = ErrorCode.Success)]
        [TestCase(AppType.Native, "997", ExpectedResult = ErrorCode.Success)]
        public ErrorCode LoginTest(AppType appType, string gameId)
        {
            _inputs.Add("gameId", gameId);
            var actual = _tpHelper.Login(_tpCustInfo, appType, tpLangId, _inputs);
            Console.WriteLine(actual.Message);
            return actual.ErrorCode;
        }
    }
}