using ThirdPartyCore;
using ThirdPartyService.Service.DAL;

namespace ThirdPartyService.Service.BLL
{
    public class TpCustInfoBLL : ITpCustInfoProvider
    {
        private readonly TpCustInfoService tpCustInfoService;

        public TpCustInfoBLL(TpCustInfoService tpCustInfoService)
        {
            this.tpCustInfoService = tpCustInfoService;
        }
        public void SetTpLastLoginTime(int custId, int tpId)
        {
            tpCustInfoService.SetTpLastLoginTime(custId, tpId);
        }
        public void SetTpLastTradingTime(int custId, int tpId)
        {
            tpCustInfoService.SetTpLastTradingTime(custId, tpId);
        }
        public void SetCustGotoGame(int custId, int tpId, int gameType)
        {
            tpCustInfoService.SetCustGotoGame(custId, tpId, gameType);
        }
    }
}
