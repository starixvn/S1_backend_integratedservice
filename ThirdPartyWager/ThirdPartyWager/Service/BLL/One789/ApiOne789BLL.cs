using System.Text;
using System.Web;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiOne789BLL
    {
        private readonly string _seamlessUrl;
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiOne789BLL> _logger;
        private readonly string _tpName = Tp.One789.ToString();

        public ApiOne789BLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiOne789BLL>>();
            _seamlessUrl = provider.GetRequiredService<ConfigBLL>().GetServiceHostConfig().SeamlessService;
        }

        public async Task<dynamic> GetBetListByTime(One789Config setting, string startTime, string endTime, int page, int perPage, int gameType)
        {
            // param
            var uri = "";
            switch (gameType)
            {
                case (int)GameType.One789_Lottery:
                    uri = "/report/lottery/date-time-report";
                    break;

                case (int)GameType.One789_Lottery_789:
                    uri = "/report/lottery789/date-time-report";
                    break;

                case (int)GameType.One789_LiveCasino:
                    uri = "/report/789-casino/sedie/date-time-report";
                    break;

                default:
                    break;
            }

            var url = $"{setting.ApiUrl}{uri}";
            var param = $"?from={startTime}&to={endTime}&type=1&size={perPage}&page={page}";

            // request
            var r = await DoHttpClientAsync(HttpMethod.Get, setting, url, param);
            var result = JsonConvert.DeserializeObject(r);
            return result;
        }

        public async Task<dynamic> CheckTicketStatus(One789Config setting, dynamic parameters)
        {
            // param
            var uri = "/transaction/exists";
            var url = $"{setting.ApiUrl}{uri}";

            // param
            var param = JsonConvert.SerializeObject(new { TXList = parameters });

            // request
            return await DoHttpClientAsync(HttpMethod.Post, setting, url, param);
        }

        private async Task<string> DoHttpClientAsync(HttpMethod method, One789Config setting, string url, string param)
        {
            var path = (method == HttpMethod.Get) ? $"{url}{param}" : url;

            _log2File.InsertRequestLog(_tpName, path, param, "1");

            var request = new HttpRequestMessage(method, path);
            request.Headers.Add("x-one-api-key", HttpUtility.UrlEncode(setting.ApiKey));
            if (method == HttpMethod.Post)
            {
                request.Content = new StringContent(param, Encoding.UTF8, "application/json");
            }
            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                result = $"[{response.Content.ReadAsStringAsync().Result}]";
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, path, param, result);
            }

            return result;
        }

        public async Task<string> callSeamlessApi(string type, BettransDataOne789 bettransDataOne789)
        {
            // param
            var url = $"{_seamlessUrl}One789";
            var uri = "/balance_transactions";
            var tag = true;
            var param = string.Empty;

            switch (type)
            {
                case "Refund":
                    var data = new
                    {
                        Tx = bettransDataOne789.Source,
                        Username = bettransDataOne789.Username,
                        Amount = Math.Abs(bettransDataOne789.Amount),
                        Type = type.ToLower(),
                        CreatedAt = DateTimeOffset.Now.ToUnixTimeSeconds(),
                        Source = bettransDataOne789.Tx,
                    };
                    param = JsonConvert.SerializeObject(data);
                    break;

                default:
                    tag = false;
                    _logger.LogWarning($"{type} not found");
                    break;
            }

            if (!tag || param == string.Empty)
            {
                return null;
            }

            // request
            var seamlessApi = $"{url}{uri}";
            return await DoSeamlessHttpClientAsync(seamlessApi, param);
        }

        public async Task<string> DoSeamlessHttpClientAsync(string url, dynamic param)
        {
            _log2File.InsertRequestLog(_tpName, url, param, "1");

            HttpRequestMessage request;
            request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/json");

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, param, result);
            }

            return result;
        }
    }
}