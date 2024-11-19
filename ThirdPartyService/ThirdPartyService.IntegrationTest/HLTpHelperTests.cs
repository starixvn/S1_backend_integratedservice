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
using ThirdParty.HL;
using ThirdPartyCore;
using ThirdPartyService.Extensions;
using ThirdPartyService.IntegrationTest.Models;
using ThirdPartyService.IntegrationTest.Stubs;
using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.IntegrationTest
{
    [TestFixture()]
    public class HLTpHelperTests
    {
        private TpHelper _tpHelper;
        private TpCustInfo _tpCustInfo;
        private HLConfig _inputs;
        private Dictionary<string, string> _loginDic;
        private IConfiguration _configuration;
        private IServiceProvider _provider;
        
        public HLTpHelperTests()
        {
            var services = new ServiceCollection();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "LOCAL",
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
            
            //MYR ONLY
            _tpCustInfo = new TpCustInfo()
            {
                TpId = 58,
                CustId = 650133,
                SiteId = 1,
                UserName = "novatest002",
                TpUserName = "DNova88_650133",
                TpCurrencyId = "MYR",
                ExchangeRate = 1.0000m,
                IsTest = false,
                ActStatus = 2,
                Status = 1,
                CStatus = 1,
            };
            
            _inputs = new HLConfig()
            {
                ApiUrl = "https://api.coba8.com/",
                MobileGameUrl = "https://wap.coba8.com/",
                ApiUser = "bmq",
                ApiPass = "K6s1DwBV",
                AgentUser = "bmq",
                AgentPass = "123789"
            };

            _loginDic = new Dictionary<string, string>
            {
                
            };
        }
        
        [Test()]
        public void CreateMemberTest()
        {
            //Act
            var actual = _tpHelper.CreateMember(_tpCustInfo, null);

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
            var actual = _tpHelper.Login(_tpCustInfo, appType, tpLangId, null);

            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }

        [Test()]
        public void GetBalanceTest()
        {
            //Act
            var actual = _tpHelper.GetBalance(_tpCustInfo, null);

            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }
    }
}