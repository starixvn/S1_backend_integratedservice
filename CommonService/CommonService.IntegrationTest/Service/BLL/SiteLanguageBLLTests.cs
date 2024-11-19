using CommonService.Extensions;
using CommonService.IntegrationTest.Models;
using CommonService.IntegrationTest.Stubs;
using CommonService.Service.Library;

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
    public class SiteLanguageBLLTests
    {
        private IConfiguration _configuration;
        private readonly SiteLanguageBLL _siteLanguageBLL;

        public SiteLanguageBLLTests()
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

            var startup = new StubStartUp(_configuration);

            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            _siteLanguageBLL = provider.GetRequiredService<SiteLanguageBLL>();
        }


        [TestCase(1, "SMSProviderFail", "en", ExpectedResult = "Request OTP failed , please contact customer support")]
        [TestCase(1, "SMSProviderFail", "jp", ExpectedResult = "Request OTP failed , please contact customer support")]
        [TestCase(1, "SMSProviderFail", "cs", ExpectedResult = "无法获取OTP，请联系在线客服")]
        [TestCase(1, "SMSProviderFail", "id", ExpectedResult = "Permintaan OTP gagal, silakan hubungi layanan pelanggan")]
        [TestCase(2, "SMSProviderFail", "en", ExpectedResult = "Request OTP failed , please contact customer support")]
        [TestCase(-1, "SMSProviderFail", "test", ExpectedResult = "Request OTP failed , please contact customer support")]
        [TestCase(-1, "x", "en", ExpectedResult = "")]
        [TestCase(-1, "x", "test", ExpectedResult = "")]

        public string GetLanguageValueTest(int siteId, string languageKey, string langId)
        {
            return _siteLanguageBLL.GetLanguageValue(siteId, languageKey, langId);
        }


    }
}