using Newtonsoft.Json;
using System.Text;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiDragonGamingBLL
    {
        private readonly string _tpName = Tp.DragonGaming.ToString();

        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        public ApiDragonGamingBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        public async Task<GameHistoryResponse> GetWagerAsync(string apiUrl, string apiKey, string startTime, string endTime)
        {
            // param
            var url = $"{apiUrl}/games/game-history-all-players/";
            var param = new
            {
                api_key = apiKey,
                amount_type = "real",
                start_date = startTime.ToString(),
                end_date = endTime.ToString(),
            };

            // request
            var r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<GameHistoryResponse>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, object param)
        {
            _log2File.InsertRequestLog(_tpName, url, JsonConvert.SerializeObject(param), "1");

            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(sParam, Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, JsonConvert.SerializeObject(param), result);
            }

            return result;
        }

    }
}
