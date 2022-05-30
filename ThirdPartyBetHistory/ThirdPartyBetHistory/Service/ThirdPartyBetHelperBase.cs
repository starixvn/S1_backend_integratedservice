using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Serilog.Core;
using ThirdPartyBetHistory.Service.DAL;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;
using ThirdPartyBetHistory.Service.Worker;

namespace ThirdPartyBetHistory.Service
{
    public interface IThirdPartyBetHelper
    {
        Task AddBetRecord(Action<IEnumerable<BetRecordInfo>> action);

        Task GetVS3rdBetDetail(long siteTicketKey, Action<IEnumerable<BetRecordInfo>> action);
        Task UpdateBetRecordByTransId(Action<IEnumerable<BetRecordInfo>> action);

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
        public abstract Task AddBetRecord(Action<IEnumerable<BetRecordInfo>> action);
        public abstract Task GetVS3rdBetDetail(long siteTicketKey, Action<IEnumerable<BetRecordInfo>> action);
        public abstract Task UpdateBetRecordByTransId(Action<IEnumerable<BetRecordInfo>> action);
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
