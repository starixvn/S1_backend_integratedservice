using Newtonsoft.Json;
using System.Text;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using static System.Net.Mime.MediaTypeNames;

namespace ThirdPartyWager.Service.BLL.Nextspin
{
    public interface INextspinApi
    {
        Task<NextspinBetHistory> GetBetHistory(string beginDate, string endDate, int page, NextspinSetting setting);
    }

    public class NextspinApi : INextspinApi
    {
        private readonly HttpHelper _httpHelper;
        private readonly ILogger<NextspinApi> _logger;
        public NextspinApi(HttpHelper httpHelper, ILogger<NextspinApi> logger)
        {
            _httpHelper = httpHelper;
            _logger = logger;
        }

        public async Task<NextspinBetHistory> GetBetHistory(string beginDate, string endDate, int page, NextspinSetting setting)
        {
            var merchantCode = setting.MerchantCode;
            var apiUrl = setting.ApiUrl;
            var request = new
            {
                merchantCode,
                beginDate,
                endDate,
                page
            };

            try
            {
                var stringContent = new StringContent(
                    JsonConvert.SerializeObject(request),
                    Encoding.UTF8,
                    Application.Json);

                var client = _httpHelper.GetClient();
                client.DefaultRequestHeaders.Add("API", "getBetHistory");
                client.DefaultRequestHeaders.Add("DataType", "JSON");

                var httpResponseMessage = await client.PostAsync(apiUrl, stringContent);

                var content = await httpResponseMessage.Content.ReadAsStringAsync();
                if (!httpResponseMessage.IsSuccessStatusCode)
                {
                    _logger.LogError($"failed, request:{JsonConvert.SerializeObject(request)} response:{content}");
                    return null;
                }
                var nextspinBetHistory = JsonConvert.DeserializeObject<NextspinBetHistory>(content);
                if (nextspinBetHistory.Code != 0) 
                {
                    _logger.LogError($"failed, request:{JsonConvert.SerializeObject(request)} response:{nextspinBetHistory.Msg}");
                    return null;
                }
                return nextspinBetHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{apiUrl} get bet list failed, request:{JsonConvert.SerializeObject(request)} exception:{ex.Message}");
                return null;
            }
        }
    }
}
