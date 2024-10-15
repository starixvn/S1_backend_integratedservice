using Dapper;
using ThirdPartySeamlessWallet.Service.DAL;
using static ThirdPartySeamlessWallet.Controllers.CRMController;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class CRMBLL
    {
        private readonly CRMService _cRMService;
        public CRMBLL(IServiceProvider provider)
        {
            _cRMService = provider.GetRequiredService<CRMService>();
        }

        public void SetCustPromotionByScheduledCampaigns(List<CustomerAction> customerActionList)
        {
            foreach (var customerAction in customerActionList)
            {
                if (int.TryParse(customerAction.CustomerID, out int custId))
                {
                    SetCustPromotion(custId, customerAction.PromoCode);
                }
            }
        }
        public void SetCustPromotionByTriggeredCampaigns(TriggeredCampaignsEventData eventData)
        {
            if (int.TryParse(eventData.CustomerID, out int custId))
            {
                foreach (var promoCode in eventData.PromoCode)
                {
                    SetCustPromotion(custId, promoCode);
                }
            }
        }

        private void SetCustPromotion(int custId, string bonusCode)
        {
            _cRMService.SetCustPromotion(custId, bonusCode);
        }
    }
}
