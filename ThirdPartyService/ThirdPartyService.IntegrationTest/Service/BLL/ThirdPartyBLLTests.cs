using NUnit.Framework;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Microsoft.Extensions.Configuration;
using System.IO;
using ThirdPartyService.IntegrationTest.Stubs;
using ThirdPartyService.Service.Library;
using Microsoft.Extensions.Logging;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using Microsoft.AspNetCore.Hosting;
using ThirdPartyService.IntegrationTest.Models;
using ThirdPartyService.Service.DAL;

namespace ThirdPartyService.Service.BLL.Tests
{
    [TestFixture()]
    public class ThirdPartyBLLTests
    {
        private IConfiguration _configuration;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        public ThirdPartyBLLTests()
        {
            var services = new ServiceCollection();

            var configBuilder = new ConfigurationBuilder();
            configBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, false)
                .AddJsonFile("appsettings.Local.json", false, false);
            _configuration = configBuilder.Build();
            services.AddSingleton(_configuration);
            
            var env = new WebHostingEnvironment
            {
                EnvironmentName = Environments.Development,
            };
            services.AddSingleton<IWebHostEnvironment>(env);
            services.AddSingleton<IHostEnvironment>(env);

            services.AddTransient(p => Substitute.For<ILogger<MySqlClient>>());
            services.AddTransient(p => Substitute.For<ILogger<ThirdPartyBLL>>());
            services.AddTransient(p => Substitute.For<ILogger<ThirdPartyHelper>>());
            
            var startup = new StubStartUp(_configuration);

            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        [TestCase(250140, "2751880439", 7, ExpectedResult = ErrorCode.Success)]
        [TestCase(249667, "20199296008", 16, ExpectedResult = ErrorCode.Success)]
        public ErrorCode GetBetDetailTest(int custId, string transId, int gameType)
        {
            var actual = _thirdPartyBLL.GetBetDetail(new GetBetDetailRequest
            {
                CustId = custId,
                TransId = transId,
                GameType = gameType,
                SettledTime = DateTime.Now,
            });
            Console.WriteLine((actual.Data as BaseDetail).DetailUrl);
            return actual.ErrorCode;
        }
    }
}