using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ThirdPartySeamlessWallet.IntegrationTest.Models;
using ThirdPartySeamlessWallet.IntegrationTest.Stubs;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.ThirdParty;

namespace ThirdPartySeamlessWallet.IntegrationTest.SandBox
{
    public class BtiServiceSandbox
    {
        private IServiceProvider _serviceProvider;

        private readonly ServiceCollection _serviceCollection;

        private IConfiguration _configuration;

        private readonly MySqlClient _mySqlClient;

        private static string TpUserName = "DNova88_249953";

        private readonly string _reserveId;

        private Dictionary<Type, Lazy<string>> _exceptSqlMapping;

        private readonly BtiService _target;


        public BtiServiceSandbox(string reserveId)
        {
            _reserveId = reserveId;
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
            _target = new BtiService(_serviceProvider);

            _exceptSqlMapping = new Dictionary<Type, Lazy<string>>
            {
                {
                    typeof(ReserveRecord), new Lazy<string>(() =>
                        @$"SELECT * FROM BettransSports_BTI_Reserve WHERE reserve_id={_reserveId}")
                },
            };
        }

        public BtiServiceSandbox CreateReserve(string reserve_id, decimal amount)
        {
            var request = new BtiRequestObj()
            {
                cust_id = TpUserName,
                reserve_id = reserve_id,
                amount = amount,
            };
            var result = _target.ReserveAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            return this;
        }

        public BtiServiceSandbox ShouldBeHaveRecord<TRecord>(Lazy<TRecord> data)
            where TRecord : class
        {
            var sqlScript = _exceptSqlMapping[typeof(TRecord)].Value;
            var actual = _mySqlClient.Query<TRecord>(sqlScript).First();
            actual.Should().BeEquivalentTo(data.Value);
            return this;
        }

        public static Lazy<ReserveRecord> ReserveRecord(string reserve_id, decimal amount) =>
        new Lazy<ReserveRecord>(() => new ReserveRecord
        {
            cust_id = TpUserName,
            reserve_id = reserve_id,
            amount = amount * 1000,
            TicketType = 0,
            TicketStatus = 0,
            SettleFlag = 0,
        });

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
                .AddJsonFile("appsettings.Development.json", false, false);
            _configuration = configBuilder.Build();
            services.AddSingleton(_configuration);
            return services;
        }

    }
}
