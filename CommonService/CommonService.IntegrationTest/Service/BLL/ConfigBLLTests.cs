using NUnit.Framework;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using CommonService.Extensions;
using CommonService.IntegrationTest.Models;
using CommonService.IntegrationTest.Stubs;
using NSubstitute;
using CommonService.Service.Model;
using CommonService.Service.Library;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CommonService.Service.BLL.Tests
{
    [TestFixture()]
    public class ConfigBLLTests
    {
        private IConfiguration _configuration;
        private readonly ConfigBLL _configBLL;

        public ConfigBLLTests()
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

            var startup = new StubStartUp(_configuration);

            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            _configBLL = provider.GetRequiredService<ConfigBLL>();
        }

        [TestCase("Nova88", ExpectedResult = "svTUT8d0")]
        [TestCase("NOVA88", ExpectedResult = "svTUT8d0")]
        [TestCase("nova88", ExpectedResult = "svTUT8d0")]
        [TestCase("Goal123", ExpectedResult = "RPQZO7FL")]
        [TestCase("Bwing", ExpectedResult = "JHvVDPa4")]
        [TestCase("Ivorysport", ExpectedResult = "svTUT8d0")]
        [TestCase("Leobet", ExpectedResult = "svTUT8d0")]
        public string GetConfigValuesBySiteNameTest(string siteName)
        {
            var actual = _configBLL.GetConfigValuesBySiteName<NxcloudSMSServiceTypeConfig>("NxCloud");
            return actual[siteName].OTP.Appkey;
        }

        [TestCase(1, ExpectedResult = "svTUT8d0")]
        [TestCase(2, ExpectedResult = "RPQZO7FL")]
        [TestCase(3, ExpectedResult = "JHvVDPa4")]
        [TestCase(5, ExpectedResult = "svTUT8d0")]
        [TestCase(6, ExpectedResult = "svTUT8d0")]
        public string GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<NxcloudSMSServiceTypeConfig>("NxCloud");
            return actual[siteId].OTP.Appkey;
        }

        [TestCase(1, ExpectedResult = "svTUT8d0")]
        [TestCase(2, ExpectedResult = "RPQZO7FL")]
        [TestCase(3, ExpectedResult = "JHvVDPa4")]
        [TestCase(5, ExpectedResult = "svTUT8d0")]
        [TestCase(6, ExpectedResult = "svTUT8d0")]
        public string GetConfigValueBySiteIdTest(int siteId)
        {
            var json = _configBLL.GetConfigValue(siteId, "NxCloud");
            var actual = JsonConvert.DeserializeObject<NxcloudSMSServiceTypeConfig>(json);
            return actual.OTP.Appkey;
        }

        [TestCase("Nova88", ExpectedResult = "svTUT8d0")]
        [TestCase("NOVA88", ExpectedResult = "svTUT8d0")]
        [TestCase("nova88", ExpectedResult = "svTUT8d0")]
        [TestCase("Goal123", ExpectedResult = "RPQZO7FL")]
        [TestCase("Bwing", ExpectedResult = "JHvVDPa4")]
        [TestCase("Ivorysport", ExpectedResult = "svTUT8d0")]
        [TestCase("Leobet", ExpectedResult = "svTUT8d0")]
        public string GetConfigValueBySiteNameTest(string siteName)
        {
            var json = _configBLL.GetConfigValue(siteName, "NxCloud");
            var actual = JsonConvert.DeserializeObject<NxcloudSMSServiceTypeConfig>(json);
            return actual.OTP.Appkey;
        }
    }
}