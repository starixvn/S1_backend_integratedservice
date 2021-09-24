using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Service.DAL
{
    public class TpBetRecordService: ITpBetRecordProvider
    {
        private readonly MySqlClient _reportDbClient;

        public TpBetRecordService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public TpBetRecordDetailModel GetTpBetRecordDetail(GetBetDetailRequest request)
        {
            var p = new DynamicParameters(new
            {
                TpId = request.TpId,
                GameType = request.GameType,
                TransId = request.TransId,
                SettledTime = request.SettledTime
            });

            return _reportDbClient.QueryFirst<TpBetRecordDetailModel>("Service_TpBetRecordDetail_Get_V1", p);
        }
    }
}
