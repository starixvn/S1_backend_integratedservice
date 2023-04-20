using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Serilog.Context;
using System.Data;
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
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly PartitionService _partitionService;
        private readonly TpUnsettleService _tpUnsettleService;

        public BettingService(IServiceProvider provider)
        {
            walletService = provider.GetRequiredService<WalletService>();
            tools = provider.GetRequiredService<Tools>();
            logger = provider.GetRequiredService<ILogger<BettingService>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
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
            CustBonus custBonus = default;
            try
            {
                // 傳進來的bonusId = 0 時，先查詢用戶是否有還在Running的CustBonus
                if (bonusId == 0)
                {
                    // 1.先找到用戶還在Running的CustBonusList
                    // 2.找出符合條件的Bonus才可以使用
                    custBonus = walletService.GetCustBonusList(tpCustInfo.CustId)
                        .FirstOrDefault(i => i.GameType == gameType || i.PromoCategoryId == PromotionCategory.ALL);
                    if (custBonus != null)
                    {
                        bonusId = custBonus.BonusId;
                    }                  
                }
                
                if (bonusId > 0)
                {
                    // 用BonusId扣除CustBonus餘額
                    result = walletService.SetBonus(gameType, tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake{memo}: {Math.Abs(amount):#,##0.##}", refId, out afterAmount, ref bonusId, conn, trans);

                    //新增tpSeamlessBonus紀錄
                    if (tpTransId != null && custBonus != null)
                    {
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
                            Stake = amount,
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
