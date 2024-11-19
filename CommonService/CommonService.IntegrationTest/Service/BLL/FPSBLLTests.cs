using CommonService.Extensions;
using CommonService.IntegrationTest.Models;
using CommonService.IntegrationTest.Stubs;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NSubstitute;

using NUnit.Framework;

namespace CommonService.Service.BLL.Tests
{
    [TestFixture()]
    public class FPSBLLTests
    {
        private IConfiguration _configuration;
        private readonly FPSBLL _FPSBLL;

        public FPSBLLTests()
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

            services.AddSingleton<IWebHostEnvironment>(env);
            services.AddSingleton<IHostEnvironment>(env);

            services.AddTransient(p => Substitute.For<ILogger<MySqlClient>>());
            services.AddTransient(p => Substitute.For<ILogger<HttpHelper>>());
            var startup = new StubStartUp(_configuration);

            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            _FPSBLL = provider.GetRequiredService<FPSBLL>();
        }

        [TestCase("Nova88", ExpectedResult = ErrorCode.Success)]
        // [TestCase("Goal123", ExpectedResult = ErrorCode.Success)]
        // [TestCase("Bwing", ExpectedResult = ErrorCode.Success)]
        // [Ignore("要測試再註解掉，")]
        public async Task<ErrorCode> FPSChecksAsyncTest(string siteName)
        {
            //沒在正式找到相關參數
            //有空再找找
            var custInfoProfile = new CustInfoProfile
            {
                CurrencyId = 2,
                UserName = "Test",
                SiteName = siteName,
                UserLang = "en",
                AreaCode = "886",
                SiteId = 1,
                TpUserName = "Test"
            };

            var fpsChecksRequest = new FPSChecksRequest
            {
                Account = 12,
                DI = "123",
                BI = "123",
                IP = "127.0.0.1",
                UserAgent = "123",
                DetecasAnalysis = "{\"startTime\":1700560192636,\"version\":\"2.0.17\",\"exceptions\":[],\"executions\":[],\"storages\":[],\"devices\":[],\"enable\":true}",
                AcceptsTypes = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7",
                SessionKey = "123",
                Token = "123",
                Url = "http://www.n88dev.com/cs",
            };
            
            var actual = await _FPSBLL.FPSChecksAsync(custInfoProfile, fpsChecksRequest);
            return actual.ErrorCode;
        }
    }
}

