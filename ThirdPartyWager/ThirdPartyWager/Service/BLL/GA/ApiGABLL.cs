using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiGABLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Log2File _log2File;
        private readonly string _tpName = Tp.GA.ToString();
        public ApiGABLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        public async Task<List<GATickets>> GetRecordListAsync(GAConfig setting, string startTime, string endTime, int page)
        {
            var startDate = DateTime.Parse(startTime).ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endDate = DateTime.Parse(endTime).ToString("yyyy-MM-ddTHH:mm:ssZ");
            //GA一次最多只能拉32筆
            var param = $"from_modified_date_time={startDate}&to_modified_date_time={endDate}&page={page}&page_size=32";
            var tmpUrl = $"{setting.ApiUrl}/api/v1/tickets/";
            var url = $"{tmpUrl}?{param}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", setting.PrivateKey);

            _log2File.InsertRequestLog(_tpName, tmpUrl, $"{param}\r\n{JsonConvert.SerializeObject(request.Headers)}", "1");

            var response = await _httpHelper.GetClient().SendAsync(request);
            var r = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, param, r);
                return null;
            }

            var result = JsonConvert.DeserializeObject<List<GATickets>>(r);
            return result;
        }
    }
}
