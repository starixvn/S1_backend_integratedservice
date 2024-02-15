using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiGABLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly ILogger<ApiGABLL> _logger;
        public ApiGABLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _logger = provider.GetRequiredService<ILogger<ApiGABLL>>();
        }

        public async Task<List<GATickets>> GetRecordListAsync(GAConfig setting, string startTime, string endTime)
        {
            try
            {
                var startDate = DateTime.Parse(startTime).ToString("yyyy-MM-ddTHH:mm:ssZ");
                var endDate = DateTime.Parse(endTime).ToString("yyyy-MM-ddTHH:mm:ssZ");
                var url = $"{setting.ApiUrl}/api/v1/tickets/?from_modified_date_time={startDate}&to_modified_date_time={endDate}";

                _logger.LogInformation($"url:{url}");

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", setting.PrivateKey);
                var response = await _httpHelper.GetClient().SendAsync(request);
                var r = response.Content.ReadAsStringAsync().Result;

                var result = JsonConvert.DeserializeObject<List<GATickets>>(r);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new List<GATickets>();
            }
        }
    }
}
