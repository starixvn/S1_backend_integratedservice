using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL.KM
{
    public class ApiKMBLL
    {
        public Dictionary<string, string> KmConfig { get; set; }
        private readonly HttpHelper _httpHelper;
        private readonly ILogger<ApiKMBLL> _logger;
        
        public ApiKMBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ApiKMBLL>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
        }
        public async Task<List<WagerKMModel>> QueryBetHistoryAsync(string startTime, string endTime)
        {
            try
            {
                var apiUrl = GetInputValue(KmConfig, "ApiUrl");
                var clientId = GetInputValue(KmConfig, "X-QM-ClientId");
                var clientSecret = GetInputValue(KmConfig, "X-QM-ClientSecret");

                startTime = $"{DateTime.Parse(startTime).ToString("s")}+08:00";
                endTime = $"{DateTime.Parse(endTime).ToString("s")}+08:00";

                var encodingQueryStr = $"startdate={HttpUtility.UrlEncode(startTime)}&" +
                                             $"enddate={HttpUtility.UrlEncode(endTime)}&issettled=true";
                var historyBet = $"{apiUrl}/api/history/bets?{encodingQueryStr}";
            
                var request = new HttpRequestMessage(HttpMethod.Get, historyBet);
                request.Headers.Add("X-QM-ClientId", clientId);
                request.Headers.Add("X-QM-ClientSecret", clientSecret);
                request.Headers.Add("X-QM-Accept", "json");
                request.Headers.Add("Accept-Encoding", "gzip");

                var client = _httpHelper.GetClientWithHandler(new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip
                });

                var response = await client.SendAsync(request);
            
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"query history bets error :{content}");
                    return null;
                }
                var kmWagers = JsonConvert.DeserializeObject<List<WagerKMModel>>(content);
                return kmWagers;
            }
            catch (Exception e)
            {
                _logger.LogError(e ,$"query history bets error :{e.Message}");
                return null;
            }
            
        }
        private string GetInputValue(Dictionary<string, string> inputs, string key)
        {
            if(inputs.ContainsKey(key) && !string.IsNullOrEmpty(inputs[key]))
            {
                return inputs[key];
            }
            throw new Exception();
        }
        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public static byte[] Decompress(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }
    }
}