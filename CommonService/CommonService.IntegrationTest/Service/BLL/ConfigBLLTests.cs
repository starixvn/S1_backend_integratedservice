using NUnit.Framework;
using CommonService.Service.BLL;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using CommonService.Extensions;
using CommonService.IntegrationTest.Models;
using CommonService.IntegrationTest.Stubs;
using NSubstitute;
using CommonService.Service.Model.enums;
using CommonService.Service.Model;

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

            var startup = new StubStartUp(_configuration);

            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            _configBLL = provider.GetRequiredService<ConfigBLL>();
        }

        [TestCase("Nova88", ExpectedResult = "svTUT8d0")]
        [TestCase("Goal123", ExpectedResult = "RPQZO7FL")]
        [TestCase("Bwing", ExpectedResult = "JHvVDPa4")]
        [TestCase("Ivorysport", ExpectedResult = "svTUT8d0")]
        [TestCase("Leobet", ExpectedResult = "svTUT8d0")]
        public string GetConfigValuesBySiteNameTest(string siteName)
        {
            var actual = _configBLL.GetConfigValuesBySiteName<NxcloudSMSServiceTypeConfig>("NxCloud");
            return actual[siteName].OTP.Appkey;
        }
    }
}