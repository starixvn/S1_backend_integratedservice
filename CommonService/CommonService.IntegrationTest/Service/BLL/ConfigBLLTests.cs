using CommonService.Extensions;
using CommonService.IntegrationTest.Models;
using CommonService.IntegrationTest.Stubs;
using CommonService.Service.Library;
using CommonService.Service.Model;

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
    public class ConfigBLLTests
    {
        private IConfiguration _configuration;
        private readonly ConfigBLL _configBLL;

        public ConfigBLLTests()
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

            _configBLL = provider.GetRequiredService<ConfigBLL>();
        }

        [TestCase(1, ExpectedResult = "svTUT8d0")]
        [TestCase(2, ExpectedResult = "RPQZO7FL")]
        [TestCase(3, ExpectedResult = "JHvVDPa4")]
        [TestCase(5, ExpectedResult = "svTUT8d0")]
        public string GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<NxcloudSMSServiceTypeConfig>("NxCloud");
            return actual[siteId].OTP.Appkey;
        }

        #region SendGrid

        [TestCase(1, ExpectedResult = "https://www.nova88.net/Home/ResetPassword")]
        [TestCase(2, ExpectedResult = "https://www.goal123.com/Home/ResetPassword")]
        [TestCase(3, ExpectedResult = "https://www.bwing.com/Home/ResetPassword")]
        [TestCase(5, ExpectedResult = "https://www.nova88.net/Home/ResetPassword")]
        public string SendGrid_GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<SendGridConfig>("SendGrid");
            return actual[siteId].ResetPasswordUrl;
        }

        #endregion

        #region FPS

        [TestCase(1, ExpectedResult = "N88")]
        [TestCase(2, ExpectedResult = "N8G")]
        [TestCase(3, ExpectedResult = "N8B")]
        [TestCase(5, ExpectedResult = "N88")]
        public string FPS_GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<FPSServicesConfig>("FPS");
            return actual[siteId].Subscriber;
        }

        #endregion

        #region LiveStreaming

        [TestCase("Nova88", ExpectedResult = "nvc")]
        [TestCase("NOVA88", ExpectedResult = "nvc")]
        [TestCase("nova88", ExpectedResult = "nvc")]
        [TestCase("Goal123", ExpectedResult = "nvcg")]
        [TestCase("Bwing", ExpectedResult = "nvcb")]
        [TestCase("Ivorysport", ExpectedResult = "nvc")]
        public string LiveStreaming_GetConfigValuesBySiteNameTest(string siteName)
        {
            var actual = _configBLL.GetConfigValuesBySiteName<LiveStreamingConfig>("LiveStreaming");
            return actual[siteName].Apiuser;
        }

        [TestCase(1, ExpectedResult = "nvc")]
        [TestCase(2, ExpectedResult = "nvcg")]
        [TestCase(3, ExpectedResult = "nvcb")]
        [TestCase(5, ExpectedResult = "nvc")]
        public string LiveStreaming_GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<LiveStreamingConfig>("LiveStreaming");
            return actual[siteId].Apiuser;
        }

        #endregion

        #region Iovation

        [TestCase(1, ExpectedResult = "1054302")]
        [TestCase(2, ExpectedResult = "1054302")]
        [TestCase(3, ExpectedResult = "1054302")]
        [TestCase(5, ExpectedResult = "1054302")]
        public string Iovation_GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<IovationServicesConfig>("Iovation");
            return actual[siteId].SubscriberID;
        }

        #endregion

        #region AWSKey

        //由於AWSKeyConfig只有一組，測兩個會把Key直接顯示，先拿掉，有需要測試的人請到DB自行搜尋填入。

        [TestCase(1, ExpectedResult = "AKIA3CXJE5UPS5ARGOYZ")]
        [TestCase(2, ExpectedResult = "AKIA3CXJE5UPS5ARGOYZ")]
        [TestCase(3, ExpectedResult = "AKIA3CXJE5UPS5ARGOYZ")]
        [TestCase(5, ExpectedResult = "AKIA3CXJE5UPS5ARGOYZ")]
        public string AWS_GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<AWSKeyConfig>("AWSKey");
            return actual[siteId].AccessKey;
        }


        #endregion

        #region Telegram

        [TestCase(1, ExpectedResult = -594288926)]
        [TestCase(2, ExpectedResult = -594288926)]
        [TestCase(3, ExpectedResult = -594288926)]
        [TestCase(5, ExpectedResult = -594288926)]
        public long Telegram_GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<TelegramConfig>("Telegram");
            return actual[siteId].TelegramNotification.FirstOrDefault().ChatId;
        }


        #endregion

        #region LetsTalk

        [TestCase(1, ExpectedResult = 100044)]
        [TestCase(2, ExpectedResult = 100044)]
        [TestCase(3, ExpectedResult = 100044)]
        [TestCase(5, ExpectedResult = 100044)]
        public int LetsTalk_GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<LetsTalkConfig>("LetsTalk");
            return actual[siteId].LetsTalkNotification.FirstOrDefault().SignInId;
        }

        #endregion

        #region SMSSupport

        [TestCase(1, ExpectedResult = "NxCloud")]
        [TestCase(2, ExpectedResult = "NxCloud")]
        [TestCase(3, ExpectedResult = "NxCloud")]
        [TestCase(5, ExpectedResult = "NxCloud")]
        public string SMSSupport_GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<SMSSupportConfig>("SMSAutoSupport");
            return actual[siteId].SMSSupport["Default"].FirstOrDefault().ToString();
        }


        #endregion
    }
}