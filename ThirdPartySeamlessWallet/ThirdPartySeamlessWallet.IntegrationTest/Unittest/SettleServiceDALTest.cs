using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using NUnit.Framework;

using ThirdPartySeamlessWallet.Core.Extensions;
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

            serviceCollection.AddTransient(p => Substitute.For<ILogger<SettledService>>());
            serviceCollection.AddTransient(p => Substitute.For<ILogger<MySqlClient>>());
            serviceCollection.AddTransient(p => Substitute.For<ILogger<Log2File>>());
            serviceCollection.AddTransient(p => Substitute.For<WalletService>(_configuration));
            serviceCollection.AddTransient(p => Substitute.For<Tools>());
            serviceCollection.AddTransient(p => Substitute.For<Log2File>(p));

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

    }
}