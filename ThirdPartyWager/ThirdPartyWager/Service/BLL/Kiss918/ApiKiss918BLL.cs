using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiKiss918BLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;

        public ApiKiss918BLL(HttpHelper httpHelper, Common common)
        {
            this.httpHelper = httpHelper;
            this.common = common;
        }

        public async Task<WagerKiss918Model> GetBetListByTime(string wagerUrl, string authcode, string secretkey, string userName, string player, string startTime, string endTime, int page, int perPage)
        {
            // param
            long current = common.GetTimestemp1();
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
                sign = common.ToMD5(sign.ToLower()).ToUpper()
            };

            // request
            string r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<WagerKiss918Model>(r);
            return result;

        }

        public async Task<WagerKiss918ReportPlayer> GetReportPlayerListByTime(string wagerUrl, string authcode, string secretkey, string userName, string startTime, string endTime)
        {
            // param
            long current = common.GetTimestemp1();
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
                sign = common.ToMD5(sign.ToLower()).ToUpper()
            };

            // request
            string r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<WagerKiss918ReportPlayer>(r);
            return result;

        }

        private async Task<string> DoHttpClientAsync(string url, object param)
        {
            var dicP = common.FromJson<Dictionary<string, string>>(common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(sParam, Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await httpHelper.GetClient().SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                return null;
            }
        }
    }
}
