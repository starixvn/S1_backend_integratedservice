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

        public TpBetRecordDetailModel GetTpBetRecordDetail(GetBetDetail getBetDetail)
        {
            var p = new DynamicParameters(new
            {
                TpId = getBetDetail.TpGameTypeOption.TpId,
                GameType = getBetDetail.TpGameTypeOption.Type,
                TransId = getBetDetail.TransId,
                SettledTime = getBetDetail.SettledTime
            });

            return _reportDbClient.QueryFirst<TpBetRecordDetailModel>("Service_TpBetRecordDetail_Get_V1", p);
        }
    }
}
