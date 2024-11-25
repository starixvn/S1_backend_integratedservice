using System.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using ThirdParty.TCG;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Extensions;
using ThirdPartyService.IntegrationTest.Models;
using ThirdPartyService.IntegrationTest.Stubs;
using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.IntegrationTest
{
    [TestFixture()]
    public class TCGTpHelperTests
    {
        private TpHelper _tpHelper;
        private TpCustInfo _tpCustInfo;
        private Dictionary<string, string> _inputs;

        private IConfiguration _configuration;
        private IServiceProvider _provider;


        public TCGTpHelperTests()
        {
            var services = new ServiceCollection();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "LOCAL",
            });

            var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
            remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, new[] { "Nova" }, remoteUrls);
            builder.Configuration.AddRemoteJsonConfig(remoteUrls);

            var app = builder.Build();
            _configuration = app.Configuration;
            services.AddSingleton(_configuration);

            var env = new WebHostingEnvironment
            {
                EnvironmentName = Environments.Development,
                ContentRootPath = AppDomain.CurrentDomain.BaseDirectory,
            };
            ThirdPartyHelperBase.IsDevelopment = env.IsDevelopment();

            services.AddSingleton<IWebHostEnvironment>(env);
            services.AddSingleton<IHostEnvironment>(env);

            services.AddTransient(p => Substitute.For<ILogger<MySqlClient>>());
            services.AddTransient(p => Substitute.For<ILogger<ThirdPartyHelper>>());

            var startup = new StubStartUp(_configuration);

            startup.ConfigureServices(services);
            _provider = services.BuildServiceProvider();
        }

        [SetUp]
        public void Setup()
        {
            //Arrange
            _tpHelper = new TpHelper(_provider);

            //_tpCustInfo = new TpCustInfo()
            //{
            //    TpId = 33,
            //    CustId = 249951,
            //    SiteId = 1,
            //    UserName = "hanstest01",
            //    TpUserName = "xnova88x249951",
            //    TpPwd = "h5N3Bjn",
            //    TpCurrencyId = "MYR",
            //    ExchangeRate = 1.0000m,
            //    IsTest = false,
            //    ActStatus = 2,
            //    Status = 1,
            //    CStatus = 1,
            //};

            //_inputs = new Dictionary<string, string>
            //{
            //    { "GameUrl_MYR", "https://tcgmyr.nova88asia.com/" },
            //    { "ApiUrl", "http://www.connect6play.com/doBusiness.do" },
            //    { "HomeUrl", "https://www.nova88.net/" },
            //    { "MerchantCode_MYR", "xpnovamyr" },
            //    { "DESKey_MYR", "dHJoyySB" },
            //    { "SHA256Key_MYR", "70tOVKomTbE5booC" },
            //};
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

        [Test()]
        public void DesEncryptTest()
        {
            //Arrange
            //var sParam = new
            //{
            //    method = "cm",
            //    username = _tpCustInfo.TpUserName,
            //    password = _tpCustInfo.TpPwd,
            //    currency = _tpCustInfo.TpCurrencyId
            //};

            var sParam = new
            {
                method = "sgb",
                username = _tpCustInfo.TpUserName,
                product_type = "384",//420,
                request_time = DateTime.Now.ToFileTime(),
            };

            var merchantCode = _inputs["MerchantCode_MYR"];
            var desKey = _inputs["DESKey_MYR"];
            var SignKey = _inputs["SHA256Key_MYR"];
            var hashString = ToJson(sParam);

            //Act
            var desEncrypt = _tpHelper.DesEncrypt(hashString, desKey);
            var sign = _tpHelper.SHA256($"{desEncrypt}{SignKey}");

            var requestBody = $"?merchant_code={HttpUtility.UrlEncode(merchantCode)}&params={HttpUtility.UrlEncode(desEncrypt)}&sign={HttpUtility.UrlEncode(sign)}";

            //Assert
            var result = _tpHelper.DesDecrypt(desEncrypt, desKey);
            result.Should().Be(hashString);


            var resultdesEncrypt = _tpHelper.DesEncrypt(result, desKey);
            var checkSing = _tpHelper.SHA256($"{resultdesEncrypt}{SignKey}");
            sign.Should().Be(checkSing);
        }


        private string ToJson(object t)
        {
            return JsonConvert.SerializeObject(t, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }
    }
}