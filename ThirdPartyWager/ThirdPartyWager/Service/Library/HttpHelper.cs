using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ThirdPartyWager.Service.Library
{
    public class HttpHelper
    {
        private readonly HttpClient client;
        public HttpHelper(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            if (configuration.GetValue<string>("ProxyHost") != null)
            {
                client = clientFactory.CreateClient("with_proxy");
            }
            else
            {
                client = clientFactory.CreateClient();
            }
        }

        public HttpClient GetClient() => client;
    }
}
