using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private readonly ILogger<ApiKiss918BLL> _logger;

        public ApiKiss918BLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _logger = provider.GetRequiredService<ILogger<ApiKiss918BLL>>();
        }

        public async Task<string> GetTimepoint(string apiToken, string wagerUrl, string dateTime)
        {
            // param
            var url = $"{wagerUrl}/api/gettimepoint";
            var param = new { dateTime = dateTime };

            var header = new Dictionary<string, string>() { { "Token", $"{apiToken}" } };
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // request
            var response = await HttpGet(url, "get timepoint", sParam, header);
            if (response == null) return null;
            var dicResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            return dicResponse["Timepoint"];
        }

        public async Task<(bool, List<WagerKiss918Model>)> GetPlaySession(string accessToken, string wagerUrl, string timepoint)
        {
            // param
            var url = $"{wagerUrl}/api/playsessions";
            var param = new { timepoint = timepoint };

            var header = new Dictionary<string, string>() { { "Token", $"{accessToken}" } };
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // request: 會拿到一串文本，要對文本拆解&加工
            var response = await HttpGet(url, "query history bets", sParam, header, "text/plain");
            if (response == null) return (false, null);
            var convertDatas = ConvertToWagerData(response);
            return (true, convertDatas);
        }

        private List<WagerKiss918Model> ConvertToWagerData(string plain)
        {
            List<WagerKiss918Model> result = new();
            string[] strings = plain.Split(Environment.NewLine);
            foreach (var data in strings.Skip(2))
            {
                if (string.IsNullOrWhiteSpace(data))
                    continue;
                var details = data.Split(",");

                //if (details.Count() == 17)
                //{
                //    result.Add(new WagerKiss918Model()
                //    {
                //        playerID = Convert.ToInt32(details[0]),
                //        extplayerID = Convert.ToInt32(details[1]),
                //        gameID = details[2].Trim('"'),
                //        methodType = details[3].Trim('"'),
                //        playSessionID = details[4].Trim('"'),
                //        referenceID = details[5].Trim('"'),
                //        status = details[6].Trim('"'),
                //        created = DateTime.ParseExact(details[7].Trim('"'), "yyyy-MM-dd HH:mm:ss.fff", null),
                //        updated = DateTime.ParseExact(details[8].Trim('"'), "yyyy-MM-dd HH:mm:ss.fff", null),
                //        betAmount = Convert.ToDecimal(details[9]),
                //        winAmount = Convert.ToDecimal(details[10]),
                //        jackpotModule = Convert.ToInt32(details[11]),
                //        jackpotContributionAmt = Convert.ToDecimal(details[12]),
                //        currency = details[13].Trim('"'),
                //        resultUrl = details[14].Trim('"'),
                //        roundDetails = details[15].Trim('"'),
                //        platform = details[16].Trim('"')
                //    });
                //}
                //else
                //{
                result.Add(new WagerKiss918Model()
                {
                    playerID = Convert.ToInt32(details[0]),
                    extplayerID = Convert.ToInt32(details[1]),
                    gameID = details[2].Trim('"'),
                    methodType = details[3].Trim('"'),
                    playSessionID = details[4].Trim('"'),
                    referenceID = details[5].Trim('"'),
                    status = details[6].Trim('"'),
                    created = DateTime.ParseExact(details[7].Trim('"'), "yyyy-MM-dd HH:mm:ss.fff", null),
                    updated = DateTime.ParseExact(details[8].Trim('"'), "yyyy-MM-dd HH:mm:ss.fff", null),
                    betAmount = Convert.ToDecimal(details[9]),
                    winAmount = Convert.ToDecimal(details[10]),
                    jackpotModule = Convert.ToInt32(details[11]),
                    jackpotContributionAmt = Convert.ToDecimal(details[12]),
                    currency = details[13].Trim('"'),
                    resultUrl = details[14].Trim('"'),
                    roundDetails = ConvertRoundDetails(15, details.Length - 1, details),
                    //platform = details[details.Length - 1].Trim('"')
                    platform = "MOBILE"
                });
                //}
            }
            return result;
        }

        private string ConvertRoundDetails(int fromIndex, int lastIndex, string[] details)
        {
            var result = string.Empty;
            for (var i = fromIndex; i < lastIndex; i++)
            {
                result = result + details[i].Trim('"') + ",";
            }
            return result.Trim(',');
        }

        protected async Task<string> HttpGet(string url,
            string recordEvent,
            string parameters,
            Dictionary<string, string> headers = null, string contentType = null)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{url}?{parameters}");

                if (!string.IsNullOrWhiteSpace(contentType))
                    request.Content = new StringContent(parameters, Encoding.UTF8, contentType);
                if (headers != null)
                {
                    foreach (var item in headers)
                        request.Headers.Add(item.Key, item.Value);
                }

                var client = _httpHelper.GetClient();
                var response = await client.SendAsync(request);

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"{url}?{parameters} {recordEvent} error:{content}");
                    return null;
                }

                return content;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{url}?{parameters} {recordEvent} failed:{e.Message}");
                return null;
            }
        }

    }
}
