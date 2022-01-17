using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class BettingService
    {
        private readonly IConfiguration configuration;
        private readonly WalletService walletService;
        private readonly RiskInspection _riskInspection;
        private readonly Tools tools;
        private readonly ILogger<BettingService> logger;
        private readonly Log2File log2File;
        private readonly MySqlClient _mainDbClient;
        private readonly PartitionService _partitionService;

        public BettingService(IServiceProvider provider)
        {
            configuration = provider.GetRequiredService<IConfiguration>();
            walletService = provider.GetRequiredService<WalletService>();
            tools = provider.GetRequiredService<Tools>();
            logger = provider.GetRequiredService<ILogger<BettingService>>();
            log2File = provider.GetRequiredService<Log2File>();
            _riskInspection = provider.GetRequiredService<RiskInspection>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _partitionService = provider.GetRequiredService<PartitionService>();
        }

        private int AddTpBonus(int tpId, string transId, int gameType, int custId, string userName, int currencyId, int siteId, decimal stake, int bonusId, TpCategory tpCategory, MySqlConnection conn, MySqlTransaction trans)
        {
            if (bonusId == 0)
                return 0;
            int result = conn.Execute($"Service_Tp{tpCategory}Bonus_Ins", new { TpId = tpId, TransId = transId, GameType = gameType, CustId = custId, UserName = userName, CurrencyId = currencyId, SiteId = siteId, Stake = stake, BonusId = bonusId }, transaction: trans, commandType: CommandType.StoredProcedure);

            return result;
        }
        public int PlaceBetV2(TpCustInfo tpCustInfo, decimal amount, int tpId, string tpTransId, int gameType, string refId, string betData, TpCategory tpCategory, out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            var t1 = DateTimeOffset.UtcNow.ToString("o");
            var t3 = "";
            var t4 = "";
            var t5 = "";
            var t6 = "";
            var t7 = "";
            var t8 = "";

            afterAmount = 0;
            int result = 0;
            bool bHasBonusWallet = true;

            if (_riskInspection.IsOutOfLimit(tpCustInfo.CustId, (int)tpCategory, gameType, Math.Abs(amount),
                                             out var minBet, out var maxBet))
            {
                logger.LogInformation($"User:{tpCustInfo.TpUserName}, tpCategory:{(int)tpCategory}, gameType:{gameType}, betAmount:{Math.Abs(amount)} " +
                                              $"minBet:{minBet} maxBet:{maxBet} out of bet limit");
                return -9;
            }
            var t2 = DateTimeOffset.UtcNow.ToString("o");

            try
            {
                if (bHasBonusWallet)
                {
                    result = walletService.SetBonus(gameType, tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, ref bonusId, conn, trans);
                    t3 = DateTimeOffset.UtcNow.ToString("o");
                    if (bonusId > 0)
                    {
                        if (tpTransId != null)
                        {
                            AddTpBonus(tpId, tpTransId, gameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, amount, bonusId, tpCategory, conn, trans);
                            t4 = DateTimeOffset.UtcNow.ToString("o");
                        }

                        if (afterAmount < 0)
                        {
                            amount = afterAmount;
                            result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
                            t5 = DateTimeOffset.UtcNow.ToString("o");
                        }
                        else
                        {
                            afterAmount += walletService.GetBalance(tpCustInfo.CustId);
                            t6 = DateTimeOffset.UtcNow.ToString("o");
                        }
                    }
                    else if (bonusId == 0)
                    {
                        result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
                        t7 = DateTimeOffset.UtcNow.ToString("o");
                    }
                }
                else
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
                    t8 = DateTimeOffset.UtcNow.ToString("o");
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("Observe", MethodBase.GetCurrentMethod().Name);
                var parameter = new
                {
                    tpCustInfo = JsonConvert.SerializeObject(tpCustInfo),
                    amount = amount,
                    tpId = tpId,
                    tpTransId = tpTransId,
                    gameType = gameType,
                    refId = refId,
                    tpCategory = tpCategory,
                    afterAmount = afterAmount,
                    bonusId = bonusId,
                };
                log2File.Error($"{JsonConvert.SerializeObject(parameter)}\r\n{ex.Message}", MethodBase.GetCurrentMethod().Name, ex);
            }

            var t9 = DateTimeOffset.UtcNow.ToString("o");
            this.logger.LogWarning($"PlaceBetV2\r\nt1:{t1}\r\nt2:{t2}\r\nt3:{t3}\r\nt4:{t4}\r\nt5:{t5}\r\nt6:{t6}\r\nt7:{t7}\r\nt8:{t8}\r\nt9:{t9}");
            return result;
        }
    }
}
