using System;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.MG;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;

namespace ThirdPartyWager.IntegrationTest.Unittest
{
    [TestFixture]
    public class MGTests
    {
        private IServiceProvider _serviceProvider;
        private IConfiguration _configuration;
        [SetUp]
        public void SetUp()
        {
            var serviceCollection = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            configBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, false)
                .AddJsonFile("appsettings.Local.json", false, false);
            _configuration = configBuilder.Build();
            serviceCollection.AddSingleton(_configuration);
            
            serviceCollection.AddTransient(p => Substitute.For<IApiMGBLL>());
            serviceCollection.AddTransient(p => Substitute.For<IApiSWBLL>());
            serviceCollection.AddTransient(p => Substitute.For<IMemoryCache>());
            serviceCollection.AddTransient(p => Substitute.For<ILogger<Log2File>>());
            serviceCollection.AddTransient<ThirdPartyHelper>();
            serviceCollection.AddTransient<ThirdPartyBLL>();
            serviceCollection.AddTransient<Log2File>();
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        
        [TestCase("AEBQAAQAYDEMUDQAAAAABRVW45RBAAAAAA", "AIBQAAQAYDEMUDQAAAAABRVW45RBAAAAAAAA")]
        [TestCase("AEBQAAQAEYTD6DIAAAAABSC5ANRBAAAAAA", "AIBQAAQAEYTD6DIAAAAABSC5ANRBAAAAAAAA")]
        [TestCase("AEBQAAQAIT2V4DYAAAAABMFXINPBAAAAAA", "AIBQAAQAIT2V4DYAAAAABMFXINPBAAAAAAAA")]
        [TestCase("AEBQAAQA2LZLIEAAAAAAAU2XWRPBAAAAAA", "AIBQAAQA2LZLIEAAAAAAAU2XWRPBAAAAAAAA")]
        [TestCase("032000C6019A430B0001000000002F141EA4", "032000C6019A430B0001000000002F141EA4")]
        [TestCase("AIBQAAQATVUOEEAAAAAABBJIYFPBAAAAAAAA", "AIBQAAQATVUOEEAAAAAABBJIYFPBAAAAAAAA")]
        public void TestTransformTxnId(string origin, string target)
        {
            var mgBll = new MGBLL(_serviceProvider);

            var actual = mgBll.TransformTxnId(origin);

            actual.Should().Be(target);
        }
    }
}