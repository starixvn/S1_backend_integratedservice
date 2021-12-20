using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.IntegrationTest.Models;
using ThirdPartySeamlessWallet.IntegrationTest.Stubs;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Workers;

namespace ThirdPartySeamlessWallet.IntegrationTest.SandBox
{
    public class SettleWorkerSandbox
    {
        private IServiceProvider _serviceProvider;

        private readonly ServiceCollection _serviceCollection;

        private IConfiguration _configuration;

        private readonly MySqlClient _mySqlClient;
   
        private readonly SettleWorker _target;

        public decimal ActualWinlostAmount;

        public SettleWorkerSandbox()
        {
            _serviceCollection = new ServiceCollection();
            _serviceCollection = SetEnvironment(_serviceCollection);
            _serviceCollection = LoadConfig(_serviceCollection);

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
            _target = new SettleWorker(_serviceProvider);
        }

        public SettleWorkerSandbox DoSettleTCGWork(GameType gameType)
        {
            _target.DoSettleTCGWork(gameType);
            return this;
        }

        public SettleWorkerSandbox InsertWager(string spName, object wager)
        {              
            _mySqlClient.Execute(spName, parameters: new DynamicParameters(wager));
            return this;
        }


        public SettleWorkerSandbox ShouldBeHaveUnsettleRecord(SettleFlag settleFlag, string transId)
        {
            var expected = CreateUnsettleRecord(settleFlag, transId);
            var sqlScript = @$"SELECT * FROM TpUnsettle WHERE TransId=""{transId}""";
            var actual = _mySqlClient.Query<TpUnsettle>(sqlScript, CommandType.Text).First();
            actual.Should().BeEquivalentTo(expected);
            return this;
        }

        private TpUnsettle CreateUnsettleRecord(SettleFlag settleFlag, string transId)
        {
            return new TpUnsettle
            {
                TransId = transId,
                SettleFlag = (int)settleFlag,
            };
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


        public SettleWorkerSandbox SettleFlagShouldBeSettled(string sqlScript)
        {
            var actual = _mySqlClient.Query<TpUnsettle>(sqlScript, CommandType.Text).First();
            actual.SettleFlag.Should().Be((int)SettleFlag.Settled);
            return this;
        }


        public SettleWorkerSandbox ShouldBeHaveTpBetRecord(Tp tp, string transId, string TpUserName, decimal Stake, decimal WinlostAmount)
        {
            var expected = CreateTpBetRecord(tp, transId, TpUserName, Stake, WinlostAmount);
            var sqlScript = @$"SELECT * FROM TpBetRecord WHERE tpid='{(int)tp}' and TransId='{transId}'";
            var actual = _mySqlClient.Query<TpBetRecord>(sqlScript, CommandType.Text).First();
            actual.Should().BeEquivalentTo(expected);
            ActualWinlostAmount = actual.WinlostAmount;
            return this;
        }

        private TpBetRecord CreateTpBetRecord(Tp tp, string transId, string TpUserName, decimal Stake, decimal WinlostAmount)
        {
            return new TpBetRecord()
            {
                TpId = (int)tp,
                TransId = transId,
                TpUserName = TpUserName,
                Stake = Stake,
                ValidBet = Stake,
                WinlostAmount = WinlostAmount,
                SettlementType = SettlementType.SettledForValid,
                TicketStatus = WinlostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost,
            };
        }
    }
}
