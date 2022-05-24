using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ThirdPartySeamlessWallet.Service.DAL.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL.ThirdParty
{
    public class SBOService : ISBOService
    {
        private readonly MySqlClient _sqlClient;

        public SBOService(IEnumerable<MySqlClient> sqlClients)
        {
            _sqlClient = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Main);
        }

        public async Task<SBOGetBetStatusData> GetBetStatus(string transferCode)
        {
            var p = new DynamicParameters(new { _TransferCode = transferCode });
            return await _sqlClient.QueryFirstAsync<SBOGetBetStatusData>("Service_BettransSports_SBO_Status_Get", p);
        }
    }
}