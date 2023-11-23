using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class TpUnSettledBLL
    {
        private readonly TpUnsettleService _tpUnsettleService;

        public TpUnSettledBLL(IServiceProvider provider)
        {
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
        }

        /// <summary>
        /// 從TpUnsettle取出第三方待結算資料
        /// </summary>
        public IEnumerable<TpUnsettle> GetTpUnsettleList(Tp tp, GameType gameType, int LimitCount = 200)
        {
            return _tpUnsettleService.GetTpUnsettleList(tp, gameType, LimitCount);
        }
    }
}
