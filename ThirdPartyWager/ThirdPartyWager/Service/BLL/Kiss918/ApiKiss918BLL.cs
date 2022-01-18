using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiKiss918BLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiKiss918BLL> _logger;
        private readonly string _tpName = Tp.Kiss918.ToString();

        public ApiKiss918BLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiKiss918BLL>>();
        }


        public async Task<WagerKiss918Model> GetBetListByTime(string wagerUrl, string authcode, string secretkey, string userName, string player, string startTime, string endTime, int page, int perPage, string guid)
        {
            // param
            long current = _common.GetTimestemp1();
            string sign = authcode + player + current + secretkey;

            string url = $"{wagerUrl}/GameLog.ashx";
            var param = new
            {
                userName = player,
                sDate = startTime,
                eDate = endTime,
                authcode = authcode,
                pageIndex = page,
                pageSize = perPage,
                time = current,
                sign = _common.ToMD5(sign.ToLower()).ToUpper()
            };

            // request
            string r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<WagerKiss918Model>(r);
            return result;

        }

        public async Task<WagerKiss918ReportPlayer> GetReportPlayerListByTime(string wagerUrl, string authcode, string secretkey, string userName, string startTime, string endTime, string guid)
        {
            // param
            long current = _common.GetTimestemp1();
            string sign = authcode + userName + current + secretkey;

            string url = $"{wagerUrl}/AgentTotalReport.ashx";
            var param = new
            {
                userName = userName,
                sDate = startTime,
                eDate = endTime,
                authcode = authcode,
                Type = "ServerTotalReport",
                time = current,
                sign = _common.ToMD5(sign.ToLower()).ToUpper()
            };

            // request
            string r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<WagerKiss918ReportPlayer>(r);
            return result;

        }

        private async Task<string> DoHttpClientAsync(string url, object param)
        {
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            
            _log2File.InsertRequestLog(_tpName, url, sParam, "1");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(sParam, Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, sParam, result);
            }

            return result;
        }
    }
}
