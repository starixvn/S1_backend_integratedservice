using Dapper;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model.SBSW;
using static ThirdPartySeamlessWallet.Controllers.CRMController;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class CRMBLL
    {
        private readonly CRMService _cRMService;
        private readonly ILogger<CRMBLL> _logger;
        public CRMBLL(IServiceProvider provider)
        {
            _cRMService = provider.GetRequiredService<CRMService>();
            _logger = provider.GetRequiredService<ILogger<CRMBLL>>();
        }

        public void SetCustPromotionByScheduledCampaigns(List<CustomerAction> customerActionList)
        {
            foreach (var customerAction in customerActionList)
            {
                if (int.TryParse(customerAction.CustomerID, out int custId))
                {
                    try
                    {
                        SetCustPromotion(custId, customerAction.PromoCode);
                    }
                    catch (Exception ex)
                    {
                        var msg = $"SetCustPromotion(custId: {custId} promoCode: {customerAction.PromoCode})";
                        _logger.LogError($"{msg}\r\n{ex.Message}");
                    }
                }
            }
        }
        public void SetCustPromotionByTriggeredCampaigns(TriggeredCampaignsEventData eventData)
        {
            if (int.TryParse(eventData.CustomerID, out int custId))
            {
                foreach (var promoCode in eventData.PromoCode)
                {
                    try
                    {
                        SetCustPromotion(custId, promoCode);
                    }
                    catch(Exception ex)
                    {
                        var msg = $"SetCustPromotion(custId: {custId} promoCode: {promoCode})";
                        _logger.LogError($"{msg}\r\n{ex.Message}");
                    }
                }
            }
        }

        private int SetCustPromotion(int custId, string bonusCode)
        {
            return _cRMService.SetCustPromotion(custId, bonusCode);
        }
    }
}
