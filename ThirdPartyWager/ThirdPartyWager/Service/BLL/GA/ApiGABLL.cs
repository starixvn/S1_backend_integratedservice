using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiGABLL
    {
        private readonly Common _common;
        private readonly HttpHelper _httpHelper;
        public ApiGABLL(IServiceProvider provider)
        {
            _common = provider.GetRequiredService<Common>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
        }

        public async Task<List<GATickets>> GetRecordListAsync(GAConfig setting, string startTime, string endTime)
        {
            var url = $"{setting.ApiUrl}/api/v1/tickets?from_modified_date_time={startTime}&to_modified_date_time={endTime}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", setting.PrivateKey);
            var response = await _httpHelper.GetClient().SendAsync(request);
            var r = response.Content.ReadAsStringAsync().Result;

            var result = JsonConvert.DeserializeObject<List<GATickets>>(r);
            return result;
        }
    }
}
