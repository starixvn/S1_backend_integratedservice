using Newtonsoft.Json;
using System.Text;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiBNGBLL
    {
        private readonly Common _common;
        private readonly HttpHelper _httpHelper;
        private readonly Log2File _log2File;
        private readonly string _tpName = Tp.BNG.ToString();

        public ApiBNGBLL(IServiceProvider provider)
        {
            _common = provider.GetRequiredService<Common>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _log2File = provider.GetRequiredService<Log2File>();
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

            // log
            _log2File.InsertRequestLog(_tpName, url, param, "1");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/json");
            var response = await _httpHelper.GetClient().SendAsync(request);
            var r = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, param, r);
            }

            var result = JsonConvert.DeserializeObject<TransactionData>(r);
            return result;
        }

    }
}
