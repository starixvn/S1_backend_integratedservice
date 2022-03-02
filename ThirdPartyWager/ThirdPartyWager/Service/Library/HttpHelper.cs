using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace ThirdPartyWager.Service.Library
{
    public class HttpHelper
    {
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
        }

        public HttpClient GetClient() => client;

        public HttpClient GetClientWithHandler(HttpClientHandler handler) => new HttpClient(handler);
    }
}
