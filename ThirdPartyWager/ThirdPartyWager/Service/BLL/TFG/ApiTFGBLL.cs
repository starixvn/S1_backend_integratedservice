using Newtonsoft.Json;
using System.Text;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL.TFG
{
    public class ApiTFGBLL
    {
        private readonly HttpHelper _httpHelper;
        public ApiTFGBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
        }

        public async Task<Root> GetBetTransactionByTime(string apiUrl, string privateToken, string startTime, string endTime)
        {
            string url = $"https://{apiUrl}/api/v2/bet-transaction/?from_settlement_datetime={startTime}&to_settlement_datetime={endTime}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", privateToken);
            var response = await _httpHelper.GetClient().SendAsync(request);
            string r = null;
            using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
            {
                r = sr.ReadToEnd();
            }

            var result = JsonConvert.DeserializeObject<Root>(r);

            return result;
        }

        public async Task<Root> GetBetTransactionById(string apiUrl, string privateToken, string id)
        {
            string url = $"https://{apiUrl}/api/v2/bet-transaction/?id={id}&settlement_status=settled";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", privateToken);
            var response = await _httpHelper.GetClient().SendAsync(request);
            string r = null;
            using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
            {
                r = sr.ReadToEnd();
            }

            var result = JsonConvert.DeserializeObject<Root>(r);

            return result;
        }
    }
}
