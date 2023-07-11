using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Serilog.Context;
using System.Data;
using System.Reflection;

using ThirdPartySeamlessWallet.Service.BLL;
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
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly PartitionService _partitionService;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly ThirdPartyBLL _thirdPartyBLL;

        public BettingService(IServiceProvider provider)
        {
            walletService = provider.GetRequiredService<WalletService>();
            tools = provider.GetRequiredService<Tools>();
            logger = provider.GetRequiredService<ILogger<BettingService>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        public int AddTpBonus(int tpId, string transId, int gameType, int custId, string userName, int currencyId, int siteId, decimal stake, int bonusId, TpCategory tpCategory, MySqlConnection conn, MySqlTransaction trans, SeamlessBonusType bonusType = SeamlessBonusType.TpCategory)
        {
            if (bonusId == 0)
            {
                return 0;
            }
            
            //用GameType取回PromoCategoryId，在新增到TpSeamlessBonus
            var tpGameType = _mainDbClient.QueryFirst<TpGameType>("Service_TpGameType_GetByType", new { GameType = gameType });
            var tpSeamlessBonus = new TpSeamlessBonus
            {
                PromotionCategoryId = (int)tpGameType.PromoCategoryId,
                TpId = tpId,
                TransId = transId,
                GameType = gameType,
                CustId = custId,
                UserName = userName,
                CurrencyId = currencyId,
                SiteId = siteId,
                Stake = stake,
                BonusId = bonusId,
            };
            return AddTpSeamlessBonus(tpSeamlessBonus, conn, trans);
        }

        public int AddTpSeamlessBonus(TpSeamlessBonus tpSeamlessBonus, MySqlConnection conn, MySqlTransaction trans)
        {
            return _mainDbClient.Execute("Service_TpSeamlessBonus_Ins", trans, tpSeamlessBonus);
        }

        public int PlaceBetV2(TpCustInfo tpCustInfo, decimal amount, int tpId, string tpTransId, int gameType, string refId, string betData, TpCategory tpCategory, out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans, SeamlessBonusType bonusType = SeamlessBonusType.TpCategory, string memo = "")
        {
            afterAmount = 0;
            int result = 0;
            try
            {
                var bonusCategory = _thirdPartyBLL.GetBonusCategory(gameType);

                // 1.先找到用戶還在Running的CustBonusList
                // 2.再找出符合條件的找到用戶還在Running的CustBonus加到Balance
                // 符合條件:
                //  1.CustBonus.PromoCategoryId = TpGameType.PromoCategoryId
                //  2.CustBonus.PromoCategoryId = 0 (不分類皆可使用，但不包含捕魚類遊戲)
                var custBonus = walletService.GetCustBonusList(tpCustInfo.CustId)
                    .FirstOrDefault(i => (int)i.PromoCategoryId == bonusCategory.Id ||
                    (i.PromoCategoryId == PromotionCategory.ALL && bonusCategory.CategoryId != (int)TpCategory.Fish));

                if (custBonus != null)
                {
                    bonusId = custBonus.BonusId;

                    // 用BonusId扣除CustBonus餘額
                    result = walletService.SetBonus(gameType, tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake{memo}: {Math.Abs(amount):#,##0.##}", refId, out afterAmount, ref bonusId, conn, trans);

                    // 新增tpSeamlessBonus紀錄
                    if (result > 0)
                    {
                        var stake = Math.Abs(amount);
                        var tpSeamlessBonus = new TpSeamlessBonus
                        {
                            PromotionCategoryId = (int)custBonus.PromoCategoryId,
                            TpId = tpId,
                            TransId = tpTransId,
                            GameType = gameType,
                            CustId = tpCustInfo.CustId,
                            UserName = tpCustInfo.UserName,
                            CurrencyId = tpCustInfo.CurrencyId,
                            SiteId = tpCustInfo.SiteId,
                            Stake = afterAmount < 0 ? stake + afterAmount : stake, // 紀錄實際使用的Bonus金額
                            BonusId = bonusId,
                        };
                        AddTpSeamlessBonus(tpSeamlessBonus, conn, trans);
                    }

                    if (afterAmount < 0)
                    {
                        amount = afterAmount;
                        result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake{memo}: {Math.Abs(amount):#,##0.##}", refId, out afterAmount, conn, trans);
                    }
                    else
                    {
                        afterAmount += walletService.GetBalance(tpCustInfo.CustId);
                    }
                }
                else
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake{memo}: {Math.Abs(amount):#,##0.##}", refId, out afterAmount, conn, trans);
                }
            }
            catch (Exception ex)
            {
                result = 0;
                LogContext.PushProperty("Observe", MethodBase.GetCurrentMethod().Name);
                var parameter = new
                {
                    tpCustInfo = JsonConvert.SerializeObject(tpCustInfo),
                    amount,
                    tpId,
                    tpTransId,
                    gameType,
                    refId,
                    tpCategory,
                    afterAmount,
                    bonusId,
                };
                logger.LogError(ex, $"{JsonConvert.SerializeObject(parameter)}\r\n{ex.Message}");
            }

            return result;
        }
    }
}
