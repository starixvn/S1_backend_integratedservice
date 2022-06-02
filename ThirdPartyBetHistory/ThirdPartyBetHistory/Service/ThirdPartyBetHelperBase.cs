using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using ThirdPartyBetHistory.Service.DAL;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service
{
    public interface IThirdPartyBetHelper
    {
        Task<long> AddBetRecord(long siteTicketKey);
        Task<long> GetVS3rdBetDetail(long siteTicketKey);
        Task UpdateBetRecordByTransId(WorkerType workerType, DateTime today, int hour);
        SyncParams InitialSyncParams();
        bool IsDebug { set; get; }
        bool IsRunning { set; get; }
        SyncParams syncParams { set; get; }
        ThirdPartyModel thirdPartyModel { set; get; }
        SiteId SiteId { set; get; }
    }

    public abstract class ThirdPartyBetHelperBase : IThirdPartyBetHelper
    {
        public SiteId SiteId { get; set; }
        public bool IsDebug { get; set; }
        public bool IsRunning { get; set; }
        public SyncParams syncParams { set; get; }
        public ThirdPartyModel thirdPartyModel { set; get; }
        public abstract Task<long> AddBetRecord(long siteTicketKey);
        public abstract Task<long> GetVS3rdBetDetail(long siteTicketKey);
        public abstract Task UpdateBetRecordByTransId(WorkerType workerType, DateTime today, int hour);
        public abstract SyncParams InitialSyncParams();

        protected ILogger<ThirdPartyBetHelperBase> _logger;
        protected ThirdPartyService _thirdPartyService;

        protected async Task<string> HttpPostAsync(string url, string parameters, string contentType)
        {
            try
            {
                LogContext.PushProperty("TpInfo", parameters);
                LogContext.PushProperty("Tp.Request.URL", url);
                var data = new StringContent(parameters, Encoding.UTF8, contentType);
                var handler = new HttpClientHandler();
                using var client = new HttpClient(handler);

                var response = await client.PostAsync(url, data);
                var result = response.Content.ReadAsStringAsync().Result;
                _logger.LogInformation(result);

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
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
