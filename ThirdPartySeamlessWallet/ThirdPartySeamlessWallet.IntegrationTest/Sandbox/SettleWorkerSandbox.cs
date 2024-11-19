using System.Data;

using Dapper;

using FluentAssertions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using ThirdPartySeamlessWallet.Core.Extensions;
using ThirdPartySeamlessWallet.IntegrationTest.Models;
using ThirdPartySeamlessWallet.IntegrationTest.Stubs;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using ThirdPartySettleWorker.Workers;

namespace ThirdPartySeamlessWallet.IntegrationTest.SandBox
{
    public class SettleWorkerSandbox
    {
        private IServiceProvider _serviceProvider;

        private readonly ServiceCollection _serviceCollection;

        private IConfiguration _configuration;

        private readonly MySqlClient _mySqlClient;

        private readonly TCGWorker _target;

        public decimal ActualWinlostAmount;

        public SettleWorkerSandbox()
        {
            _serviceCollection = new ServiceCollection();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "LOCAL",
            });

            var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
            remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, new[] { "Nova" }, remoteUrls);
            builder.Configuration.AddRemoteJsonConfig(remoteUrls);

            var app = builder.Build();
            _configuration = app.Configuration;
            _serviceCollection.AddSingleton(_configuration);


            _serviceCollection.AddTransient(p => Substitute.For<ILogger<SettledService>>());
            _serviceCollection.AddTransient(p => Substitute.For<ILogger<BettingService>>());
            var mySqlLog = Substitute.For<ILogger<MySqlClient>>();
            _serviceCollection.AddTransient(p => mySqlLog);
            var conn = _configuration.GetConnectionString("ConnectionString");
            _serviceCollection.AddSingleton(provider => new MySqlClient(provider, DataBase.Main, conn));

            var startUp = new StubStartUp(_configuration);
            startUp.ConfigureServices(_serviceCollection);
            _serviceProvider = _serviceCollection.BuildServiceProvider();
            _mySqlClient = _serviceProvider.GetRequiredService<MySqlClient>();
            _target = new TCGWorker(_serviceProvider);
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
            var sqlScript = @$"SELECT * FROM Siebog.TpUnsettle WHERE TransId=""{transId}""";
            var actual = _mySqlClient.Query<TpUnsettle>(sqlScript, CommandType.Text).First();
            actual.PartitionTime = default;
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

        public SettleWorkerSandbox SettleFlagShouldBeSettled(string sqlScript)
        {
            var actual = _mySqlClient.Query<TpUnsettle>(sqlScript, CommandType.Text).First();
            actual.SettleFlag.Should().Be((int)SettleFlag.Settled);
            return this;
        }

        public SettleWorkerSandbox SettleFlagShouldBeEndGame(string sqlScript)
        {
            var actual = _mySqlClient.Query<TpUnsettle>(sqlScript, CommandType.Text).First();
            actual.SettleFlag.Should().Be((int)SettleFlag.EndGame);
            return this;
        }

        public SettleWorkerSandbox ShouldBeHaveTpBetRecord(Tp tp, string transId, string TpUserName, decimal Stake, decimal WinlostAmount)
        {
            var expected = CreateTpBetRecord(tp, transId, TpUserName, Stake, WinlostAmount);
            var sqlScript = @$"SELECT * FROM Siebog.TpBetRecord WHERE tpid='{(int)tp}' and TransId='{transId}'";
            var actual = _mySqlClient.Query<TpBetRecord>(sqlScript, CommandType.Text).First();
            actual.Should().BeEquivalentTo(expected);
            ActualWinlostAmount = actual.WinlostAmount;
            return this;
        }

        public SettleWorkerSandbox ShouldBeHaveUnsettleTpBetRecord(Tp tp, string transId, string TpUserName, decimal Stake, decimal WinlostAmount)
        {
            var expected = CreateUnsettleTpBetRecord(tp, transId, TpUserName, Stake, WinlostAmount);
            var sqlScript = @$"SELECT * FROM Siebog.TpBetRecord WHERE tpid='{(int)tp}' and TransId='{transId}'";
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

        private TpBetRecord CreateUnsettleTpBetRecord(Tp tp, string transId, string TpUserName, decimal Stake, decimal WinlostAmount)
        {
            return new TpBetRecord()
            {
                TpId = (int)tp,
                TransId = transId,
                TpUserName = TpUserName,
                Stake = Stake,
                ValidBet = Stake,
                WinlostAmount = WinlostAmount,
                SettlementType = SettlementType.UnSettled,
                TicketStatus = TicketStatus.UnSettled,
            };
        }
    }
}
