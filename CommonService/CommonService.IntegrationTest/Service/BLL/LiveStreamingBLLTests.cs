using NUnit.Framework;
using CommonService.Service.BLL;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonService.IntegrationTest.Models;
using CommonService.IntegrationTest.Stubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using CommonService.Extensions;
using NSubstitute;
using CommonService.Service.Library;
using Microsoft.Extensions.Logging;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using CommonService.Service.DAL;

namespace CommonService.Service.BLL.Tests
{
    [TestFixture()]
    public class LiveStreamingBLLTests
    {
        private IConfiguration _configuration;
        private readonly LiveStreamingBLL _liveStreamingBLL;

        public LiveStreamingBLLTests()
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

            services.AddSingleton<IWebHostEnvironment>(env);
            services.AddSingleton<IHostEnvironment>(env);

            services.AddTransient(p => Substitute.For<ILogger<MySqlClient>>());
            services.AddTransient(p => Substitute.For<ILogger<HttpHelper>>());
            var startup = new StubStartUp(_configuration);

            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            _liveStreamingBLL = provider.GetRequiredService<LiveStreamingBLL>();
        }
        
        [TestCase("Nova88", ExpectedResult = ErrorCode.Success)]
        [TestCase("Goal123", ExpectedResult = ErrorCode.Success)]
        [TestCase("Bwing", ExpectedResult = ErrorCode.Success)]
        [Ignore("要測試再註解掉，")]
        public async Task<ErrorCode> GetLiveStreamingLinkAsyncTest(string siteName)
        {
            var liveStreamingInfo = new LiveStreamingLinkReq()
            {
                Ch = 754,
                SiteName = siteName,
                Uid = $"{siteName}_1234",
                Ip = "127.0.0.1"
            };

            var actual = await _liveStreamingBLL.GetLiveStreamingLink(liveStreamingInfo);
            return actual.ErrorCode;
        }
    }
}

