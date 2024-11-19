using System.Data;

using FluentAssertions;

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
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.IntegrationTest.SandBox
{
    public class BTIBLLSandbox
    {
        private IServiceProvider _serviceProvider;

        private readonly ServiceCollection _serviceCollection;

        private IConfiguration _configuration;

        private readonly MySqlClient _mySqlClient;

        private static string TpUserName = "DNova88_249953";

        private readonly string _reserveId;

        private Dictionary<Type, Lazy<string>> _exceptSqlMapping;

        private readonly BTIBLL _target;


        public BTIBLLSandbox(string reserveId)
        {
            _reserveId = reserveId;

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

            var env = new WebHostingEnvironment
            {
                EnvironmentName = Environments.Development,
                ContentRootPath = AppDomain.CurrentDomain.BaseDirectory,
            };
            _serviceCollection.AddSingleton<IWebHostEnvironment>(env);
            _serviceCollection.AddSingleton<IHostEnvironment>(env);

            _serviceCollection.AddTransient(p => Substitute.For<ILogger<SettledService>>());
            _serviceCollection.AddTransient(p => Substitute.For<ILogger<BettingService>>());

            var startUp = new StubStartUp(_configuration);
            startUp.ConfigureServices(_serviceCollection);
            _serviceProvider = _serviceCollection.BuildServiceProvider();
            _mySqlClient = _serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);

            _target = new BTIBLL(_serviceProvider);

            _exceptSqlMapping = new Dictionary<Type, Lazy<string>>
            {
                {
                    typeof(ReserveRecord), new Lazy<string>(() =>
                        @$"SELECT * FROM BettransSports_BTI_Reserve WHERE reserve_id=""{_reserveId}""")
                },
            };
        }

        public BTIBLLSandbox CreateReserve(string reserve_id, decimal amount)
        {
            var request = new BTIRequestObj()
            {
                cust_id = TpUserName,
                reserve_id = reserve_id,
                amount = amount,
            };
            var result = _target.ReserveAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            return this;
        }

        public BTIBLLSandbox ShouldBeHaveRecord<TRecord>(Lazy<TRecord> data)
            where TRecord : class
        {
            var sqlScript = _exceptSqlMapping[typeof(TRecord)].Value;
            var actual = _mySqlClient.Query<TRecord>(sqlScript, CommandType.Text).First();
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

    }
}
