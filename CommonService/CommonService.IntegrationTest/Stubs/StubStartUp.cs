using Microsoft.Extensions.Configuration;

namespace CommonService.IntegrationTest.Stubs
{
    public class StubStartUp : Startup
    {
        public StubStartUp(IConfiguration configuration) : base(configuration) { }
    }
}
