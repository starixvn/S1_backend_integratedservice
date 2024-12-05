using System.Net;
using System.Text;

using Newtonsoft.Json;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiSWBLL : IApiSWBLL
    {
        private readonly string _seamlessUrl;
        private readonly ILogger<ApiSWBLL> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _client;

        public ApiSWBLL(IServiceProvider provider)
        {
            _configuration = provider.GetRequiredService<IConfiguration>();
            _seamlessUrl = provider.GetRequiredService<ConfigBLL>().GetServiceHostConfig().SeamlessService;
            _logger = provider.GetRequiredService<ILogger<ApiSWBLL>>();
            var httpHelper = provider.GetRequiredService<HttpHelper>();
            _client = httpHelper.GetClientWithHandler(CreateHandler());
        }

        public async Task<bool> MgUpdateBalance(MGUpdatebalanceRequest request)
        {
            try
            {
                var httpRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"{_seamlessUrl}MG/updatebalance"),
                    Headers = {
                        { "X-MGP-REQ-ID", Guid.NewGuid().ToString()},
                        { "X-MGP-REQUEST-TIMEMS", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()},
                    },
                    Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
                };
                var response = await _client.SendAsync(httpRequest);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"mg sw updatebalance failed, request{JsonConvert.SerializeObject(request)}");
                return false;
            }
        }

        private HttpClientHandler CreateHandler()
        {
            var handler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("ProxyHost")))
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(_configuration.GetValue<string>("ProxyHost"),
                    _configuration.GetValue<int>("ProxyPort"));
            }

            return handler;
        }
    }
}