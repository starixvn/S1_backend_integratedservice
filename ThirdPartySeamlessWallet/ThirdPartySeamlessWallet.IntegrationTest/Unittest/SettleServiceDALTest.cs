using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.IntegrationTest.Unittest
{
    public class SettleServiceDALTest
    {
        private IServiceProvider _serviceProvider;
        private IConfiguration _configuration;
        
        [SetUp]
        public void SetUp()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection = SetEnvironment(serviceCollection);
            serviceCollection = LoadConfig(serviceCollection);
            serviceCollection.AddTransient(p => Substitute.For<ILogger<SettledService>>());
            serviceCollection.AddTransient(p => Substitute.For<ILogger<MySqlClient>>());
            serviceCollection.AddTransient(p => Substitute.For<ILogger<Log2File>>());
            serviceCollection.AddTransient(p => Substitute.For<WalletService>(_configuration));
            serviceCollection.AddTransient(p => Substitute.For<Tools>());
            serviceCollection.AddTransient(p => Substitute.For<Log2File>(p));
            
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        [TestCase]
        public void Test_PT_Slot_Settle_Without_Bonus()
        {
            var tpCustInfo = new TpCustInfo(){ CustId = 250342, SiteId = 1, CurrencyId = 2, UserName = "antking", TpUserName = "ANVC_XNOVA88_250342", TpPwd = "SAfMLpj"};
            var betRecordInfo = new BetRecordInfo()
            {
                TpId = 27, Stake = 10M, ValidBet = 10M, WinlostAmount = 50M, ProgressiveBet = 0M, ProgressiveWin = 0M, 
                TransId = "PT11111111", GameType = GameType.PT_RNG, TpUserName = "ANVC_XNOVA88_250342", SettledTime = 1617022829,
                SettlementType = SettlementType.SettledForValid,
                BonusId = 0
            };
            
            var settleService = new SettledService(_serviceProvider);
            
            var result = settleService.Settle(tpCustInfo, betRecordInfo);
            Assert.AreEqual(1, result);
        }
        
        [TestCase]
        public void Test_PT_LiveCasino_Settle_Without_Bonus()
        {
            var tpCustInfo = new TpCustInfo(){ CustId = 250342, SiteId = 1, CurrencyId = 2, UserName = "antking", TpUserName = "ANVC_XNOVA88_250342", TpPwd = "SAfMLpj"};
            var betRecordInfo = new BetRecordInfo()
            {
                TpId = 27, Stake = 10M, ValidBet = 10M, WinlostAmount = 50M, ProgressiveBet = 0M, ProgressiveWin = 0M, 
                TransId = "PT22222222", GameType = GameType.PT_LiveCasino, TpUserName = "ANVC_XNOVA88_250342", SettledTime = 1617022829,
                SettlementType = SettlementType.SettledForValid, 
                BonusId = 0
            };
            
            var settleService = new SettledService(_serviceProvider);
            
            var result = settleService.Settle(tpCustInfo, betRecordInfo);
            Assert.AreEqual(1, result);
        }
        
        [TestCase]
        public void Test_PT_Slot_Settle_With_Bonus()
        {
            var tpCustInfo = new TpCustInfo(){ CustId = 250342, SiteId = 1, CurrencyId = 2, UserName = "antking", TpUserName = "ANVC_XNOVA88_250342", TpPwd = "SAfMLpj"};
            var betRecordInfo = new BetRecordInfo()
            {
                TpId = 27, Stake = 10M, ValidBet = 10M, WinlostAmount = 50M, ProgressiveBet = 0M, ProgressiveWin = 0M, 
                TransId = "PT33333333", GameType = GameType.PT_RNG, TpUserName = "ANVC_XNOVA88_250342", SettledTime = 1617022829,
                SettlementType = SettlementType.SettledForValid,
                BonusId = 55367
            };
            
            var settleService = new SettledService(_serviceProvider);
            
            var result = settleService.Settle(tpCustInfo, betRecordInfo);
            Assert.AreEqual(1, result);
        }
        
        [TestCase]
        public void Test_PT_LiveCasino_Settle_With_Bonus()
        {
            var tpCustInfo = new TpCustInfo(){ CustId = 250342, SiteId = 1, CurrencyId = 2, UserName = "antking", TpUserName = "ANVC_XNOVA88_250342", TpPwd = "SAfMLpj"};
            var betRecordInfo = new BetRecordInfo()
            {
                TpId = 27, Stake = 200M, ValidBet = 200M, WinlostAmount = 200M, ProgressiveBet = 0M, ProgressiveWin = 0M, 
                TransId = "PT44444447", GameType = GameType.PT_LiveCasino, TpUserName = "ANVC_XNOVA88_250342", SettledTime = 1617022829,
                SettlementType = SettlementType.SettledForValid, 
                BonusId = 55376
            };
            
            var settleService = new SettledService(_serviceProvider);
            
            var result = settleService.Settle(tpCustInfo, betRecordInfo);
            Assert.AreEqual(1, result);
        }

        [TestCase]
        public void Test_GetBonusIdByPromoCategoryId()
        {
            var custId = 250323;
            var promoCategoryId = 7;
            
            var settleService = new SettledService(_serviceProvider);

            var actual = settleService.GetBonusIdByPromoCategoryId(custId, promoCategoryId, out var bonusId);
            
            Assert.IsTrue(actual);
            Assert.AreEqual(55361, bonusId);
        }

        private ServiceCollection SetEnvironment(ServiceCollection services)
        {
            var env = new HostingEnvironment()
            {
                EnvironmentName = Environments.Development,
            };
            services.AddSingleton<IHostEnvironment>(env);
            return services;
        }

        private ServiceCollection LoadConfig(ServiceCollection services)
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, false)
                .AddJsonFile("appsettings.Local.json", false, false);
            _configuration = configBuilder.Build();
            services.AddSingleton(_configuration);
            return services;
        }
    }
}