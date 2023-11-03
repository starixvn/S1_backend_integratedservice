using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using System.Text;
using System.Transactions;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using static ThirdPartyWager.Service.BLL.ApiSkywindBLL;

namespace ThirdPartyWager.Service.BLL.BNG
{
    public class ApiBNGBLL
    {
        private readonly ILogger<ApiBNGBLL> _logger;
        private readonly Common _common;
        private readonly HttpHelper _httpHelper;
        public ApiBNGBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ApiBNGBLL>>();
            _common = provider.GetRequiredService<Common>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
        }

        public async Task<TransactionData> GetRecordListAsync(Dictionary<string, string> setting, string startTime, string endTime)
        {
            var url = $"{setting["ApiUrl"]}/{setting["ProjectName"]}/api/v1/transaction/aggregate/";

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
