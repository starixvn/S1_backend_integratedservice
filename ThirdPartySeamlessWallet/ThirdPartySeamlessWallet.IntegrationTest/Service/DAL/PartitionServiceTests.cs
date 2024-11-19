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

namespace ThirdPartySeamlessWallet.IntegrationTest.Service.DAL
{
    public class PartitionServiceTests
    {
        private readonly PartitionService _partitionService;
        private readonly MySqlClient _mySqlClient;
        public PartitionServiceTests()
        {
            var services = new ServiceCollection();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "LOCAL",
            });

            var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
            remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, new[] { "Nova" }, remoteUrls);
            builder.Configuration.AddRemoteJsonConfig(remoteUrls);

            var app = builder.Build();
            var configuration = app.Configuration;


            services.AddSingleton(configuration);
            var mySqlLog = Substitute.For<ILogger<MySqlClient>>();
            services.AddTransient(p => mySqlLog);

            var conn = configuration.GetConnectionString("ConnectionString");
            services.AddSingleton(provider => new MySqlClient(provider, DataBase.Main, conn));


            var provider = services.BuildServiceProvider();
            _mySqlClient = provider.GetRequiredService<MySqlClient>();
            _partitionService = new PartitionService(provider);
        }

        private BettingDataFish_Joker _data;

        [SetUp]
        public void Setup()
        {
            var id = Guid.NewGuid().ToString();
            _data = new BettingDataFish_Joker()
            {
                appid = "test",
                hash = id,
                id = id,
                amount = 10,
                username = "testuser",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                type = "test",
                description = "test",
            };
        }

        [TestCase("2021-12-05 00:00", ExpectedResult = 1)]
        [TestCase("2022-01-05 00:00", ExpectedResult = 1)]

        public int Partition_Execute__Test(DateTime partitionTime)
        {
            _data.description = $"{_data.id}-{partitionTime:yyyyMM}";
            using var conn = _mySqlClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = _partitionService.Execute(partitionTime, "Service_BettransFish_Joker_Ins", _data, trans);
            if (result > 0)
            {
                trans.Commit();
            }
            return result;
        }

        [TestCase("2021-12-05 00:00", ExpectedResult = true)]
        [TestCase("2022-01-05 00:00", ExpectedResult = true)]

        public bool Partition_Execute_Then_Check_Test(DateTime partitionTime)
        {
            _data.description = $"{_data.id}-{partitionTime:yyyyMM}";
            using var conn = _mySqlClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = _partitionService.Execute(partitionTime, "Service_BettransFish_Joker_Ins", _data, trans);
            if (result > 0)
            {
                trans.Commit();
            }

            var check = _partitionService.Query<string>(partitionTime, "Service_BettransFish_Joker_Check", _data);
            return check.Any();
        }

        [TestCase("2021-12-31 23:59", "2022-01-01 00:00", ExpectedResult = true)]
        [TestCase("2021-12-31 23:59", "2022-01-04 00:00", ExpectedResult = false)]

        public bool PartitionCheckDay_Test(DateTime insertTime, DateTime checkTime)
        {
            _data.description = $"{_data.id}-{insertTime:yyyyMM}-{checkTime:yyyyMM}";
            using var conn = _mySqlClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            _partitionService.Execute(insertTime, "Service_BettransFish_Joker_Ins", _data, trans);
            trans.Commit();

            var check = _partitionService.Query<string>(checkTime, "Service_BettransFish_Joker_Check", _data);
            return check.Any();
        }

        [TestCase(ExpectedResult = 1)]
        public int GetUnsettleDataList_Test()
        {
            var partitionTime = new DateTime(2021, 12, 31, 23, 59, 59);
            _data.roundid = _data.id[..10];
            _data.description = $"{_data.id}-{partitionTime:yyyyMM}";
            using var conn = _mySqlClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            _partitionService.Execute(partitionTime, "Service_BettransFish_Joker_Ins", _data, trans);
            trans.Commit();


            var tpUnsettle = new TpUnsettle
            {
                TransId = _data.roundid,
                PartitionTime = partitionTime,
            };
            var unsettleDataList = _partitionService.GetUnsettleDataList<BettingDataFish_Joker>("Service_BettransFish_Joker_Unsettle_Get", tpUnsettle);
            return unsettleDataList.Count();
        }

        [TestCase(ExpectedResult = 2)]
        public int GetPartitionUnsettleDataList_Test()
        {
            var partitionTime = new DateTime(2021, 12, 31, 23, 59, 59);
            _data.roundid = _data.id[..10];
            _data.description = $"{_data.id}-{partitionTime:yyyyMM}";
            using var conn = _mySqlClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            _partitionService.Execute(partitionTime, "Service_BettransFish_Joker_Ins", _data, trans);
            _partitionService.Execute(partitionTime.AddSeconds(1), "Service_BettransFish_Joker_Ins", _data, trans);
            trans.Commit();

            var tpUnsettle = new TpUnsettle
            {
                TransId = _data.roundid,
                PartitionTime = partitionTime.AddSeconds(1),
            };
            var unsettleDataList = _partitionService.GetUnsettleDataList<BettingDataFish_Joker>("Service_BettransFish_Joker_Unsettle_Get", tpUnsettle);
            return unsettleDataList.Count();
        }

    }
}
