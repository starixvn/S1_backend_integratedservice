using NUnit.Framework;
using System.Collections.Generic;
using ThirdPartyCore.Model;
using FluentAssertions;
using ThirdPartyCore.Model.enums;
using Newtonsoft.Json;
using System.Web;
using System.Diagnostics;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using ThirdPartyService.Extensions;
using ThirdPartyService.IntegrationTest.Models;
using Microsoft.Extensions.Hosting;
using ThirdPartyCore;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;
using ThirdPartyService.IntegrationTest.Stubs;
using Microsoft.Extensions.Logging;
using ThirdPartyService.Service.Library;
using Microsoft.Extensions.Configuration;
using ThirdPartyService.Service.BLL;
using ThirdPartyService.Service.DAL;
using ThirdParty.TFG;

namespace ThirdParty.TFG.Tests
{
    [TestFixture()]
    public class TFGTpHelperTests
    {
        private TpHelper _tpHelper;
        private TpCustInfo _tpCustInfo;
        private Dictionary<string, string> _inputs;
        private IConfiguration _configuration;

        public TFGTpHelperTests()
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
            services.AddTransient(p => Substitute.For<ILogger<ThirdPartyBLL>>());
            services.AddTransient(p => Substitute.For<ILogger<ThirdPartyHelper>>());

            var startup = new StubStartUp(_configuration);

            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();
            ThirdPartyHelperBase.AuthProvider = provider.GetRequiredService<AuthBLL>();
            ThirdPartyHelperBase.TpCustInfoProvider = provider.GetRequiredService<TpCustInfoBLL>();
        }
        [SetUp]
        public void Setup()
        {
            //Arrange
            _tpHelper = new TpHelper();

            _tpCustInfo = new TpCustInfo()
            {
                TpId = 48,
                CustId = 644440,
                SiteId = 2,
                UserName = "willie1016",
                TpUserName = "DGoal123_644440",
                TpPwd = "1234567",
                TpCurrencyId = "VND",
                ExchangeRate = 1000.0000m,
                IsTest = false,
                ActStatus = 2,
                Status = 1,
                CStatus = 1,
            };

            _inputs = new Dictionary<string, string>
            {
                { "ApiUrl", "spi-test.r4espt.com" },
                { "RetrieveUrl_VND", "https://api-test-v4.diaoq.com/api/v4/launch/url/?auth=0a47dd19d91c40f0ae57c7cffe3c1905" },
                { "PublicToken_VND", "0a47dd19d91c40f0ae57c7cffe3c1905" },
                { "PartnerId_VND", "4253" },
                { "PrivateToken_VND", "00021b63e00da92648ff86c4cdff7ef1501079372548bcbc7bc5fd39012b4da4d11b0c2a55883e45c195bf1dc189974793a1620a69cdc6aaf80fb8b25c8fc8306bc95216327dc8696207578b18b8f85716e254a3df8514e3f84f2dbe2a1c7d40344966f7700f28b0dbba0f810a06dce3e5fc1af05041720657544360236e9b8ca763" },
                { "Token", "rQ55U0TMkkGfkq5hxirZCA"}
            };
        }

        [Test()]
        public void LoginTest()
        {
            //Arrange
            var appType = AppType.Desktop;
            var tpLangId = "en";

            //Act
            var actual = _tpHelper.Login(_tpCustInfo, appType, tpLangId, _inputs);

            //Assert
            actual.ErrorCode.Should().Be(ErrorCode.Success);
        }
    }
}
