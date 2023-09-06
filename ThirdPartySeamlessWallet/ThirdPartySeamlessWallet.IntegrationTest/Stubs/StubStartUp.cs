using Microsoft.Extensions.Configuration;

namespace ThirdPartySeamlessWallet.IntegrationTest.Stubs
{
    public class StubStartUp : Startup
    {
        public StubStartUp(IConfiguration configuration) : base(configuration) { }
    }
}
