using Microsoft.Extensions.Configuration;

namespace ThirdPartyService.IntegrationTest.Stubs
{
    public class StubStartUp : Startup
    {
        public StubStartUp(IConfiguration configuration) : base(configuration) { }
    }
}
