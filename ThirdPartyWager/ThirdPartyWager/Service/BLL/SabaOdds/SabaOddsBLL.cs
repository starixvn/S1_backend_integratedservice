using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class SabaOddsBLL
    {
        private readonly SabaOddsService _sabaOddsService;

        public SabaOddsBLL(IServiceProvider provider)
        {
            _sabaOddsService = provider.GetRequiredService<SabaOddsService>();
        }
        public Task<int> SetSabaOdds(IEnumerable<UpdateOdd> sabaOdds)
        {
            return _sabaOddsService.SetSabaOdds(sabaOdds);
        }
        public IEnumerable<int> GetSabaOddsMatchId()
        {
            return _sabaOddsService.GetSabaOddsMatchId();
        }
    }
}
