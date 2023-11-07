using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiPegasusBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly string _tpName = Tp.Pegasus.ToString();

        public ApiPegasusBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        public async Task<WagerData_Pegasus<PegasusData>> GetBetListByTime(string apiUrl, string secretKey, string merchantId, string startTime, string endTime, int page)
        {
            // param
            var request = new
            {
                MerchantSecretKey = secretKey,
                MerchantID = merchantId,
                StartTime = startTime,
                EndTime = endTime,
                PageIndex = page ,
                PageSize = 500 ,
                version = "v1",
            };
            
            var dicRequest = _common.FromJson<Dictionary<string, string>>(_common.ToJson(request));
            var parameters = string.Join("&", dicRequest.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var url = apiUrl + "/game-records";
         
            // request
            var r = await DoHttpClientAsync(url, parameters);
            var result = JsonConvert.DeserializeObject<WagerData_Pegasus<PegasusData>>(r);
            return result;
        }
       
        public async Task<WagerData_Pegasus<WagerSummary_Pegasus>> GetBetSummaryByTime(string apiUrl, string secretKey, string merchantId, string startTime, string endTime)
        {
            // param
            var request = new
            {
                MerchantSecretKey = secretKey,
                MerchantID = merchantId,
                StartTime = startTime,
                EndTime = endTime,
                version = "v1",
            };
            
            var dicRequest = _common.FromJson<Dictionary<string, string>>(_common.ToJson(request));
            var parameters = string.Join("&", dicRequest.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var url = apiUrl + "/game-records/report";
         
            // request
            var r = await DoHttpClientAsync(url, parameters);
            var result = JsonConvert.DeserializeObject<WagerData_Pegasus<WagerSummary_Pegasus>>(r);
            return result;
        }
        
        private async Task<string> DoHttpClientAsync(string url, string param)
        {
            _log2File.InsertRequestLog(_tpName, url, param, "1");
            url = $"{url}?{param}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, param, result);
                return null;
            }
            
            return result;
        }
    }
}
