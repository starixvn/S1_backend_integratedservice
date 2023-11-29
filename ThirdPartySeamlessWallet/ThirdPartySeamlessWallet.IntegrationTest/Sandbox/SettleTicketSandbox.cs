using System.Data;

using Dapper;

using FluentAssertions;
using FluentAssertions.Common;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NSubstitute;

using ThirdPartySeamlessWallet.Core.Extensions;
using ThirdPartySeamlessWallet.IntegrationTest.Models;
using ThirdPartySeamlessWallet.IntegrationTest.Stubs;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using ThirdPartySettleWorker.Workers;

namespace ThirdPartySeamlessWallet.IntegrationTest.SandBox
{
    public class SettleTicketSandbox
    {
        private IServiceProvider _serviceProvider;

        private readonly ServiceCollection _serviceCollection;

        private IConfiguration _configuration;

        private readonly MySqlClient _mySqlClient;

        public decimal ActualWinlostAmount;

        private readonly WalletService _walletService;
        private readonly ILogger<SettledService> _logger;
        private readonly TpBetRecordExtendBLL _tpBetRecordExtendBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;

        public SettleTicketSandbox()
        {
            _serviceCollection = new ServiceCollection();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "Local",
            });

            var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
            remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, new[] { "Nova" }, remoteUrls);
            builder.Configuration.AddRemoteJsonConfig(remoteUrls);

            var app = builder.Build();
            _configuration = app.Configuration;
            _serviceCollection.AddSingleton(_configuration);

            var env = new WebHostingEnvironment
            {
                EnvironmentName = Environments.Development,
                ContentRootPath = AppDomain.CurrentDomain.BaseDirectory,
            };
            _serviceCollection.AddSingleton<IWebHostEnvironment>(env);

            _serviceCollection.AddTransient(p => Substitute.For<ILogger<SettledService>>());
            _serviceCollection.AddTransient(p => Substitute.For<ILogger<BettingService>>());
            var mySqlLog = Substitute.For<ILogger<MySqlClient>>();
            _serviceCollection.AddTransient(p => mySqlLog);
            var conn = _configuration.GetConnectionString("ConnectionString");
            _serviceCollection.AddSingleton(new MySqlClient(conn, mySqlLog));

            var startUp = new StubStartUp(_configuration);
            startUp.ConfigureServices(_serviceCollection);
            _serviceProvider = _serviceCollection.BuildServiceProvider();
            _mySqlClient = _serviceProvider.GetRequiredService<MySqlClient>();

            _walletService = _serviceProvider.GetRequiredService<WalletService>();
            _logger = _serviceProvider.GetRequiredService<ILogger<SettledService>>();
            _tpBetRecordExtendBLL = _serviceProvider.GetRequiredService<TpBetRecordExtendBLL>();
            _thirdPartyBLL = _serviceProvider.GetRequiredService<ThirdPartyBLL>();
        }

        public SettleTicket CreateSettleTicket(TpCustInfo tpCustInfo, BetRecordInfo betRecord, BetRecordInfo refBetRecord)
        {
            var conn = _mySqlClient.GetMySqlConnection();
            conn.Open();
            return new SettleTicket(
                        tpCustInfo: tpCustInfo,
                        betRecord: betRecord,
                        refBetRecord: refBetRecord,
                        mainDbClient: _mySqlClient,
                        trans: conn.BeginTransaction(),
                        walletService: _walletService,
                        logger: _logger,
                        tpBetRecordExtendBLL: _tpBetRecordExtendBLL,
                        thirdPartyBLL: _thirdPartyBLL
                        );
        }        
    }
}
