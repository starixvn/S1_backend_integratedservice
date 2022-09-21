using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
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

        public async Task<string> GetTimepoint(string accessToken, string wagerUrl, string dateTime)
        {
            // param
            var url = $"{wagerUrl}/api/gettimepoint";
            var param = new { dateTime = dateTime };

            var header = new Dictionary<string, string>() { { "token", $"{accessToken}" } };
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // request
            var response = await HttpGet(url, sParam, header);
            var dicResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            return dicResponse["Timepoint"];
        }

        public async Task<(bool, List<WagerKiss918Model>)> GetPlaySession(string accessToken, string wagerUrl, string timepoint)
        {
            // param
            var url = $"{wagerUrl}/api/playsessions";
            var param = new { timepoint = timepoint };

            var header = new Dictionary<string, string>() { { "token", $"{accessToken}" } };
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            // request: 會拿到一串文本，要對文本拆解&加工
            var response = await HttpGet(url, sParam, "text/plain", header);
            var convertDatas = ConvertToWagerData( response);
            return (true, convertDatas);
        }

        private List<WagerKiss918Model> ConvertToWagerData(string plain)
        {
            int start = 0;
            List<WagerKiss918Model> result = new();
            string[] strings = plain.Split(Environment.NewLine);
            foreach (var data in strings.Skip(2))
            {
                if (string.IsNullOrWhiteSpace(data))
                    continue;
                var details = data.Split(",");
                if (start > 1)
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
                        roundDetails = details[15].Trim('"'),
                        platform = details[16].Trim('"')
                    });
                start++;
            }
            return result;
        }

        protected async Task<string> HttpGet(string url, string parameters, Dictionary<string, string> headers = null)
        {
            using var handler = new HttpClientHandler();
            using var client = new HttpClient(handler);
            try
            {
                if (headers != null)
                {
                    foreach (var item in headers)
                    {
                        client.DefaultRequestHeaders.Add(item.Key, item.Value);
                    }
                }
                var stream = client.GetStreamAsync($"{url}?{parameters}").Result;
                using StreamReader myStreamReader = new StreamReader(stream, Encoding.GetEncoding("UTF-8"));
                var response = myStreamReader.ReadToEnd();
                return response;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        protected async Task<string> HttpGet(string url, string parameters, string contentType, Dictionary<string, string> headers = null)
        {
            //InsertLog?.Invoke(url, parameters, "1");
            WebRequest request = WebRequest.Create($"{url}?{parameters}");

            request.Method = "GET";
            request.ContentType = contentType;
            if (headers != null)
            {
                foreach (var item in headers)
                {
                    request.Headers.Add(item.Key, item.Value);
                }
            }

            StringBuilder sb = new StringBuilder();
            using (WebResponse response = request.GetResponse())
            using (Stream dataStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(dataStream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    sb.AppendLine(line);
                }
            }
            //InsertLog?.Invoke(url, parameters, sb.ToString());
            return sb.ToString();
        }

    }
}
