using FluentAssertions;
using NUnit.Framework;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ThirdPartyCore;
using ThirdPartyService.Extensions;
using ThirdPartyService.IntegrationTest.Models;
using ThirdPartyService.IntegrationTest.Stubs;
using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Library;

namespace ThirdParty.AMB.Tests
{
    [TestFixture()]
    public class AMBTpHelperTests
    {
        private TpHelper _tpHelper;
        private TpCustInfo _tpCustInfo;
        private AMBConfig _inputs;
        private Dictionary<string, string> _loginDic;
        private IConfiguration _configuration;
        private IServiceProvider _provider;

        public AMBTpHelperTests()
        {
            var services = new ServiceCollection();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "Local",
            });

            builder.Configuration.AddRemoteJsonConfig(builder.Configuration.GetSection("ConfigService").Get<string[]>());
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

            //THB
            _tpCustInfo = new TpCustInfo()
            {
                TpId = 30,
                CustId = 250764,
                SiteId = 1,
                UserName = "hulkchou4",
                TpUserName = "DNova88_250764",
                TpCurrencyId = "THB",
                ExchangeRate = 1.0000m,
                IsTest = false,
                ActStatus = 2,
                Status = 1,
                CStatus = 1,
            };

            //Anthoer
            // _tpCustInfo = new TpCustInfo()
            // {
            //     TpId = 30,
            //     CustId = 650159,
            //     SiteId = 1,
            //     UserName = "novatestcny001",
            //     TpUserName = "DNova88_650159",
            //     TpCurrencyId = "CNY",
            //     ExchangeRate = 1.0000m,
            //     IsTest = false,
            //     ActStatus = 2,
            //     Status = 1,
            //     CStatus = 1,
            // };
            _inputs = new AMBConfig();
            _inputs.PokerCurrencyData.Add("THB", new CurrencyDataAMB
            {
                CurrencyId = "THB",
                Agent = "nova88thbuataa",
                ApiUrl = "https://api-uat.ambpoker-api.com",
                SecretKey = "89659806ccc97ecb456e97755cb0c0f4",
            });
            _inputs.SlotCurrencyData.Add("USD", new CurrencyDataAMB
            {
                CurrencyId = "USD",
                Agent = "nova88usdtestaa",
                ApiUrl = "https://uat.ambslot-api.com",
                SecretKey = "f7f4b07890fe30516cf11e643d44cc74",
            });
            _inputs.SlotCurrencyData.Add("THB", new CurrencyDataAMB
            {
                CurrencyId = "THB",
                Agent = "nova88thbtestaa",
                ApiUrl = "https://uat.ambslot-api.com",
                SecretKey = "1b15232c9933c8d031a68a98d14a2333",
            });
            _inputs.SlotCurrencyData.Add("CNY", new CurrencyDataAMB
            {
                CurrencyId = "CNY",
                Agent = "nova88cnytestaa",
                ApiUrl = "https://uat.ambslot-api.com",
                SecretKey = "7ad72d245fd206c470cb1d819d2497a2",
            });

            _loginDic = new Dictionary<string, string>
            {
                { "tpGameType", "76" },
                { "gameId", "ANGRYBIRD" },
                { "homeUrl", "http://www.n88dev.com" }
            };
        }

        [Test]
        public void CreateMemberTest()
        {
            var actual = _tpHelper.CreateMember(_tpCustInfo, null);
            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }

        [Test]
        public void CreatePokerMember()
        {
            var actual = _tpHelper.CreateMember(_tpCustInfo, null);
            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }

        [Test]
        public void CreateSlotMember()
        {
            var actual = _tpHelper.CreateMember(_tpCustInfo, null);
            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }

        [Test]
        public void CreatePokerAndSlotMember()
        {
            var actual = _tpHelper.CreateMember(_tpCustInfo, null);
            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }

        // 更新為新的方式後不可能沒設定 先註解
        // [Test]
        // public void CreateMemberButNoSetting()
        // {
        //     var actual = _tpHelper.CreateMember(_tpCustInfo, null);
        //     //Assert
        //     actual.ErrorCode.Should().Be(ErrorCode.SystemError);
        // }


        [Test]
        public void LoginTest()
        {
            //Arrange
            var appType = AppType.Desktop;
            var tpLangId = "th";

            //Act
            var actual = _tpHelper.Login(_tpCustInfo, appType, tpLangId, _loginDic);

            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }

        [Test]
        public void GetBalanceTest()
        {
            var actual = _tpHelper.GetBalance(_tpCustInfo, null);

            actual.ErrorCode.Should().Be(ErrorCode.Fail);
        }
    }
}