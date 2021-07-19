using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
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
        private readonly HttpHelper httpHelper;
        private readonly Common common;
        private readonly IConfiguration configuration;
        private readonly Log2File log2File;
        private readonly ILogger<ApiPTBLL> logger;
        private readonly string logName = Tp.PT.ToString();

        public ApiPTBLL(
            HttpHelper httpHelper,
            Common common,
            IConfiguration configuration,
            Log2File log2File,
            ILogger<ApiPTBLL> logger
        )
        {
            this.httpHelper = httpHelper;
            this.common = common;
            this.configuration = configuration;
            this.log2File = log2File;
            this.logger = logger;
        }

        public async Task<WagerPTModel> GetBetListByTime(string apiUrl, string entityKey, Dictionary<string, string> certificate, string startTime, string endTime, int page, int perPage, string guid)
        {
            // param
            string url = $"{apiUrl}/game/flow";
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
            var cert = new { currency = certificate["currency"], site = certificate["site"] };

            // log
            log2File.AddRequestLog(logName, guid, url, param, cert);

            // request
            string r = await DoHttpClientAsync(url, param, entityKey, certificate, guid);
            var result = JsonConvert.DeserializeObject<WagerPTModel>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, object param, string entityKey, Dictionary<string, string> certificate, string guid = null)
        {
            var dicP = common.FromJson<Dictionary<string, string>>(common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(sParam, Encoding.UTF8, "application/x-www-form-urlencoded");
            request.Headers.Add("X_ENTITY_KEY", entityKey);

            using HttpClientHandler handler = new HttpClientHandler();
            if (configuration.GetValue<string>("ProxyHost") != null)
            {
                handler.Proxy = new WebProxy(configuration.GetValue<string>("ProxyHost"),
                    configuration.GetValue<int>("ProxyPort"));
                handler.UseProxy = true;
            }

            handler.ClientCertificates.Add(new X509Certificate2(certificate["path"], certificate["password"],
                X509KeyStorageFlags.MachineKeySet));

            using var client = new HttpClient(handler);
            using var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                var result = "{\"error\":\"fatal error\",\"errorcode\":999999}";
                LogContext.PushProperty("Tp", logName);
                logger.LogError($"URL {url}");
                logger.LogError($"REQUEST: {sParam}");
                logger.LogError($"HEADER: {request.Headers}");
                logger.LogError($"CERT: {certificate}");
                logger.LogError($"RESPONSE: {response}");
                log2File.AddResponseLog(logName, guid, url, sParam, response, "Error", request.Headers, certificate);
                return result;
            }
        }
    }
}
