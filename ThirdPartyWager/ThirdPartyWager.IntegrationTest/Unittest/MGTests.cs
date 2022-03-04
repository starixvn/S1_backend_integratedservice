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
    }
}