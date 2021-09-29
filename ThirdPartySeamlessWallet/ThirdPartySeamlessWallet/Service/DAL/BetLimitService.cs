using System.Linq;
using ThirdPartySeamlessWallet.Service.DAL.Interface;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class BetLimitService
    {
        private readonly ICustBetLimitService _custBetLimitService;
        public int MinBet { get; set; }
        public int MaxBet { get; set; }
        public BetLimitService(ICustBetLimitService custBetLimitService)
        {
            _custBetLimitService = custBetLimitService;
        }
        
        public bool IsOutOfLimit(int custId, int tpCategory, int gameTypeId, decimal amount)
        {
            var betSetting = _custBetLimitService.GetCustBetLimit(custId);
            var category = betSetting?.BetSettings?.FirstOrDefault(a => a.TpCategory == tpCategory);
            if(category == null)
                return false;

            var gameType = category.TpBetSetting?.FirstOrDefault(a => a.GameTypeId == gameTypeId);
            if (gameType != null)
            {
                MinBet = gameType.MinBet;
                MaxBet = gameType.MaxBet;
            }
            MinBet = category.MinBet;
            MaxBet = category.MaxBet;
            
            if (MinBet != 0 && MaxBet != 0)
            {
                return amount < MinBet || amount > MaxBet;
            }
            return false;
        }
    }
}