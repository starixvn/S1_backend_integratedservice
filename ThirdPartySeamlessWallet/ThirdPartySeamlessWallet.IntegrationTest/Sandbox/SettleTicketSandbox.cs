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

using BonusLog = ThirdPartySeamlessWallet.IntegrationTest.Models.BonusLog;
using FinancialLog = ThirdPartySeamlessWallet.IntegrationTest.Models.FinancialLog;

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

        public void BeginTransaction()
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

        public BonusStatus GetBonusInfo(int bonusId)
        {
            var sqlScript = @$"SELECT * FROM BonusInfo WHERE BonusId='{bonusId}';";
            return _mySqlClient.Query<BonusStatus>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public BonusStatus GetCustBonus(int bonusId)
        {
            var sqlScript = @$"SELECT * FROM CustBonus WHERE BonusId='{bonusId}';";
            return _mySqlClient.Query<BonusStatus>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public CustBonusRollover GetCustBonusRollover(int bonusId)
        {
            var sqlScript = @$"SELECT * FROM CustBonusRollover WHERE BonusId='{bonusId}';";
            return _mySqlClient.Query<CustBonusRollover>(sqlScript, CommandType.Text).FirstOrDefault();
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

        public BonusStatus ResetBonus(int bonusId, decimal bonusAmount, decimal rollover, decimal turnover)
        {
            var sqlScript =
                @$"UPDATE CustBonusWinLoss SET Winnings=0,Losses=0 WHERE BonusId='{bonusId}';" +
                @$"UPDATE BonusInfo SET Status=1 WHERE BonusId='{bonusId}';" +
                @$"UPDATE CustBonus SET Bonus= {bonusAmount}, Status=1 WHERE BonusId='{bonusId}';" +
                @$"UPDATE CustBonusRollover SET Turnover = {turnover}, Rollover = {rollover}, Status=1 WHERE BonusId='{bonusId}';";
            return _mySqlClient.Query<BonusStatus>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public BonusStatus SetBonusStatus(int bonusId, int status)
        {
            var sqlScript =                
                @$"UPDATE BonusInfo SET Status={status} WHERE BonusId='{bonusId}';" +
                @$"UPDATE CustBonus SET Status={status} WHERE BonusId='{bonusId}';" +
                @$"UPDATE CustBonusRollover SET Turnover = 0, Status={status} WHERE BonusId='{bonusId}';";
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
        public FinancialLog GetFinancialLog(int transId)
        {
            var sqlScript = @$"SELECT * FROM FinancialLog_{DateTime.Now:yyyyMM} WHERE RefTransId='{transId}' ORDER BY Id DESC LIMIT 1;";
            return _mySqlClient.Query<FinancialLog>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public int CreateTurnOverLog(decimal turnOver, int turnOverType, TpCustInfo tpCustInfo, BetRecordInfo betRecord)
        {
            var mainType = turnOver > 0 ? 1 : 2;
            var sqlScript =
                @$"INSERT INTO TurnOverLog_{DateTime.Now:yyyyMM} 
                (TurnOverType, CustId, UserName, SiteId, TpId, GameType, MainType, ChangeTurnOver, BeforeTurnOver, AfterTurnOver, Memo, BonusId, RefTransId) VALUES 
                ({turnOverType}, {tpCustInfo.CustId}, '{tpCustInfo.UserName}', {tpCustInfo.SiteId}, {betRecord.TpId}, {(int)betRecord.GameType}, {mainType}, {turnOver}, 0, 0, 'test', {betRecord.BonusId}, '{betRecord.TransId}');";
            var insert = _mySqlClient.Execute(sqlScript, CommandType.Text);
            return insert;
        }

        public TurnOverLog GetLastTurnOverLog(string transId)
        {
            var sqlScript = @$"SELECT * FROM TurnOverLog_{DateTime.Now:yyyyMM} WHERE RefTransId='{transId}' order by Id desc;";
            return _mySqlClient.Query<TurnOverLog>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public IEnumerable<TurnOverLog> GetTurnOverLogList(string transId)
        {
            var sqlScript = @$"SELECT * FROM TurnOverLog_{DateTime.Now:yyyyMM} WHERE RefTransId='{transId}';";
            return _mySqlClient.Query<TurnOverLog>(sqlScript, CommandType.Text);
        }

        public CustWithdrawalLimit GetCustWithdrawalLimit(int custId)
        {
            var sqlScript = @$"SELECT * FROM CustWithdrawalLimit WHERE CustId='{custId}';";
            return _mySqlClient.Query<CustWithdrawalLimit>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public BonusStatus ResetCustWithdrawalLimit(int custId)
        {
            var sqlScript = @$"UPDATE CustWithdrawalLimit SET WithdrawalLimit=100000,TurnOver=10000 WHERE CustId='{custId}';";
            return _mySqlClient.Query<BonusStatus>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public BonusStatus SetCustWithdrawalLimit(int custId, decimal withdrawalLimit, decimal turnOver)
        {
            var sqlScript = @$"UPDATE CustWithdrawalLimit SET WithdrawalLimit={withdrawalLimit},TurnOver={turnOver} WHERE CustId='{custId}';";
            return _mySqlClient.Query<BonusStatus>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public CustTransSummary GetCustTransSummary(int custId)
        {
            var sqlScript = @$"SELECT * FROM CustTransSummary WHERE CustId='{custId}';";
            return _mySqlClient.Query<CustTransSummary>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public int AddTpSeamlessBonus(ThirdPartySeamlessWallet.Service.Model.TpSeamlessBonus tpSeamlessBonus)
        {
            return _mySqlClient.Execute("Service_TpSeamlessBonus_Ins", CommandType.StoredProcedure, tpSeamlessBonus);
        }

        public int ResetTpBetRecord(int tpId, int custId, int bonusId)
        {
            var sqlScript = @$"DELETE FROM TpBetRecord WHERE TpId ='{tpId}' and CustId ='{custId}' and BonusId ='{bonusId}';";
            return _mySqlClient.Execute(sqlScript, CommandType.Text);
        }

        public int SetTpBetRecord(TpCustInfo tpCustInfo, BetRecordInfo betRecord)
        {
            var result = _mySqlClient.Execute("Service_TpBetRecord_Set_V3", CommandType.StoredProcedure, new
            {
                betRecord.TpId,
                betRecord.TransId,
                GameType = (int)betRecord.GameType,
                tpCustInfo.CustId,
                tpCustInfo.UserName,
                tpCustInfo.CurrencyId,
                tpCustInfo.SiteId,
                tpCustInfo.TpUserName,
                betRecord.SettledTime,
                betRecord.TransTime,
                betRecord.Stake,
                betRecord.ValidBet,
                betRecord.Revenue,
                betRecord.WinlostAmount,
                betRecord.ProgressiveBet,
                betRecord.ProgressiveWin,
                SettlementType = (int)betRecord.SettlementType,
                TicketStatus = (int)betRecord.TicketStatus,
                betRecord.OriginalState,
                betRecord.TicketKey,
                betRecord.BonusId,
                betRecord.GameID
            });
            return result;
        }

        public BetRecordInfo GetTpBetRecord(int tpId, string transId, int gameType)
        {
            var sqlScript =     
                @$"SELECT " +
                "TpId,"+
                "TransId,"+
                "GameType,"+
                "CustId,"+
                "UserName,"+
                "CurrencyId,"+
                "SiteId,"+
                "TpUserName,"+
                "UNIX_TIMESTAMP(TransTime) as TransTime," +
                "UNIX_TIMESTAMP(SettledTime) as SettledTime," +
                "Stake,"+
                "ValidBet,"+
                "Revenue,"+
                "WinlostAmount,"+
                "ProgressiveBet,"+
                "ProgressiveWin,"+
                "SettlementType,"+
                "TicketStatus,"+
                "OriginalState,"+
                "TicketKey,"+
                "BonusId,"+
                "GameID"+
                @$" FROM TpBetRecord WHERE TpId ='{tpId}' and TransId ='{transId}' and GameType ='{gameType}';";
            return _mySqlClient.Query<BetRecordInfo>(sqlScript, CommandType.Text).FirstOrDefault();
        }

        public int GetTpSeamlessBonusFlag(int tpId, string transId)
        {
            var sqlScript = @$"SELECT Flag FROM TpSeamlessBonus WHERE TpId='{tpId}' AND TransId='{transId}';";
            return _mySqlClient.Query<int>(sqlScript, CommandType.Text).FirstOrDefault();
        }


    }
}
