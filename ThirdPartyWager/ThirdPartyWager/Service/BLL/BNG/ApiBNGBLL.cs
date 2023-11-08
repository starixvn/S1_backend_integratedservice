using Newtonsoft.Json;
using System.Text;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiBNGBLL
    {
        private readonly Common _common;
        private readonly HttpHelper _httpHelper;
        public ApiBNGBLL(IServiceProvider provider)
        {
            _common = provider.GetRequiredService<Common>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
        }

        public async Task<TransactionData> GetRecordListAsync(Dictionary<string, string> setting, string startTime, string endTime)
        {
            var url = $"{setting["ApiUrl"]}/{setting["ProjectName"]}/api/v1/transaction/list/";

            var sParam = new
            {
                api_token = setting["ApiToken"],
                start_date = startTime,
                end_date = endTime
            };
            var param = _common.ToJson(sParam);
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/json");
            var response = await _httpHelper.GetClient().SendAsync(request);
            var r = response.Content.ReadAsStringAsync().Result;

            var result = JsonConvert.DeserializeObject<TransactionData>(r);
            return result;
        }

    }
}
