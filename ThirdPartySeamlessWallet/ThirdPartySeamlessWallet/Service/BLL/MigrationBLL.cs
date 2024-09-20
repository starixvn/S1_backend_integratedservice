using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class MigrationBLL
    {
        private readonly MigrationService _migrationService;
            
        public MigrationBLL(IServiceProvider provider)
        {
            _migrationService = provider.GetRequiredService<MigrationService>();
        }

        public async Task<(int , int)> MigrationCustomerIns(MigrationCustomerRequest request , int agent ,string allAgent)
        {
            return await _migrationService.MigrationCustomerIns(request, agent, allAgent);
        }
    }
}
