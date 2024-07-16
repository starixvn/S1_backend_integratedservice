using System.Net;
using System.Text;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;
using static System.Net.Mime.MediaTypeNames;

namespace ThirdPartyWager.Service.BLL
{
    public interface INextspinApi
    {
        Task<NextspinBetHistory> GetBetHistory(string beginDate, string endDate, int page, NextspinConfig config);
        Task<NextspinProfitLoss> RetrieveProfitLoss(string beginDate, string endDate, int pageIndex, NextspinConfig config);
    }

    public class NextspinApi : INextspinApi
    {
        private readonly HttpHelper _httpHelper;
        private readonly IConfiguration _configuration;
        private readonly Log2File _log2File;
        private readonly string _tpName = Tp.Nextspin.ToString();

        public NextspinApi(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _configuration = provider.GetRequiredService<IConfiguration>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        public async Task<NextspinBetHistory> GetBetHistory(string beginDate, string endDate, int page, NextspinConfig config)
        {
            var merchantCode = config.MerchantCode;
            var apiUrl = config.ApiUrl;
            var request = new
            {
                merchantCode,
                beginDate,
                endDate,
                page
            };
            var json = JsonConvert.SerializeObject(request);

            try
            {
                var stringContent = new StringContent(
                    json,
                    Encoding.UTF8,
                    Application.Json
                );

                using var client = _httpHelper.GetClientWithHandler(CreateHandler());
                client.DefaultRequestHeaders.Add("API", "getBetHistory");
                client.DefaultRequestHeaders.Add("DataType", "JSON");
                _log2File.InsertRequestLog(_tpName, apiUrl, $"{json}\r\n{JsonConvert.SerializeObject(client.DefaultRequestHeaders)}", "1");

                var httpResponseMessage = await client.PostAsync(apiUrl, stringContent);

                var content = await httpResponseMessage.Content.ReadAsStringAsync();
                if (!httpResponseMessage.IsSuccessStatusCode)
                {
                    _log2File.InsertHttpErrorLog(_tpName, httpResponseMessage.StatusCode, httpResponseMessage.ReasonPhrase);
                    _log2File.InsertResponseLog(_tpName, apiUrl, json, httpResponseMessage.Content.ReadAsStringAsync().Result);
                    return null;
                }
                var nextspinBetHistory = JsonConvert.DeserializeObject<NextspinBetHistory>(content);
                if (nextspinBetHistory.Code != 0)
                {
                    _log2File.InsertHttpErrorLog(_tpName, nextspinBetHistory.Code, nextspinBetHistory.Msg);
                    _log2File.InsertResponseLog(_tpName, apiUrl, json, JsonConvert.SerializeObject(content));
                    return null;
                }
                return nextspinBetHistory;
            }
            catch (Exception ex)
            {
                _log2File.InsertResponseLog(_tpName, apiUrl, json, ex.Message);
                return null;
            }
        }

        public async Task<NextspinProfitLoss> RetrieveProfitLoss(string beginDate, string endDate, int pageIndex, NextspinConfig config)
        {
            var merchantCode = config.Agent;
            var apiUrl = config.ApiUrl;
            var request = new
            {
                beginDate,
                endDate,
                merchantCode,
                isAll = true,
                pageIndex
            };
            var json = JsonConvert.SerializeObject(request);

            try
            {
                var stringContent = new StringContent(
                    json,
                    Encoding.UTF8,
                    Application.Json
                );

                using var client = _httpHelper.GetClientWithHandler(CreateHandler());
                client.DefaultRequestHeaders.Add("API", "getProfitLoss");
                client.DefaultRequestHeaders.Add("DataType", "JSON");
                _log2File.InsertRequestLog(_tpName, apiUrl, $"{json}\r\n{JsonConvert.SerializeObject(client.DefaultRequestHeaders)}", "1");

                var httpResponseMessage = await client.PostAsync(apiUrl, stringContent);

                var content = await httpResponseMessage.Content.ReadAsStringAsync();
                if (!httpResponseMessage.IsSuccessStatusCode)
                {
                    _log2File.InsertHttpErrorLog(_tpName, httpResponseMessage.StatusCode, httpResponseMessage.ReasonPhrase);
                    _log2File.InsertResponseLog(_tpName, apiUrl, json, httpResponseMessage.Content.ReadAsStringAsync().Result);
                    return null;
                }
                var profitLoss = JsonConvert.DeserializeObject<NextspinProfitLoss>(content);
                if (profitLoss.Code != 0)
                {
                    _log2File.InsertHttpErrorLog(_tpName, profitLoss.Code, profitLoss.Msg);
                    _log2File.InsertResponseLog(_tpName, apiUrl, json, JsonConvert.SerializeObject(content));
                    return null;
                }
                return profitLoss;
            }
            catch (Exception ex)
            {
                _log2File.InsertResponseLog(_tpName, apiUrl, json, ex.Message);
                return null;
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
