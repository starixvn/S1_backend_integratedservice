using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Options;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class BonusWalletBLL
    {
        private readonly Dictionary<int, List<int>> _categoryGameType;
        private readonly MySqlClient _mainDbClient;

        public BonusWalletBLL(IServiceProvider provider)
        {
            _categoryGameType = provider.GetRequiredService<IEnumerable<PromotionCategoryGameTypeOption>>()
                .GroupBy(category => category.PromotionCategoryId)
                .ToDictionary(i => i.Key, i => i.Select(i => i.GameType).ToList());
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public CustBonus GetCustBonus(int custId, int gameType)
        {
            if (gameType == 0 )
            {
                return default;
            }

            var custBonus = GetCustBonus(custId);
            if (_categoryGameType.TryGetValue(custBonus.PromoCategoryId, out var supportGameTypeList)
                && supportGameTypeList.Contains(gameType))
            {
                return custBonus;
            }
            else
            {
                return default;
            }
        }

        public CustBonus GetCustBonusByBonusId(int BonusId)
        {
            return _mainDbClient.QueryFirst<CustBonus>("Service_CustBonus_GetById", parameters: new { BonusId });
        }


        /// <summary>
        /// 找到用戶還在Running的CustBonus
        /// </summary>
        private CustBonus GetCustBonus(int CustId)
        {
            return _mainDbClient.QueryFirst<CustBonus>("Service_CustBonus_Get", parameters: new { CustId });
        }
    }
}
