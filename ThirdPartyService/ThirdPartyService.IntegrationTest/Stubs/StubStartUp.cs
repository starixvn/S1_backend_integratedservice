using ThirdPartyService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ThirdPartyService.IntegrationTest.Stubs
{
    public class StubStartUp : Startup
    {
        public StubStartUp(IConfiguration configuration) : base( configuration) { }
    }
}
