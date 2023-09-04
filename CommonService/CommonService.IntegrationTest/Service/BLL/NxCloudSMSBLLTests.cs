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
    public class NxCloudSMSBLLTests
    {
        private IConfiguration _configuration;
        private readonly NxCloudSMSBLL _nxCloudSMSBLL;

        public NxCloudSMSBLLTests()
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
            services.AddTransient(p => Substitute.For<ILogger<OTPSMSService>>());
            


            var startup = new StubStartUp(_configuration);

            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            _nxCloudSMSBLL = provider.GetRequiredService<NxCloudSMSBLL>();
        }

        [TestCase("+886912345678", ExpectedResult = ErrorCode.Success)] //使用要測試的號碼
        [Ignore("需要測試時在註解")]
        public async Task<ErrorCode> SendOTPSMSAsyncTest(string PhoneNo)
        {
            var custInfoProfile = new CustInfoProfile
            {
                CurrencyId = 2,
                UserName = "Test",
                SiteName = "Nova88",
                UserLang ="en",
                AreaCode = "886"
            };
            var custId = 250713;
            var userLang = "en";
            var actual = await _nxCloudSMSBLL.SendOTPSMSAsync(PhoneNo, "test", custInfoProfile, userLang, custId);
            return actual.ErrorCode;
        }
    }
}