using System.Text;

using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartyBetHistory.Service.DAL;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service
{
    public abstract class ThirdPartyBetHelperBase : IThirdPartyBetHelper
    {
        public int SiteId { get; set; }

        public Tp Tp { get; set; }

        public string TpName
        {
            get
            {
                return Tp.ToString();
            }
        }

        public SabaConfig SabaConfig { set; get; }

        protected ILogger<ThirdPartyBetHelperBase> _logger;

        protected ThirdPartyService _thirdPartyService;

        public abstract Task UpdateBetRecordByTransId(WorkerType workerType, DateTime startTime, DateTime endTime, IEnumerable<SettlementType> settlementTypeList);                     

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
                return default;
            }
        }

        public abstract Task<long> AddTpBetRecordDetail(WorkerType workerType, long siteTicketKey);

        public abstract Task ResubmitProcess(VgamingUnSettle unSettle);
    }
}
