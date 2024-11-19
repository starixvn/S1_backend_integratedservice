using System.Data;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MySql.Data.MySqlClient;

using NSubstitute;

using ThirdPartySeamlessWallet.Core.Extensions;
using ThirdPartySeamlessWallet.IntegrationTest.Models;
using ThirdPartySeamlessWallet.IntegrationTest.Stubs;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;

using BonusLog = ThirdPartySeamlessWallet.IntegrationTest.Models.BonusLog;
using FinancialLog = ThirdPartySeamlessWallet.IntegrationTest.Models.FinancialLog;
using TpSeamlessBonus = ThirdPartySeamlessWallet.IntegrationTest.Models.TpSeamlessBonus;

namespace ThirdPartySeamlessWallet.IntegrationTest.SandBox
{
    public class BettingServiceSandbox
    {
        public MySqlConnection Conn;

        public MySqlTransaction Trans = null;

        public IServiceProvider ServiceProvider;

        private readonly ServiceCollection _serviceCollection;

        private IConfiguration _configuration;

        private readonly MySqlClient _mySqlClient;

        public BettingServiceSandbox()
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

            var env = new WebHostingEnvironment
            {
                EnvironmentName = Environments.Development,
                ContentRootPath = AppDomain.CurrentDomain.BaseDirectory,
            };
            _serviceCollection.AddSingleton<IWebHostEnvironment>(env);

            _serviceCollection.AddTransient(p => Substitute.For<ILogger<SettledService>>());
            _serviceCollection.AddTransient(p => Substitute.For<ILogger<BettingService>>());

            var startUp = new StubStartUp(_configuration);
            startUp.ConfigureServices(_serviceCollection);
            ServiceProvider = _serviceCollection.BuildServiceProvider();
            _mySqlClient = ServiceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);

            Conn = _mySqlClient.GetMySqlConnection();        
        }

        public void BeginTransaction()
        {
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }

            if (Trans == null)
            {         
                Trans = Conn.BeginTransaction();
            }      
        }

        public void Commit()
        {
            Trans.Commit();
            Trans.Dispose();
            Trans = null;
            Conn.Close();
        }

        public void Rollback()
        {
            Trans.Rollback();         
            Trans.Dispose();
            Trans = null;
            Conn.Close();
        }

        public BonusStatus SetCustWallet(int custId, decimal amount)
        {
            var sqlScript = @$"UPDATE CustWallet SET Balance={amount} WHERE CustId='{custId}';";
            return _mySqlClient.Query<BonusStatus>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public BonusStatus CancelBonus(int bonusId)
        {
            var sqlScript =
                @$"UPDATE CustBonusWinLoss SET Winnings=0,Losses=0 WHERE BonusId='{bonusId}';" +
                @$"UPDATE BonusInfo SET Status=2 WHERE BonusId='{bonusId}';" +
                @$"UPDATE CustBonus SET Bonus=0, Status=2 WHERE BonusId='{bonusId}';" +
                @$"UPDATE CustBonusRollover SET Turnover = 0, Status=2 WHERE BonusId='{bonusId}';";
            return _mySqlClient.Query<BonusStatus>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public BonusStatus ResetBonusStatusAndBonusAmount(int bonusId, decimal bonusAmount)
        {
            var sqlScript =
                @$"UPDATE CustBonusWinLoss SET Winnings=0,Losses=0 WHERE BonusId='{bonusId}';" +
                @$"UPDATE BonusInfo SET Status=1 WHERE BonusId='{bonusId}';" +
                @$"UPDATE CustBonus SET Bonus= {bonusAmount}, Status=1 WHERE BonusId='{bonusId}';" +
                @$"UPDATE CustBonusRollover SET Turnover = 0, Status=1 WHERE BonusId='{bonusId}';";
            return _mySqlClient.Query<BonusStatus>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        /// <summary>
        /// 取回最後一筆BonusLog紀錄
        /// </summary>
        public BonusLog GetBonusLog(int bonusId)
        {
            var sqlScript = @$"SELECT * FROM BonusLog where BonusId ='{bonusId}' ORDER BY Id DESC LIMIT 1;";
            return _mySqlClient.Query<BonusLog>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        /// <summary>
        /// 取回最後一筆FinancialLog紀錄
        /// </summary>
        public FinancialLog GetFinancialLog(string transId)
        {
            var sqlScript = @$"SELECT * FROM FinancialLog_{DateTime.Now:yyyyMM} WHERE RefTransId='{transId}' ORDER BY Id DESC LIMIT 1;";
            return _mySqlClient.Query<FinancialLog>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public TpSeamlessBonus GetTpSeamlessBonus(int tpId, string transId)
        {
            var sqlScript = @$"SELECT * FROM TpSeamlessBonus WHERE TpId='{tpId}' AND TransId='{transId}';";
            return _mySqlClient.Query<TpSeamlessBonus>(sqlScript, CommandType.Text).FirstOrDefault();
        }
    }
}
