using System.Net;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.Library
{
    public class HttpHelper
    {
        private readonly HttpClient clientWithProxy;
        private readonly HttpClient client;
        private readonly ILogger<HttpHelper> _logger;
        private readonly IConfiguration _configuration;

        public HttpHelper(IHttpClientFactory clientFactory, IConfiguration configuration, IServiceProvider provider)
        {
            clientWithProxy = !string.IsNullOrEmpty(configuration.GetValue<string>("ProxyHost"))
                ? clientFactory.CreateClient("with_proxy")
                : clientFactory.CreateClient();
            client = clientFactory.CreateClient();
            _configuration = provider.GetRequiredService<IConfiguration>();
            _logger = provider.GetRequiredService<ILogger<HttpHelper>>();
        }

        public HttpClient GetClient() => client;

        public HttpClient GetClientWithProxy() => clientWithProxy;

        public HttpClient GetClientWithHandler(HttpClientHandler handler) => new HttpClient(handler);

        public HttpClientHandler CreateProxyConfigHandler(ProxyConfig proxyConfig)
        {
            var handler = new HttpClientHandler();
            string proxyHost;
            int proxyPort;

            if (proxyConfig != null)
            {
                handler.UseProxy = true;
                proxyHost = proxyConfig.ProxyHost;
                proxyPort = proxyConfig.ProxyPort;
                handler.Proxy = new WebProxy(proxyHost, proxyPort);

                _logger.LogInformation($"use proxy, host:{proxyHost}, port:{proxyPort}");

                return handler;
            }

            proxyHost = _configuration.GetValue<string>("ProxyHost");
            proxyPort = _configuration.GetValue<int>("ProxyPort");

            if (!string.IsNullOrEmpty(proxyHost))
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(proxyHost, proxyPort);

                _logger.LogInformation($"use proxy, host:{proxyHost}, port:{proxyPort}");
            }

            return handler;
        }
    }
}