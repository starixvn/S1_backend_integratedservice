using System;
using System.Collections.Generic;
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
        private readonly HttpHelper _httpHelper;
        private readonly ILogger<ApiSWBLL> _logger;
        
        public ApiSWBLL(IServiceProvider provider, IConfiguration configuration)
        {
            _url = configuration.GetValue<string>("TpSWserviceUrl");
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _logger = provider.GetRequiredService<ILogger<ApiSWBLL>>();
        }
        public async Task<bool> MgUpdateBalance(MGUpdatebalanceRequest request)
        {
            try
            {
                var client = _httpHelper.GetClient();
                client.DefaultRequestHeaders.Add("X-MGP-REQ-ID", Guid.NewGuid().ToString());
                client.DefaultRequestHeaders.Add("X-MGP-REQUEST-TIME", DateTime.UtcNow.Ticks.ToString());
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_url}/MG/updatebalance", content);
                return response.IsSuccessStatusCode;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"mg sw updatebalance failed, request{JsonConvert.SerializeObject(request)}");
                return false;
            }
        }
    }
}