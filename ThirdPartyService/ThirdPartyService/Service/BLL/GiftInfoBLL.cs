using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Service.BLL;

public class GiftInfoBLL
{
    private readonly GiftInfoService _giftInfoService;

    public GiftInfoBLL(GiftInfoService giftInfoService)
    {
        _giftInfoService = giftInfoService;
    }

    public IEnumerable<GiftInfo> GetGiftList(int tpId)
    {
        return _giftInfoService.GetGiftList(tpId);
    }
}