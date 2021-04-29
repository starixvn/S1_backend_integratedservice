using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace ThirdPartySeamlessWallet.IntegrationTest.Stubs
{
    public class StubStartUp : Startup
    {
        public StubStartUp(IConfiguration configuration) : base(configuration)  {  }
    }
}
