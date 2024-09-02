using System.Text;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiSkywindBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly string _tpName = Tp.Skywind.ToString();
        private readonly MySqlClient _wagerDbClient;
        private readonly MySqlClient _mainDbClient;

        public ApiSkywindBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public class JwtToken
        {
            public string key { get; set; }
            public string username { get; set; }
            public string accessToken { get; set; }
        }

        public async Task<List<WagerSkywindModel>> GetBetListByTime(SkywindConfig setting, string startTime, string endTime)
        {
            var jwtToken = await GetJwtTokenAsync(setting.ApiUrl, setting.Username, setting.Password, setting.SecretKey);
            string url = $"{setting.ApiUrl}/history/game?ts__gte={startTime}&ts__lte={endTime}&finished='true'&limit=100";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-ACCESS-TOKEN", jwtToken.accessToken);

            // request log
            _log2File.InsertRequestLog(_tpName, url, $"{JsonConvert.SerializeObject(request.Headers)}", "1");

            var response = await _httpHelper.GetClient().SendAsync(request);
            string r = null;
            using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
            {
                r = sr.ReadToEnd();
            }

            // response log
            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, "", r);
            }

            var result = JsonConvert.DeserializeObject<List<WagerSkywindModel>>(r);

            return result;
        }

        public async Task<WagerSkywindModel> GetBetByTransId(SkywindConfig setting, string transId)
        {
            var jwtToken = await GetJwtTokenAsync(setting.ApiUrl, setting.Username, setting.Password, setting.SecretKey);
            string url = $"{setting.ApiUrl}/history/game?roundId={transId}&finished='true'&limit=100";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-ACCESS-TOKEN", jwtToken.accessToken);

            // request log
            _log2File.InsertRequestLog(_tpName, url, $"{JsonConvert.SerializeObject(request.Headers)}", "1");

            var response = await _httpHelper.GetClient().SendAsync(request);
            string r = null;
            using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
            {
                r = sr.ReadToEnd();
            }

            // response log
            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, "", r);
            }

            var result = JsonConvert.DeserializeObject<List<WagerSkywindModel>>(r).FirstOrDefault();

            return result;
        }

        public async Task<List<WagerSkywindThridPartyModel>> GetThirdPartyBetListByTime(SkywindConfig setting, string startTime, string endTime)
        {
            var jwtToken = await GetJwtTokenAsync(setting.ApiUrl, setting.Username, setting.Password, setting.SecretKey);
            string url = $"{setting.ApiUrl}/history/external?insertedAt__gt={startTime}&insertedAt__lt={endTime}&sortBy=finished&limit=100";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-ACCESS-TOKEN", jwtToken.accessToken);

            // request log
            _log2File.InsertRequestLog(_tpName, url, $"{JsonConvert.SerializeObject(request.Headers)}", "1");

            var response = await _httpHelper.GetClient().SendAsync(request);
            string r = null;
            using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
            {
                r = sr.ReadToEnd();
            }

            // response log
            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, "", r);
            }

            var result = JsonConvert.DeserializeObject<List<WagerSkywindThridPartyModel>>(r);

            return result;
        }

        public async Task<List<WagerSkywindThridPartyModel>> GetThirdPartyBetByTransId(SkywindConfig setting, string transId)
        {
            var jwtToken = await GetJwtTokenAsync(setting.ApiUrl, setting.Username, setting.Password, setting.SecretKey);
            string url = $"{setting.ApiUrl}/history/external?roundId={transId}&sortBy=finished&limit=100";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-ACCESS-TOKEN", jwtToken.accessToken);

            // request log
            _log2File.InsertRequestLog(_tpName, url, $"{JsonConvert.SerializeObject(request.Headers)}", "1");

            var response = await _httpHelper.GetClient().SendAsync(request);
            string r = null;
            using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
            {
                r = sr.ReadToEnd();
            }

            // response log
            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, "", r);
            }

            var result = JsonConvert.DeserializeObject<List<WagerSkywindThridPartyModel>>(r);

            return result;
        }

        public int InsertWager(BetSkywind bet)
        {
            return _wagerDbClient.Execute("Service_TpWagerSkywind_Ins", bet);
        }

        public Task<IEnumerable<SkywindUnsettleCheck>> GetUnsettleCheckList()
        {
            return _mainDbClient.QueryAsync<SkywindUnsettleCheck>("Service_Skywind_Unsettle_Check", null);
        }

        public SkywindUnsettleCustInfo GetUnsettleSiteIdByTransId(string YYYYMM,string TransId)
        {
            return _mainDbClient.QueryFirst<SkywindUnsettleCustInfo>("Service_Skywind_Unsettle_SiteId_Get", new { YYYYMM = YYYYMM, TransId = TransId });
        }

        private async Task<JwtToken> GetJwtTokenAsync(string apiUrl, string username, string password, string secretKey)
        {
            string url = $"{apiUrl}/login";
            var sParam = new
            {
                secretKey = secretKey,
                username = username,
                password = password
            };
            var param = _common.ToJson(sParam);

            // request log
            _log2File.InsertRequestLog(_tpName, url, $"{param}", "1");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/json");
            var response = await _httpHelper.GetClient().SendAsync(request);
            var r = response.Content.ReadAsStringAsync().Result;

            // response log
            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, "", r);
            }

            var result = JsonConvert.DeserializeObject<JwtToken>(r);
            return result;
        }
    }
}
