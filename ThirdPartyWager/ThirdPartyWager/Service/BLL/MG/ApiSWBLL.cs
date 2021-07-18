using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL.MG
{
    public class ApiSWBLL : IApiSWBLL
    {
        private readonly string _url;
        private readonly ILogger<ApiSWBLL> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _client;
        
        public ApiSWBLL(IServiceProvider provider, IConfiguration configuration)
        {
            _configuration = configuration;
            _url = configuration.GetValue<string>("TpSWserviceUrl");
            _logger = provider.GetRequiredService<ILogger<ApiSWBLL>>();
            var httpHelper = provider.GetRequiredService<HttpHelper>();
            _client = httpHelper.GetClientWithHandler(CreateHandler());
        }
        public async Task<bool> MgUpdateBalance(MGUpdatebalanceRequest request)
        {
            try
            {
                _client.DefaultRequestHeaders.Add("X-MGP-REQ-ID", Guid.NewGuid().ToString());
                _client.DefaultRequestHeaders.Add("X-MGP-REQUEST-TIME", DateTime.UtcNow.Ticks.ToString());
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync($"{_url}/MG/updatebalance", content);
                return response.IsSuccessStatusCode;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"mg sw updatebalance failed, request{JsonConvert.SerializeObject(request)}");
                return false;
            }
        }
        private HttpClientHandler CreateHandler()
        {
            var handler = new HttpClientHandler();
            if (_configuration.GetValue<string>("ProxyHost") != null)
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(_configuration.GetValue<string>("ProxyHost"),
                    _configuration.GetValue<int>("ProxyPort"));
            }

            return handler;
        }
    }
}