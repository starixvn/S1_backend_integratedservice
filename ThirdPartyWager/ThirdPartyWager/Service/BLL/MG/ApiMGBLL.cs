using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL.MG
{
    public class ApiMGBLL : IApiMGBLL
    {
        public Dictionary<string, string> MgConfig { get; set; }
        private readonly ILogger<ApiMGBLL> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _client;

        public ApiMGBLL(IServiceProvider provider, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = provider.GetRequiredService<ILogger<ApiMGBLL>>();
            var httpHelper = provider.GetRequiredService<HttpHelper>();
            _client = httpHelper.GetClientWithHandler(CreateHandler());
        }

        public async Task<List<MGFailedTransactions>> GetFailedTransactionsAsync(string token, string currencyId)
        {
            var apiUrl = GetInputValue(MgConfig, "ApiUrl");
            var strAgents = GetInputValue(MgConfig, "Agent");
            var mgAgents = JsonConvert.DeserializeObject<List<MGAgents>>(strAgents);
            var agent = mgAgents.SingleOrDefault(a => a.Currency == currencyId);

            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _client.GetAsync($"{apiUrl}/agents/{agent.AgentCode}/failedTransactions");
                var result = await response.Content.ReadAsStringAsync();
                var failedTransactions = JsonConvert.DeserializeObject<List<MGFailedTransactions>>(result);
                return failedTransactions;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "get mg failed transactions failed");
                return null;
            }
        }

        public async Task UpdateFailedTransactionToResolvedState(string token, string txnId, string currencyId)
        {
            var apiUrl = GetInputValue(MgConfig, "ApiUrl");
            var strAgents = GetInputValue(MgConfig, "Agent");
            var mgAgents = JsonConvert.DeserializeObject<List<MGAgents>>(strAgents);
            var agent = mgAgents.SingleOrDefault(a => a.Currency == currencyId);

            try
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _client.PatchAsync($"{apiUrl}/agents/{agent.AgentCode}/failedTransactions/{txnId}", null);
                _logger.LogInformation($"update mg failed transactions {response.StatusCode}");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Update failed transaction to resolved state failed");
            }
        }

        public async Task<string> GetToken(string tpCurrencyId)
        {
            try
            {
                var tokenUrl = GetInputValue(MgConfig, "TokenUrl");
                var strAgents = GetInputValue(MgConfig, "Agent");
                var mgAgents = JsonConvert.DeserializeObject<List<MGAgents>>(strAgents);
                var agent = mgAgents.SingleOrDefault(a => a.Currency == tpCurrencyId);

                var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", agent.AgentCode },
                    { "client_secret", agent.SecretKey },
                    { "grant_type", "client_credentials" }
                });
                var response = await _client.PostAsync($"{tokenUrl}/connect/token", httpContent);
                var tokenResult = await response.Content.ReadAsStringAsync();
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult);
                return dict.ContainsKey("access_token") ? dict["access_token"] : string.Empty;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "get mg token failed");
                return string.Empty;
            }
        }
        private string GetInputValue(Dictionary<string, string> inputs, string key)
        {
            if(inputs.ContainsKey(key) && !string.IsNullOrEmpty(inputs[key]))
            {
                return inputs[key];
            }
            throw new Exception();
        }
        private class MGAgents
        {
            public string Currency { get; set; }
            public string MasterAgent { get; set; }
            public string AgentCode { get; set; }
            public string SecretKey { get; set; }
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