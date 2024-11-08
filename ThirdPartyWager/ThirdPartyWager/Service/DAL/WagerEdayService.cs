using Dapper;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerEdayService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerEdayService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public int InsertEdayWager(WagerEdayBillData wager)
        {
            var edayResultItemConvertJson = JsonConvert.SerializeObject(wager.Items);
            
            var p = new DynamicParameters();
            p.Add("billId", wager.BillId);
            p.Add("player", wager.Player);
            p.Add("totalBet", wager.TotalBet);
            p.Add("totalWin", wager.TotalWin);
            p.Add("totalNet", wager.TotalNet);
            p.Add("createdTime", wager.CreatedTime.Replace("+00:00", ""));
            p.Add("endTime", wager.EndTime.Replace("+00:00", ""));
            p.Add("settlementTime", wager.SettlementTime.Replace("+00:00", ""));
            p.Add("roundCode", wager.RoundCode);
            p.Add("gameCode", wager.GameCode);
            p.Add("billStatus", wager.BillStatus);
            p.Add("itemCount", wager.ItemCount);
            p.Add("items", edayResultItemConvertJson);
            p.Add("wagerType", wager.WagerType);
            var result = _wagerDbClient.Execute("Service_TpWager_Eday_Ins", p);
            return !result ? 0 : 1;
        }
    }
}