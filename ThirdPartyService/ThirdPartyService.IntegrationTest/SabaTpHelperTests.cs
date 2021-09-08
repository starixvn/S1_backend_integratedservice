using NUnit.Framework;
using System.Collections.Generic;
using ThirdPartyCore.Model;
using FluentAssertions;
using ThirdPartyCore.Model.enums;
using Newtonsoft.Json;
using System.Web;
using System.Diagnostics;
using System;
using static ThirdParty.Saba.TpHelper;

namespace ThirdParty.Saba.Tests
{
    [TestFixture()]
    public class SabaTpHelperTests
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
                TpId = 1,
                CustId = 249951,
                SiteId = 1,
                UserName = "hanstest01",
                TpUserName = "DNova88_249951",
                TpPwd = "Hc55xs6",
                TpCurrencyId = "MYR",
                ExchangeRate = 1.0000m,
                IsTest = false,
                ActStatus = 2,
                Status = 1,
                CStatus = 1,
            };

            _inputs = new Dictionary<string, string>
            {
                { "ApiUrl", "https://tsa.n88dev.com/api" },
                { "NewApiUrl", "http://k024tsa.db5688.com/api" },
                { "VendorId", "aakx8ncolf" },
            };
        }

        [TestCase("", ExpectedResult = "ApiUrl")]
        [TestCase(null, ExpectedResult = "ApiUrl")]
        [TestCase("NewApiUrl", ExpectedResult = "NewApiUrl")]
        public string SabaConfig_Test(string newApiUrl)
        {
            _inputs = new Dictionary<string, string>
            {
                { "ApiUrl", "ApiUrl" },
                { "VendorId", "aakx8ncolf" },
            };

            if (!string.IsNullOrEmpty(newApiUrl))
            {
                _inputs.Add("NewApiUrl", newApiUrl);
            }
            
            var config = new SabaConfig(_inputs);
            return config.ApiUrl;
        }

        [TestCase(AppType.Desktop, ExpectedResult = ErrorCode.Success)]
        [TestCase(AppType.Mobile, ExpectedResult = ErrorCode.Success)]
        [TestCase(AppType.Native, ExpectedResult = ErrorCode.Fail)]
        public ErrorCode FunGame_LoginTest(AppType appType)
        {
            var actual = _tpHelper.Login(new TpCustInfo(), appType, tpLangId, _inputs);
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


    }
}