using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiPlayTechBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiPlayTechBLL> _logger;
        private readonly string _tpName = Tp.PlayTech.ToString();

        public ApiPlayTechBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiPlayTechBLL>>();
        }

        public async Task<WagerPlayTechModel> GetBetListByTime(PlayTechConfig config, string startTime, string endTime, int page)
        {
            string wagerUrl = config.WagerUrl;
            string brandId = config.BrandId;
            string SecretKey = config.SecretKey;
            string uri = "/history/game";
            // param
            var param = new
            {
                requestId = Guid.NewGuid().ToString(),
                brandId = brandId,
                startTime = startTime,
                endTime = endTime,
                size = 500,
                page = page,
            };
            var dicP = _common.FromJson<Dictionary<string, dynamic>>(_common.ToJson(param));
            string sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // encrypt
            string hash = EncryptString(ExceptHashAndSort(dicP) + SecretKey);
            string url = $"{wagerUrl}{uri}?hash={hash}";

            // request
            string r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<WagerPlayTechModel>(r);
            return result;
        }

        public async Task<CurrencyStats_PlayTech> GetBetSummaryByTime(PlayTechConfig config, string startTime, string endTime)
        {
            string wagerUrl = config.WagerUrl;
            string brandId = config.BrandId;
            string SecretKey = config.SecretKey;
            string uri = "/history/gameReport";
            // param
            var param = new
            {
                requestId = Guid.NewGuid().ToString(),
                brandId = brandId,
                startTime = startTime,
                endTime = endTime,
            };
            var dicP = _common.FromJson<Dictionary<string, dynamic>>(_common.ToJson(param));
            string sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // encrypt
            string hash = EncryptString(ExceptHashAndSort(dicP) + SecretKey);
            string url = $"{wagerUrl}{uri}?hash={hash}";

            // request
            string r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<CurrencyStats_PlayTech>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, object param)
        {
            var json = JsonConvert.SerializeObject(param);
            _log2File.InsertRequestLog(_tpName, url, json, "1");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;
            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, json, result);
                return result;
            }

            return result;
        }

        private static string ExceptHashAndSort(Dictionary<string, dynamic> requestDict)
        {
            requestDict.Remove("hash");
            Dictionary<string, dynamic> sortedDict = requestDict.OrderBy(o => o.Key).ToDictionary(o => o.Key, p => p.Value);
            string plain = "";
            foreach (KeyValuePair<string, dynamic> dict in sortedDict)
            {
                plain += dict.Key + "=";

                if (dict.Value.GetType() == typeof(Newtonsoft.Json.Linq.JArray))
                {
                    plain += JsonConvert.SerializeObject(dict.Value, Formatting.None);
                }
                else
                {
                    plain += dict.Value;
                }

                plain += "&";
            }
            return plain.Substring(0, plain.Length - 1);
        }

        private static string EncryptString(string str)
        {
            MD5 md5 = MD5.Create();
            byte[] byteOld = Encoding.UTF8.GetBytes(str);
            byte[] byteNew = md5.ComputeHash(byteOld);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in byteNew)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
