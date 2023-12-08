using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class BonusWalletBLL
    {
        private readonly MySqlClient _mainDbClient;

        public BonusWalletBLL(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        /// <summary>
        /// 使用GameType找到用戶還在Running的CustBonus
        /// </summary>
        public CustBonus GetCustBonus(int CustId, int GameType)
        {
            if (GameType == 0)
            {
                return default;
            }

            return _mainDbClient.QueryFirst<CustBonus>("Service_CustBonus_Get", parameters: new { CustId, GameType });
        }

        /// <summary>
        /// 使用BonusId找到用戶的CustBonus
        /// </summary>
        public CustBonus GetCustBonus(int BonusId)
        {
            return _mainDbClient.QueryFirst<CustBonus>("Service_CustBonus_GetById", parameters: new { BonusId });
        }
    }
}
