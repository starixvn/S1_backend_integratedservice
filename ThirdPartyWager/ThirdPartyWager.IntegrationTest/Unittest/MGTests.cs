using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using NUnit.Framework;

using ThirdPartyWager.Extensions;
using ThirdPartyWager.Service.BLL;
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
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "LOCAL",
            });

            var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
            remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, new[] { "Nova" }, remoteUrls);
            builder.Configuration.AddRemoteJsonConfig(remoteUrls);

            var app = builder.Build();
            serviceCollection.AddSingleton(_configuration);
            _configuration = app.Configuration;
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