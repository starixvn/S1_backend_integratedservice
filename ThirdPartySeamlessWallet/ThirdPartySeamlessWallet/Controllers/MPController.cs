using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class MPController : ControllerBase
    {
        private readonly ILogger<MPController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _apiUrl;

        public MPController(
            IServiceProvider provider,
            ILogger<MPController> logger,
            IConfiguration configuration,
            IHttpClientFactory clientFactory
        )
        {
            _logger = logger;
            _configuration = configuration;
            _clientFactory = clientFactory;
            _apiUrl = configuration.GetValue<string>("TpService");
        }

        [Route("[controller]")]
        [HttpGet]
        [HttpPost]
        public dynamic SeamlessWallet()
        {
            return true;
        }
    }
}
