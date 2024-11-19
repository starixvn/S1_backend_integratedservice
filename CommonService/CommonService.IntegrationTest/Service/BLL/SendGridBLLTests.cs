using CommonService.Extensions;
using CommonService.IntegrationTest.Models;
using CommonService.IntegrationTest.Stubs;
using CommonService.Service.DAL;
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
    public class SendGridBLLTests
    {
        private IConfiguration _configuration;
        private readonly SendGridBLL _sendGridBLL;

        public SendGridBLLTests()
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
            services.AddTransient(p => Substitute.For<ILogger<OTPSMSService>>());
            var startup = new StubStartUp(_configuration);

            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            _sendGridBLL = provider.GetRequiredService<SendGridBLL>();
        }

        [TestCase("test@zbock.com", ExpectedResult = ErrorCode.Success)]
        [Ignore("要測試再註解掉，")]
        public async Task<ErrorCode> SendEMAILAsyncTest(string mail)
        {
            var custInfoProfile = new CustInfoProfile
            {
                CurrencyId = 2,
                UserName = "Test",
                SiteName = "Nova88",
                UserLang = "en",
                Email = "test@zbock.com",
                AreaCode = "886"
            };

            var actual = await _sendGridBLL.SendMailAsync(custInfoProfile, null, TemplateType.WelcomeMail);
            return actual.ErrorCode;
        }
    }
}

