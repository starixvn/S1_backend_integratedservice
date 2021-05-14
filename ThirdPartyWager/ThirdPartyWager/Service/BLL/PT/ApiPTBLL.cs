using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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

namespace ThirdPartyWager.Service.BLL
{
    public class ApiPTBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;
        private readonly IConfiguration configuration;
        private readonly Log2File log2File;

        public ApiPTBLL(
            HttpHelper httpHelper,
            Common common,
            IConfiguration configuration,
            Log2File log2File
        )
        {
            this.httpHelper = httpHelper;
            this.common = common;
            this.configuration = configuration;
            this.log2File = log2File;
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

            // request
            log2File.Info($"Guid:\r\n{guid}\r\nUrl:\r\n{url}\r\nRequest:\r\n{JsonConvert.SerializeObject(param)}\r\nSiteId:{certificate["site"]}\r\nCurrency:{certificate["currency"]}", "PT");
            string r = await DoHttpClientAsync(url, param, entityKey, certificate);
            var result = JsonConvert.DeserializeObject<WagerPTModel>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, object param, string entityKey, Dictionary<string, string> certificate)
        {
            var dicP = common.FromJson<Dictionary<string, string>>(common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(sParam, Encoding.UTF8, "application/x-www-form-urlencoded");
            request.Headers.Add("X_ENTITY_KEY", entityKey);

            HttpClientHandler handler = new HttpClientHandler();
            if (configuration.GetValue<string>("ProxyHost") != null)
            {
                handler.Proxy = new WebProxy(configuration.GetValue<string>("ProxyHost"), configuration.GetValue<int>("ProxyPort"));
                handler.UseProxy = true;
            }
            handler.ClientCertificates.Add(new X509Certificate2(certificate["path"], certificate["password"], X509KeyStorageFlags.MachineKeySet));

            var client = new HttpClient(handler);
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                string fatal = "{\"error\":\"fatal error\",\"errorcode\":999999}";
                return fatal;
            }
        }
    }
}
