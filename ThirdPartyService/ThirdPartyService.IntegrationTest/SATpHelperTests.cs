using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ThirdParty.SA;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdPartyService.IntegrationTest
{
    [TestFixture()]
    public class SabaTpHelperTests
    {
        private TpHelper _tpHelper;
        private TpCustInfo _tpCustInfo;
        private Dictionary<string, string> _inputs;
        private string tpLangId = "ch";
        private IServiceProvider _provider;

        public SabaTpHelperTests()
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
                TpId = 2,
                CustId = 649735,
                SiteId = 1,
                UserName = "lorita",
                TpUserName = "DNova88x649735",
                TpPwd = "NVl6WSG",
                TpCurrencyId = "MYR",
                ExchangeRate = 1.0000m,
                IsTest = false,
                ActStatus = 2,
                Status = 1,
                CStatus = 1,
            };

            _inputs = new Dictionary<string, string>
            {
                { "ApiUrl", "http://sai-api.sa-apisvr.com/api/api.aspx" },
                { "SecretKey", "30DB5B39856A472EAC78F4579D77D7ED" },
                { "EncrypKey", "g9G16nTs" },
                { "MD5Key","GgaIMaiNNtg"},
                { "GameUrl","https://www.sai.slgaming.net/app.aspx"},
            };

        }



        [TestCase(AppType.Desktop, "1", ExpectedResult = ErrorCode.Success)]
        public ErrorCode LoginTest(AppType appType, string gameId)
        {
            _inputs.Add("gameId", gameId);
            var actual = _tpHelper.Login(_tpCustInfo, appType, tpLangId, _inputs);
            Console.WriteLine(actual.Message);
            return actual.ErrorCode;
        }
    }
}