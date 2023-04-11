using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Serilog.Context;
using System.Reflection;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class BettingService
    {
        private readonly WalletService walletService;
        private readonly Tools tools;
        private readonly ILogger<BettingService> logger;
        private readonly Log2File log2File;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly PartitionService _partitionService;
        private readonly TpUnsettleService _tpUnsettleService;

        public BettingService(IServiceProvider provider)
        {
            walletService = provider.GetRequiredService<WalletService>();
            tools = provider.GetRequiredService<Tools>();
            logger = provider.GetRequiredService<ILogger<BettingService>>();
            log2File = provider.GetRequiredService<Log2File>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
        }

        public int AddTpBonus(int tpId, string transId, int gameType, int custId, string userName, int currencyId, int siteId, decimal stake, int bonusId, TpCategory tpCategory, MySqlConnection conn, MySqlTransaction trans, SeamlessBonusType bonusType = SeamlessBonusType.TpCategory)
        {
            if (bonusId == 0)
                return 0;

            var spName = bonusType == SeamlessBonusType.TpCategory ? $"Service_Tp{tpCategory}Bonus_Ins" : $"Service_Tp_Bonus_Ins";
            var result = _mainDbClient.Execute(spName, new { TpId = tpId, TransId = transId, GameType = gameType, CustId = custId, UserName = userName, CurrencyId = currencyId, SiteId = siteId, Stake = stake, BonusId = bonusId }, conn, trans);

            return result;
        }
        
        public int PlaceBetV2(TpCustInfo tpCustInfo, decimal amount, int tpId, string tpTransId, int gameType, string refId, string betData, TpCategory tpCategory, out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans, SeamlessBonusType bonusType = SeamlessBonusType.TpCategory, string memo = "")
        {
            afterAmount = 0;
            var result = 0;
            var bHasBonusWallet = true;

            try
            {
                if (bHasBonusWallet)
                {
                    result = walletService.SetBonus(gameType, tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake{memo}: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, ref bonusId, conn, trans);
                    if (bonusId > 0)
                    {
                        if (tpTransId != null)
                        {
                            AddTpBonus(tpId, tpTransId, gameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, amount, bonusId, tpCategory, conn, trans, bonusType);
                        }

                        if (afterAmount < 0)
                        {
                            amount = afterAmount;
                            result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake{memo}: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
                        }
                        else
                        {
                            afterAmount += walletService.GetBalance(tpCustInfo.CustId);
                        }
                    }
                    else if (bonusId == 0)
                    {
                        result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake{memo}: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
                    }
                }
                else
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake{memo}: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
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
