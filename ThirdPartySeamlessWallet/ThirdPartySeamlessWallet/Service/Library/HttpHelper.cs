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
        private readonly HttpClient _clientWithoutProxy;
        private readonly HttpClient client;
        public HttpHelper(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            if (!string.IsNullOrEmpty(configuration.GetValue<string>("ProxyHost")))
            {
                client = clientFactory.CreateClient("with_proxy");
            }
            else
            {
                client = clientFactory.CreateClient();
            }
            _clientWithoutProxy = clientFactory.CreateClient();
        }

        public HttpClient GetClient() => client;
        public HttpClient GetWithoutProxyClient() => _clientWithoutProxy;

        public HttpClient GetClientWithHandler(HttpClientHandler handler) => new HttpClient(handler);
    }
}
