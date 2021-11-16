using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class SabaOddsBLL
    {
        private readonly SabaOddsService sabaOddsService;

        public SabaOddsBLL(SabaOddsService sabaOddsService)
        {
            this.sabaOddsService = sabaOddsService;
        }
        public Task<int> SetSabaOdds(IEnumerable<UpdateOdd> sabaOdds)
        {
            return sabaOddsService.SetSabaOdds(sabaOdds);
        }
        public IEnumerable<int> GetSabaOddsMatchId()
        {
            return sabaOddsService.GetSabaOddsMatchId();
        }
    }
}
