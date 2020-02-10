using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
            var data = new StringContent(parameters, Encoding.UTF8, contentType);

            using var client = new HttpClient();

            var response = await client.PostAsync(url, data);

            string result = response.Content.ReadAsStringAsync().Result;

            return result;
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
