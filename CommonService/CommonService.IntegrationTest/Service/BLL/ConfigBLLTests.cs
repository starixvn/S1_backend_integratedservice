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

        #region SendGrid
        [TestCase("Nova88", ExpectedResult = "noreply@nova88service.org")]
        [TestCase("NOVA88", ExpectedResult = "noreply@nova88service.org")]
        [TestCase("nova88", ExpectedResult = "noreply@nova88service.org")]
        [TestCase("Goal123", ExpectedResult = "noreply@goal123service.com")]
        [TestCase("Bwing", ExpectedResult = "noreply@bwingservice.com")]
        [TestCase("Ivorysport", ExpectedResult = "noreply@nova88service.org")]
        [TestCase("Leobet", ExpectedResult = "noreply@nova88service.org")]
        public string SendGrid_GetConfigValuesBySiteNameTest(string siteName)
        {
            var actual = _configBLL.GetConfigValuesBySiteName<SendGridConfig>("SendGrid");
            return actual[siteName].mailFrom.email;
        }
        
        [TestCase(1, ExpectedResult = "https://www.nova88.net/Home/ResetPassword")]
        [TestCase(2, ExpectedResult = "https://www.goal123.com/Home/ResetPassword")]
        [TestCase(3, ExpectedResult = "https://www.bwing.com/Home/ResetPassword")]
        [TestCase(5, ExpectedResult = "https://www.nova88.net/Home/ResetPassword")]
        [TestCase(6, ExpectedResult = "https://www.nova88.net/Home/ResetPassword")]
        public string SendGrid_GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<SendGridConfig>("SendGrid");
            return actual[siteId].ResetPasswordUrl;
        }
        #endregion

        #region FPS

        [TestCase("Nova88", ExpectedResult = "N88")]
        [TestCase("NOVA88", ExpectedResult = "N88")]
        [TestCase("nova88", ExpectedResult = "N88")]
        [TestCase("Goal123", ExpectedResult = "N8G")]
        [TestCase("Bwing", ExpectedResult = "N8B")]
        [TestCase("Ivorysport", ExpectedResult = "N88")]
        [TestCase("Leobet", ExpectedResult = "N88")]
        public string FPS_GetConfigValuesBySiteNameTest(string siteName)
        {
            var actual = _configBLL.GetConfigValuesBySiteName<FPSServicesConfig>("FPS");
            return actual[siteName].Subscriber;
        }
        
        [TestCase(1, ExpectedResult = "N88")]
        [TestCase(2, ExpectedResult = "N8G")]
        [TestCase(3, ExpectedResult = "N8B")]
        [TestCase(5, ExpectedResult = "N88")]
        [TestCase(6, ExpectedResult = "N88")]
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
        [TestCase("Leobet", ExpectedResult = "nvc")]
        public string LiveStreaming_GetConfigValuesBySiteNameTest(string siteName)
        {
            var actual = _configBLL.GetConfigValuesBySiteName<LiveStreamingConfig>("LiveStreaming");
            return actual[siteName].Apiuser;
        }
        
        [TestCase(1, ExpectedResult = "nvc")]
        [TestCase(2, ExpectedResult = "nvcg")]
        [TestCase(3, ExpectedResult = "nvcb")]
        [TestCase(5, ExpectedResult = "nvc")]
        [TestCase(6, ExpectedResult = "nvc")]
        public string LiveStreaming_GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<LiveStreamingConfig>("LiveStreaming");
            return actual[siteId].Apiuser;
        }

        #endregion

        #region Iovation

        [TestCase("Nova88", ExpectedResult = "1054302")]
        [TestCase("NOVA88", ExpectedResult = "1054302")]
        [TestCase("nova88", ExpectedResult = "1054302")]
        [TestCase("Goal123", ExpectedResult = "1054302")]
        [TestCase("Bwing", ExpectedResult = "1054302")]
        [TestCase("Ivorysport", ExpectedResult = "1054302")]
        [TestCase("Leobet", ExpectedResult = "1054302")]
        public string Iovation_GetConfigValuesBySiteNameTest(string siteName)
        {
            var actual = _configBLL.GetConfigValuesBySiteName<IovationServicesConfig>("Iovation");
            return actual[siteName].SubscriberID;
        }
        
        [TestCase(1, ExpectedResult = "1054302")]
        [TestCase(2, ExpectedResult = "1054302")]
        [TestCase(3, ExpectedResult = "1054302")]
        [TestCase(5, ExpectedResult = "1054302")]
        [TestCase(6, ExpectedResult = "1054302")]
        public string  Iovation_GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<IovationServicesConfig>("Iovation");
            return actual[siteId].SubscriberID;
        }

        #endregion

        #region AWSKey
        //由於AWSKeyConfig只有一組，測兩個會把Key直接顯示，先拿掉，有需要測試的人請到DB自行搜尋填入。
        [TestCase("Nova88", ExpectedResult = "")]
        [TestCase("NOVA88", ExpectedResult = "")]
        [TestCase("nova88", ExpectedResult = "")]
        [TestCase("Goal123", ExpectedResult = "")]
        [TestCase("Bwing", ExpectedResult = "")]
        [TestCase("Ivorysport", ExpectedResult = "")]
        [TestCase("Leobet", ExpectedResult = "")]
        public string AWS_GetConfigValuesBySiteNameTest(string siteName)
        {
            var actual = _configBLL.GetConfigValuesBySiteName<AWSKeyConfig>("AWSKey");
            //return actual[siteName].AccessKey;
            return "";
        }
        
        [TestCase(1, ExpectedResult = "")]
        [TestCase(2, ExpectedResult = "")]
        [TestCase(3, ExpectedResult = "")]
        [TestCase(5, ExpectedResult = "")]
        [TestCase(6, ExpectedResult = "")]
        public string  AWS_GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<AWSKeyConfig>("AWSKey");
            //return actual[siteId].SecretKey;
            return "";
        }
        

        #endregion

        #region Telegram

        [TestCase("Nova88", ExpectedResult = "https://api.telegram.org")]
        [TestCase("NOVA88", ExpectedResult = "https://api.telegram.org")]
        [TestCase("nova88", ExpectedResult = "https://api.telegram.org")]
        [TestCase("Goal123", ExpectedResult = "https://api.telegram.org")]
        [TestCase("Bwing", ExpectedResult = "https://api.telegram.org")]
        [TestCase("Ivorysport", ExpectedResult = "https://api.telegram.org")]
        [TestCase("Leobet", ExpectedResult = "https://api.telegram.org")]
        public string Telegram_GetConfigValuesBySiteNameTest(string siteName)
        {
            var actual = _configBLL.GetConfigValuesBySiteName<TelegramConfig>("Telegram");
            return actual[siteName].TelegramCommon.Url;
        }
        
        [TestCase(1, ExpectedResult = -594288926)]
        [TestCase(2, ExpectedResult = -594288926)]
        [TestCase(3, ExpectedResult = -594288926)]
        [TestCase(5, ExpectedResult = -594288926)]
        [TestCase(6, ExpectedResult = -594288926)]
        public long Telegram_GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<TelegramConfig>("Telegram");
            return actual[siteId].TelegramNotification.FirstOrDefault().ChatId;
        }
        

        #endregion

        #region LetsTalk

        [TestCase("Nova88", ExpectedResult = "https://message.biatalk.cc/bot/v3/sign-token")]
        [TestCase("NOVA88", ExpectedResult = "https://message.biatalk.cc/bot/v3/sign-token")]
        [TestCase("nova88", ExpectedResult = "https://message.biatalk.cc/bot/v3/sign-token")]
        [TestCase("Goal123", ExpectedResult = "https://message.biatalk.cc/bot/v3/sign-token")]
        [TestCase("Bwing", ExpectedResult = "https://message.biatalk.cc/bot/v3/sign-token")]
        [TestCase("Ivorysport", ExpectedResult = "https://message.biatalk.cc/bot/v3/sign-token")]
        [TestCase("Leobet", ExpectedResult = "https://message.biatalk.cc/bot/v3/sign-token")]
        public string LetsTalk_GetConfigValuesBySiteNameTest(string siteName)
        {
            var actual = _configBLL.GetConfigValuesBySiteName<LetsTalkConfig>("LetsTalk");
            return actual[siteName].LetsTalkCommon.SignInURL;
        }
        
        [TestCase(1, ExpectedResult = 100044)]
        [TestCase(2, ExpectedResult = 100044)]
        [TestCase(3, ExpectedResult = 100044)]
        [TestCase(5, ExpectedResult = 100044)]
        [TestCase(6, ExpectedResult = 100044)]
        public int LetsTalk_GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<LetsTalkConfig>("LetsTalk");
            return actual[siteId].LetsTalkNotification.FirstOrDefault().SignInId;
        }

        #endregion

        #region SMSSupport
        
        [TestCase("Nova88", ExpectedResult = "NxCloud")]
        [TestCase("NOVA88", ExpectedResult = "NxCloud")]
        [TestCase("nova88", ExpectedResult = "NxCloud")]
        [TestCase("Goal123", ExpectedResult = "NxCloud")]
        [TestCase("Bwing", ExpectedResult = "NxCloud")]
        [TestCase("Ivorysport", ExpectedResult = "NxCloud")]
        [TestCase("Leobet", ExpectedResult = "NxCloud")]
        public string SMSSupport_GetConfigValuesBySiteNameTest(string siteName)
        {
            var actual = _configBLL.GetConfigValuesBySiteName<SMSSupportConfig>("SMSSupport");
            return actual[siteName].SMSSupport["Default"];
        }
        
        [TestCase(1, ExpectedResult = "NxCloud")]
        [TestCase(2, ExpectedResult = "NxCloud")]
        [TestCase(3, ExpectedResult = "NxCloud")]
        [TestCase(5, ExpectedResult = "NxCloud")]
        [TestCase(6, ExpectedResult = "NxCloud")]
        public string SMSSupport_GetConfigValuesBySiteIdTest(int siteId)
        {
            var actual = _configBLL.GetConfigValuesBySiteId<SMSSupportConfig>("SMSSupport");
            return actual[siteId].SMSSupport["Default"];
        }
        

        #endregion
    }
}