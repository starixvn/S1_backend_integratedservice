using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service
{
    public interface IThirdPartyBetHelper
    {
        Task AddBetRecord(Action<IEnumerable<BetRecordInfo>> action);
        SyncParams InitialSyncParams();
        bool IsDebug { set; get; }
        bool IsRunning { set; get; }
        SyncParams syncParams { set; get; }
        ThirdPartyModel thirdPartyModel { set; get; }
    }

    public abstract class ThirdPartyBetHelperBase : IThirdPartyBetHelper
    {
        public static string proxyHost;
        public static int proxyPort;
        public bool IsDebug { get; set; }
        public bool IsRunning { get; set; }
        public SyncParams syncParams { set; get; }
        public ThirdPartyModel thirdPartyModel { set; get; }
        public abstract Task AddBetRecord(Action<IEnumerable<BetRecordInfo>> action);
        public abstract SyncParams InitialSyncParams();

        protected async Task<string> HttpPostAsync(string url, string parameters, string contentType)
        {
            try
            {
                var data = new StringContent(parameters, Encoding.UTF8, contentType);
                HttpClientHandler handler = null;
                if (!string.IsNullOrEmpty(proxyHost))
                {
                    handler = new HttpClientHandler()
                    {
                        Proxy = new WebProxy(proxyHost, proxyPort),
                        UseProxy = true,
                    };
                }
                else
                {
                    handler = new HttpClientHandler();
                }

                using var client = new HttpClient(handler);

                var response = await client.PostAsync(url, data);

                return response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception e)
            {
                throw new Exception($"url:{url}, parameters:{parameters}, contentType:{contentType}, ex:{e.Message}");
            }
           
        }
        protected T FromJson<T>(string jsonString)
        {
            if (!string.IsNullOrEmpty(jsonString))
            {
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
            else
            {
                return default(T);
            }
        }
        protected string ToJson(object t) =>
                    JsonConvert.SerializeObject(t, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

        protected long ConvertLocTime(DateTime time) =>
            new DateTimeOffset(time, new TimeSpan(syncParams.Offsets, 0, 0)).ToUnixTimeSeconds();
    }
}
