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
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

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

        public MySqlConnection _conn;

        public MySqlTransaction _trans = null;

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

            var startUp = new StubStartUp(_configuration);
            startUp.ConfigureServices(_serviceCollection);
            _serviceProvider = _serviceCollection.BuildServiceProvider();
            _mySqlClient = _serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);

            _walletService = _serviceProvider.GetRequiredService<WalletService>();
            _logger = _serviceProvider.GetRequiredService<ILogger<SettledService>>();
            _tpBetRecordExtendBLL = _serviceProvider.GetRequiredService<TpBetRecordExtendBLL>();

            _conn = _mySqlClient.GetMySqlConnection();
            _conn.Open();
            _trans = _conn.BeginTransaction();
        }

        private void BeginTransaction()
        {       
            if (_trans == null)
            {
                _trans = _conn.BeginTransaction();
            }      
        }

        public SettleTicket CreateSettleTicket(TpCustInfo tpCustInfo, BetRecordInfo betRecord, BetRecordInfo refBetRecord)
        {
            BeginTransaction();
            return new SettleTicket(
                        tpCustInfo: tpCustInfo,
                        betRecord: betRecord,
                        refBetRecord: refBetRecord,
                        mainDbClient: _mySqlClient,
                        trans: _trans,
                        walletService: _walletService,
                        logger: _logger,
                        tpBetRecordExtendBLL: _tpBetRecordExtendBLL
                        );
        }

        public void Commit()
        {
            _trans.Commit();
            _trans.Dispose();
            _trans = null;
        }

        public CustWinLost GetCustWinLost(int custId)
        {
            var sqlScript = @$"SELECT * FROM CustWinLost WHERE CustId='{custId}';";
            return _mySqlClient.Query<CustWinLost>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public CustWinLost GetCustBonusWinLoss(int bonusId)
        {
            var sqlScript = @$"SELECT * FROM CustBonusWinLoss WHERE BonusId='{bonusId}';";
            return _mySqlClient.Query<CustWinLost>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public WinningsLog GetWinningsLog(string transId)
        {   
            var sqlScript = @$"SELECT * FROM WinningsLog_{DateTime.Now:yyyyMM} WHERE RefTransId='{transId}';"; 
            return _mySqlClient.Query<WinningsLog>(sqlScript, CommandType.Text).FirstOrDefault(); 
        }

        public LossesLog GetLossesLog(string transId)
        {
            var sqlScript = @$"SELECT * FROM LossesLog_{DateTime.Now:yyyyMM} WHERE RefTransId='{transId}';";
            return _mySqlClient.Query<LossesLog>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public WinningsLog GetBonusWinningsLog(string transId)
        {
            var sqlScript = @$"SELECT * FROM BonusWinningsLog WHERE RefTransId='{transId}';";
            return _mySqlClient.Query<WinningsLog>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public LossesLog GetBonusLossesLog(string transId)
        {
            var sqlScript = @$"SELECT * FROM BonusLossesLog WHERE RefTransId='{transId}';";
            return _mySqlClient.Query<LossesLog>(sqlScript, CommandType.Text).FirstOrDefault();
        }
    }
}
