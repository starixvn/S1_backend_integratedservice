using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Newtonsoft.Json;

namespace CommonService.Service.DAL
{
    public class IovationService
    {
        private readonly MySqlClient _logbClient;

        public IovationService(IServiceProvider provider)
        {
            _logbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Log);
        }

        public int IovationResultIns(IovationResult iovationResult)
        {
            var deviceId = iovationResult?.details?.device?.alias;
            var p = new
            {
                AccountCode = iovationResult.accountCode,
                DeviceId = string.IsNullOrEmpty(deviceId) ? "" : deviceId,
                Details = JsonConvert.SerializeObject(iovationResult.details),
                UUId = iovationResult.id,
                Reason = iovationResult.reason,
                Result = iovationResult.result,
                StatedIp = iovationResult.statedIp,
                TrackingNumber = iovationResult.trackingNumber
            };
            return _logbClient.Execute("Common_IovationLog_Ins_V2", p);
        }
    }
}
