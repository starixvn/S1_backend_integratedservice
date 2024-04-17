using System.Reflection;

using MySql.Data.MySqlClient;

using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class BettingService
    {
        private readonly WalletService _walletService;
        private readonly Tools _tools;
        private readonly ILogger<BettingService> _logger;
        private readonly MySqlClient _mainDbClient;
        private readonly PartitionService _partitionService;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly BonusWalletBLL _bonusWalletBLL;

        public BettingService(IServiceProvider provider)
        {
            _walletService = provider.GetRequiredService<WalletService>();
            _tools = provider.GetRequiredService<Tools>();
            _logger = provider.GetRequiredService<ILogger<BettingService>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _bonusWalletBLL = provider.GetRequiredService<BonusWalletBLL>();
        }

        public int AddTpBonus(int tpId, string transId, int gameType, int custId, string userName, int currencyId, int siteId, decimal useBonus, int bonusId, MySqlTransaction trans, SetTpSeamlessBonusType setTpSeamlessBonusType = SetTpSeamlessBonusType.Insert)
        {            
            if (bonusId == 0)
            {
                return 0;
            }

            if (useBonus <= 0)
            {
                return 0;
            }

            var custBonus = _bonusWalletBLL.GetCustBonus(bonusId);
            var tpSeamlessBonus = new TpSeamlessBonus
            {
                PromotionCategoryId = custBonus.PromoCategoryId,
                TpId = tpId,
                TransId = transId,
                GameType = gameType,
                CustId = custId,
                UserName = userName,
                CurrencyId = currencyId,
                SiteId = siteId,
                Stake = useBonus,
                BonusId = bonusId,
            };
            return AddTpSeamlessBonus(tpSeamlessBonus, trans, setTpSeamlessBonusType);
        }

        public int PlaceBet(TpCustInfo tpCustInfo, decimal amount, int tpId, string tpTransId, int gameType, string refId, out decimal afterAmount, ref int bonusId, out decimal useBonus, MySqlConnection conn, MySqlTransaction trans, string memo = "")
        {
            useBonus = 0;
            afterAmount = 0;
            int result = 0;
            try
            {
                var custBonus = _bonusWalletBLL.GetCustBonus(tpCustInfo.CustId, gameType);

                if (custBonus != null)
                {
                    bonusId = custBonus.BonusId;

                    // 用BonusId扣除CustBonus餘額
                    result = _walletService.SetBonus(gameType, tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake{memo}: {Math.Abs(amount):#,##0.##}", refId, out decimal afterBonus, ref bonusId, conn, trans);

                    // 新增tpSeamlessBonus紀錄
                    if (result > 0)
                    {
                        var stake = Math.Abs(amount);
                        useBonus = afterBonus < 0 ? stake + afterBonus : stake;
                        var tpSeamlessBonus = new TpSeamlessBonus
                        {
                            PromotionCategoryId = custBonus.PromoCategoryId,
                            TpId = tpId,
                            TransId = tpTransId,
                            GameType = gameType,
                            CustId = tpCustInfo.CustId,
                            UserName = tpCustInfo.UserName,
                            CurrencyId = tpCustInfo.CurrencyId,
                            SiteId = tpCustInfo.SiteId,
                            Stake = useBonus, // 紀錄實際使用的Bonus金額
                            BonusId = bonusId,
                        };
                        AddTpSeamlessBonus(tpSeamlessBonus, trans, SetTpSeamlessBonusType.Insert);
                    }

                    if (afterBonus < 0)
                    {
                        amount = afterBonus;
                        result = _walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake{memo}: {Math.Abs(amount):#,##0.##}", refId, out afterAmount, conn, trans);
                    }
                    else
                    {
                        afterAmount = afterBonus + _walletService.GetBalance(tpCustInfo.CustId);
                    }
                }
                else
                {
                    result = _walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake{memo}: {Math.Abs(amount):#,##0.##}", refId, out afterAmount, conn, trans);
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
                    afterAmount,
                    bonusId,
                };
                _logger.LogError(ex, $"{JsonConvert.SerializeObject(parameter)}\r\n{ex.Message}");
            }

            return result;
        }

        private int AddTpSeamlessBonus(TpSeamlessBonus tpSeamlessBonus, MySqlTransaction trans, SetTpSeamlessBonusType setTpSeamlessBonusType)
        {
            var sp = setTpSeamlessBonusType switch
            {
                SetTpSeamlessBonusType.Insert => "Service_TpSeamlessBonus_Ins",
                SetTpSeamlessBonusType.Update => "Service_TpSeamlessBonus_Set",
            };       
            return _mainDbClient.Execute(sp, trans, tpSeamlessBonus);
        }
    }
}
