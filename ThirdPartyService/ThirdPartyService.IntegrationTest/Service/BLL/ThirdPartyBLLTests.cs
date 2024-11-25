using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordExtend;
using ThirdPartyService.Extensions;
using ThirdPartyService.IntegrationTest.Models;
using ThirdPartyService.IntegrationTest.Stubs;
using ThirdPartyService.Service.BLL;
using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.IntegrationTest.Service.BLL
{
    [TestFixture()]
    public class ThirdPartyBLLTests
    {
        private IConfiguration _configuration;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        public ThirdPartyBLLTests()
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
            ThirdPartyHelperBase.IsDevelopment = env.IsDevelopment();

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

        //[Ignore("dev")]
        [TestCase(250072, "2360725352", 7, ExpectedResult = ErrorCode.Success)]
        public ErrorCode GetBetDetailTest(int custId, string transId, int gameType)
        {
            var actual = _thirdPartyBLL.GetBetDetail(new GetBetDetailRequest
            {
                CustId = custId,
                TransId = transId,
                GameType = gameType,
                SettledTime = DateTime.Now,
            });
            Console.WriteLine(JsonConvert.SerializeObject(actual));
            return actual.ErrorCode;
        }

        [TestCase(39, 250072, "en", ExpectedResult = ErrorCode.Success)]
        public ErrorCode GotoGameTest(int TpId, int CustId, string LangId)
        {
            var actual = _thirdPartyBLL.GotoGame(new TpInfo
            {
                TpId = TpId,
                CustId = CustId,
                LangId = LangId,
                AppType = 1,
                SiteId = 1,
                inputs = new Dictionary<string, string>
                {
                    { "LangId", "en-US" },
                    { "gameId", "10001" }
                }
            });
            Console.WriteLine(JsonConvert.SerializeObject(actual));
            return actual.ErrorCode;
        }

        [Ignore("dev")]
        [TestCase(226500, "5WG09igtsu5CEV", 67, 39, 51, ExpectedResult = ErrorCode.Success)]
        public ErrorCode GetTpBetRecordExtendTest(int custId, string transId, int gameType, int tpId, int currencyId)
        {
            var actual = _thirdPartyBLL.GetTpBetRecordExtend(new GetTpBetRecordExtendRequest
            {
                CustId = custId,
                CurrencyId = currencyId,
                BetRecordInfo = new BetRecordInfo()
                {
                    TransId = transId,
                    GameType = gameType,
                    TpId = tpId,
                }
            });
            Console.WriteLine(JsonConvert.SerializeObject(actual));
            return actual.ErrorCode;
        }
    }
}