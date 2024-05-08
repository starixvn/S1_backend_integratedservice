using System.Text;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiRedTigerBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly string _tpName = Tp.RedTiger.ToString();

        public ApiRedTigerBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        public async Task<List<WagerEVOModel>> GetBetListByTime(RedTigerConfig setting, string startTime, string endTime)
        {
            var wagerUrl = setting.ApiUrl_Backup;
            var casinoKey = setting.CasinoKey;
            var gameHistoryApiToken = setting.GameHistoryApiToken;
            var gameProvider = setting.GameProvider;

            // param
            var url = $"{wagerUrl}/api/gamehistory/v1/casino/games/stream";
            var param = new
            {
                startDate = startTime,
                endDate = endTime,
                gameProvider = gameProvider
            };
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // request
            var r = await DoHttpClientAsync(url, sParam, casinoKey, gameHistoryApiToken);
            var result = JsonConvert.DeserializeObject<List<WagerEVOModel>>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param, string casinoKey, string ua2Token)
        {
            var tmpUrl = url;
            url = $"{url}?{param}";
            _log2File.InsertRequestLog(_tpName, tmpUrl, param, "1");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes($"{casinoKey}:{ua2Token}")));

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, param, result);
                return result;
            }

            if (string.IsNullOrEmpty(result))
            {
                var tmp = "{\"error\":\"nodata\"}";
                result = $"[{tmp}]";
            }
            else
            {
                var word = "}\n{";
                if (result.Contains(word))
                {
                    var tmp = result.Replace(word, "},{");
                    result = $"[{tmp}]";
                }
                else
                {
                    result = $"[{result}]";
                }
            }
            return result;
        }
    }
}
