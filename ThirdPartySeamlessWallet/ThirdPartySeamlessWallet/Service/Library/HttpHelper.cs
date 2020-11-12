using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Library
{
    public class HttpHelper
    {
        private readonly IHttpClientFactory clientFactory;
        private readonly IConfiguration configuration;
        public HttpHelper(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            this.clientFactory = clientFactory;
            this.configuration = configuration;
        }
        public HttpClient GetClient() => configuration.GetValue<string>("ProxyHost") != null ? clientFactory.CreateClient("with_proxy") : clientFactory.CreateClient();

    }
}
