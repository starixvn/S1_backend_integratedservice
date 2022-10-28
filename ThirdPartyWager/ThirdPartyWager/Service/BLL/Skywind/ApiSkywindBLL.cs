using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiSkywindBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiSkywindBLL> _logger;
        private readonly string _tpName = Tp.Skywind.ToString();
        private readonly MySqlClient _wagerDbClient;
        private readonly MySqlClient _mainDbClient;

        public ApiSkywindBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiSkywindBLL>>();
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public class JwtToken
        {
            public string key { get; set; }
            public string username { get; set; }
            public string accessToken { get; set; }
        }

        public async Task<List<WagerSkywindModel>> GetBetListByTime(string apiUrl, string secretKey, string username, string password, string startTime, string endTime)
        {
            var jwtToken = await GetJwtTokenAsync(apiUrl, username, password, secretKey);
            string url = $"{apiUrl}/history/game?ts__gte={startTime}&ts__lte={endTime}&finished='true'&limit=100";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-ACCESS-TOKEN", jwtToken.accessToken);
            var response = await _httpHelper.GetClient().SendAsync(request);
            string r = null;
            using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
            {
                r = sr.ReadToEnd();
            }

            var result = JsonConvert.DeserializeObject<List<WagerSkywindModel>>(r);

            return result;
        }

        public async Task<WagerSkywindModel> GetBetByTransId(string apiUrl, string secretKey, string username, string password, string transId)
        {
            var jwtToken = await GetJwtTokenAsync(apiUrl, username, password, secretKey);
            string url = $"{apiUrl}/history/game?roundId={transId}&finished='true'&limit=100";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-ACCESS-TOKEN", jwtToken.accessToken);
            var response = await _httpHelper.GetClient().SendAsync(request);
            string r = null;
            using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
            {
                r = sr.ReadToEnd();
            }

            var result = JsonConvert.DeserializeObject<List<WagerSkywindModel>>(r).FirstOrDefault();

            return result;
        }

        public async Task<List<WagerSkywindThridPartyModel>> GetThirdPartyBetListByTime(string apiUrl, string secretKey, string username, string password, string startTime, string endTime)
        {
            var jwtToken = await GetJwtTokenAsync(apiUrl, username, password, secretKey);
            string url = $"{apiUrl}/history/external?insertedAt__gt={startTime}&insertedAt__lt={endTime}&sortBy=finished&limit=100";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-ACCESS-TOKEN", jwtToken.accessToken);
            var response = await _httpHelper.GetClient().SendAsync(request);
            string r = null;
            using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
            {
                r = sr.ReadToEnd();
            }

            var result = JsonConvert.DeserializeObject<List<WagerSkywindThridPartyModel>>(r);

            return result;
        }

        public async Task<WagerSkywindThridPartyModel> GetThirdPartyBetByTransId(string apiUrl, string secretKey, string username, string password, string transId)
        {
            var jwtToken = await GetJwtTokenAsync(apiUrl, username, password, secretKey);
            string url = $"{apiUrl}/history/external?roundId={transId}&sortBy=finished&limit=100";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-ACCESS-TOKEN", jwtToken.accessToken);
            var response = await _httpHelper.GetClient().SendAsync(request);
            string r = null;
            using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("utf-8")))
            {
                r = sr.ReadToEnd();
            }

            var result = JsonConvert.DeserializeObject<List<WagerSkywindThridPartyModel>>(r).FirstOrDefault();

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
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/json");
            var response = await _httpHelper.GetClient().SendAsync(request);
            var r = response.Content.ReadAsStringAsync().Result;

            var result = JsonConvert.DeserializeObject<JwtToken>(r);
            return result;
        }
    }
}
