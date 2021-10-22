using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiEVOBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiEVOBLL> _logger;
        private readonly string _logName = Tp.EVO.ToString();

        public ApiEVOBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiEVOBLL>>();
        }

        public async Task<List<WagerEVOModel>> GetBetListByTime(string wagerUrl, string casinoKey, string gameHistoryApiToken, string gameProvider, string startTime, string endTime, string guid)
        {
            // param
            string url = $"{wagerUrl}/api/gamehistory/v1/casino/games/stream";
            var param = new
            {
                startDate = startTime,
                endDate = endTime,
                gameProvider = gameProvider
            };
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // log
            _log2File.AddRequestLog(_logName, guid, url, sParam);

            // request
            string r = await DoHttpClientAsync(url, sParam, casinoKey, gameHistoryApiToken, guid);
            var result = JsonConvert.DeserializeObject<List<WagerEVOModel>>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param, string casinoKey, string ua2Token, string guid = null)
        {
            string tmpUrl = url;
            url = $"{url}?{param}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes($"{casinoKey}:{ua2Token}")));
            var response = await _httpHelper.GetClient().SendAsync(request);
            string result = null;
            string tmp = null;
            if (response.IsSuccessStatusCode)
            {
                result = response.Content.ReadAsStringAsync().Result;
                _log2File.AddResponseLog(_logName, guid, tmpUrl, param, result, "Info");

                if (string.IsNullOrEmpty(result))
                {
                    tmp = "{\"error\":\"nodata\"}";
                    result = $"[{tmp}]";
                }
                else
                {
                    string word = "error";
                    if (!result.Contains(word))
                    {
                        word = "}\n{";
                        if (result.Contains(word))
                        {
                            tmp = result.Replace(word, "},{");
                            result = $"[{tmp}]";
                        }
                        else
                        {
                            result = $"[{result}]";
                        }
                    }
                }
                return result;
            }
            _log2File.AddResponseLog(_logName, guid, tmpUrl, param, response, "Error");
            return string.IsNullOrEmpty(response.Content.ReadAsStringAsync().Result) ? null : response.Content.ReadAsStringAsync().Result;
        }
    }
}
