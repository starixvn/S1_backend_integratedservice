using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Data;
using System.Linq;
using System.Reflection;
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

        public int AddTpBonus(int tpId, string transId, int gameType, int custId, string userName, int currencyId, int siteId, decimal stake, int bonusId, TpCategory tpCategory, MySqlConnection conn, MySqlTransaction trans, SeamlessBonusType bonusType = SeamlessBonusType.TpCategory)
        {
            if (bonusId == 0)
                return 0;

            var spName = bonusType == SeamlessBonusType.TpCategory ? $"Service_Tp{tpCategory}Bonus_Ins" : $"Service_Tp_Bonus_Ins";
            int result = conn.Execute(spName, new { TpId = tpId, TransId = transId, GameType = gameType, CustId = custId, UserName = userName, CurrencyId = currencyId, SiteId = siteId, Stake = stake, BonusId = bonusId }, transaction: trans, commandType: CommandType.StoredProcedure);

            return result;
        }
        public int PlaceBetV2(TpCustInfo tpCustInfo, decimal amount, int tpId, string tpTransId, int gameType, string refId, string betData, TpCategory tpCategory, out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans, SeamlessBonusType bonusType = SeamlessBonusType.TpCategory)
        {
            afterAmount = 0;
            int result = 0;
            bool bHasBonusWallet = true;

            try
            {
                if (bHasBonusWallet)
                {
                    result = walletService.SetBonus(gameType, tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, ref bonusId, conn, trans);
                    if (bonusId > 0)
                    {
                        if (tpTransId != null)
                        {
                            AddTpBonus(tpId, tpTransId, gameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, amount, bonusId, tpCategory, conn, trans, bonusType);
                        }

                        if (afterAmount < 0)
                        {
                            amount = afterAmount;
                            result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
                        }
                        else
                        {
                            afterAmount += walletService.GetBalance(tpCustInfo.CustId);
                        }
                    }
                    else if (bonusId == 0)
                    {
                        result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
                    }
                }
                else
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
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

            return result;
        }
    }
}
