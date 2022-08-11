using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiPTBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiPTBLL> _logger;
        private readonly string _tpName = Tp.PT.ToString();
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;

        public ApiPTBLL(
            IServiceProvider provider,
            IConfiguration configuration
        )
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiPTBLL>>();
            _configuration = configuration;
            _environment = provider.GetRequiredService<IHostEnvironment>();
        }

        public async Task<WagerPTModel> GetBetListByTime(string apiUrl, string entityKey, Dictionary<string, string> certificate, string startTime, string endTime, int page, int perPage, string guid)
        {
            // param
            var url = $"{apiUrl}/game/flow";
            var param = new
            {
                exitgame = true,
                showdetailedinfo = true,
                showbonustype = true,
                //excludezero = false,
                //progressiveonly = false,
                startdate = startTime,
                enddate = endTime,
                clientinfo = true,
                page = page,
                perPage = perPage,
                showgameshortname = true,
            };

            // request
            var r = await DoHttpClientAsync(url, param, entityKey, certificate);
            var result = JsonConvert.DeserializeObject<WagerPTModel>(r);
            return result;
        }

        public async Task<CurrencyStats_PT> GetBetSummaryByTime(dynamic setting, string currency, int siteId, string startTime, string endTime)
        {
            var apiUrl = setting["ApiUrl"];
            var entityKey = setting[$"XEntityKey_{currency}"];
            var pass = setting[$"Pass_{currency}"];
            var certificate = GetCertificate(pass, currency, siteId);

            // param
            var url = $"{apiUrl}/customreport/getdata";
            var param = new
            {
                startdate = startTime,
                enddate = DateTime.Parse(endTime).AddSeconds(1).ToString("yyyy-MM-dd HH:mm:ss"),
                reportname = "GameStatsSummary",
                showjackpot = 1
                //currency = "base",//native / base
                //gametype = "regular"//regular|live|both
            };

            // request
            var r = await DoHttpClientAsync(url, param, entityKey, certificate);
            var result = JsonConvert.DeserializeObject<CurrencyStats_PT>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, object param, string entityKey, Dictionary<string, string> certificate)
        {
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            _log2File.InsertRequestLog(_tpName, url, sParam, "1");

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(sParam, Encoding.UTF8, "application/x-www-form-urlencoded");
            request.Headers.Add("X_ENTITY_KEY", entityKey);

            using var handler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("ProxyHost")))
            {
                handler.Proxy = new WebProxy(_configuration.GetValue<string>("ProxyHost"),
                    _configuration.GetValue<int>("ProxyPort"));
                handler.UseProxy = true;
            }

            handler.ClientCertificates.Add(new X509Certificate2(certificate["path"], certificate["password"],
                X509KeyStorageFlags.MachineKeySet));

            using var client = new HttpClient(handler);
            using var response = await client.SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, sParam, result);
                result = "{\"error\":\"fatal error\",\"errorcode\":999999}";
            }

            return (response.StatusCode == HttpStatusCode.ServiceUnavailable) ? "{\"error\":\"SYSTEM_MATAINING\",\"errorcode\":999999}" : result;
        }

        private Dictionary<string, string> GetCertificate(string pass, string currency, int siteId)
        {
            return new Dictionary<string, string>
            {
                { "path", Path.Combine(_environment.ContentRootPath, "Storage", "PT", $"{currency}.p12") },
                { "password", pass },
                { "currency", currency },
                { "site", siteId.ToString() }
            };
        }
    }
}
