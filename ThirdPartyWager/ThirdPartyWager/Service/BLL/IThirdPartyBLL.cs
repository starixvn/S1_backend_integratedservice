using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public interface IThirdPartyBLL
    {
        ThirdPartyTimerModel GetThirdPartyTimer(int tpId, int type);
    }
}