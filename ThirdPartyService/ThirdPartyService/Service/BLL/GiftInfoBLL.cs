using System.Security.Policy;
using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Model;
using static ThirdParty.EBet.TpHelper;

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

    public IEnumerable<WorkerPeriodTask> GetWorkList(string workerName)
    {
        return _giftInfoService.GetWorkList(workerName);
    }

    public int SetTaskDone(int periodTaskSiteId, int periodTaskCurrencyId, string workerName)
    {
        return _giftInfoService.SetTaskDone(periodTaskSiteId, periodTaskCurrencyId, workerName);
    }
}