using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{
    public class SabaOddsBLL
    {
        private readonly SabaOddsService sabaOddsService;

        public SabaOddsBLL(SabaOddsService sabaOddsService)
        {
            this.sabaOddsService = sabaOddsService;
        }
        public int SetSabaOdds(string sabaOddsJson)
        {
            return sabaOddsService.SetSabaOdds(sabaOddsJson);
        }
    }
}
