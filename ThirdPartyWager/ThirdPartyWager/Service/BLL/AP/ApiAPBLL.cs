using System.Security.Cryptography;
using System.Text;
using System.Web;

using Newtonsoft.Json;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiAPBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiAPBLL> _logger;
        private readonly string _tpName = Tp.AP.ToString();

        public ApiAPBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiAPBLL>>();
        }

        public async Task<BetList_AdvantPlayMini> GetBetListByTime(dynamic setting, string startTime, string endTime, int page)
        {
            var apiUrl = setting["WagerUrl_MiniGame"];
            var secretKey = setting["SecretKey_MiniGame"];
            var apiKey = setting["APIKey_MiniGame"];

            // param
            var url = $"{apiUrl}/api/v2/client/history";

            var param = new
            {
                dateTimeFrom = startTime,
                dateTimeTo = endTime,
                pageNo = page,
                pageSize = 500,
            };

            // log
            _logger.LogInformation(JsonConvert.SerializeObject(param));

            var payload = AesEncryptV2(_common.ToJson(param), secretKey);
            var signature = HmacSha256(payload, secretKey);

            var sParam = $"payload={HttpUtility.UrlEncode(payload)}&signature={HttpUtility.UrlEncode(signature)}";

            // request
            var r = await DoHttpClientAsync(url, sParam, HttpMethod.Get, true, apiKey);
            if (r == null || r == "")
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<BetList_AdvantPlayMini>(r);
            return result;
        }

        public async Task<CurrencyStats_AdvantPlayMini> GetBetListByTimeOfMini(dynamic setting, string startTime, string endTime)
        {
            var apiUrl = setting["WagerUrl_MiniGame"];
            var secretKey = setting["SecretKey_MiniGame"];
            var apiKey = setting["APIKey_MiniGame"];

            // param
            var url = $"{apiUrl}/api/v2/Client/summary";

            var param = new
            {
                dateTimeFrom = DateTime.Parse(startTime).ToString("yyyy-MM-dd HH:mm"),
                dateTimeTo = DateTime.Parse(endTime).ToString("yyyy-MM-dd HH:mm"),
            };

            // log
            _logger.LogInformation(JsonConvert.SerializeObject(param));

            var payload = AesEncryptV2(_common.ToJson(param), secretKey);
            var signature = HmacSha256(payload, secretKey);

            var sParam = $"payload={HttpUtility.UrlEncode(payload)}&signature={HttpUtility.UrlEncode(signature)}";

            // request
            var r = await DoHttpClientAsync(url, sParam, HttpMethod.Get, true, apiKey);
            if (r == null || r == "")
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<CurrencyStats_AdvantPlayMini>(r);
            return result;
        }

        public async Task<CurrencyStats_AP> GetBetSummaryByTime(dynamic setting, string startTime, string endTime)
        {
            var apiUrl = setting["ApiUrl"];
            var siteCode = setting["SiteCode"];

            // param
            var url = $"{apiUrl}GetTransSummary";
            string uuid = Guid.NewGuid().ToString();
            string Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffffzzz");

            var param = new
            {
                Timestamp = Timestamp,
                Seq = uuid,
                SiteCode = siteCode,
                DateFrom = startTime,
                DateTo = endTime
            };

            var sParam = JsonConvert.SerializeObject(param);

            // request
            var r = await DoHttpClientAsync(url, sParam, HttpMethod.Post, false);
            var result = JsonConvert.DeserializeObject<CurrencyStats_AP>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param, HttpMethod method, bool header, string key = null)
        {

            HttpRequestMessage request;

            if (method == HttpMethod.Get)
            {
                url = $"{url}?{param}";
                request = new HttpRequestMessage(method, url);
            }
            else
            {
                request = new HttpRequestMessage(method, url);
                request.Content = new StringContent(param, Encoding.UTF8, "application/json");
            }

            if (header)
            {
                request.Headers.Add("X-API-KEY", key);
                _log2File.InsertRequestLog(_tpName, url, $"{param}\r\n{JsonConvert.SerializeObject(request.Headers)}", "1");
            }
            else
            {
                _log2File.InsertRequestLog(_tpName, url, param, "1");
            }

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;
            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, param, result);
            }

            return result;
        }

        private string HmacSha256(string json, string key)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                var encryptValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(json));
                return Convert.ToBase64String(encryptValue);
            };
        }

        private string AesEncryptV2(string jsonObject, string secretKey)
        {
            byte[] toEncryptArray = Encoding.UTF8.GetBytes(jsonObject);
            byte[] keyBytes = new byte[16];
            byte[] secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
            Array.Copy(secretKeyBytes, keyBytes, Math.Min(keyBytes.Length, secretKeyBytes.Length));
            RijndaelManaged rm = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = 128,
                BlockSize = 128,
                Key = keyBytes,
                IV = keyBytes

            };
            byte[] resultArray = rm.CreateEncryptor().TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }
    }
}
