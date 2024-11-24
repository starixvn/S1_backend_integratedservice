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
        private readonly MySqlClient _mainDbClient;

        public WagerEdayService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
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

        public int UpdateEdayBillId(WagerEdayBillData wager,int ticketStatus)
        {
            var p = new DynamicParameters();
            
            p.Add("betTotal", wager.TotalBet);
            p.Add("winTotal", wager.TotalWin);
            p.Add("UTCSettleTime", wager.SettlementTime.Replace("+00:00", ""));
            p.Add("TicketStatus", ticketStatus);
            //因為是拉結算單，直接給EndGame跟Payout
            p.Add("TicketType", 4);
            p.Add("SettleFlag", 2);
            p.Add("billId", wager.BillId.TrimStart('0'));
            p.Add("player", wager.Player);
            
            var result = _mainDbClient.Execute("Service_BettransLT_Eday_BillId_Set", p);
            return !result ? 0 : 1;
        }

        public IEnumerable<GetEdayBillId> GetEdayBillIdByTime()
        {
            var p = new DynamicParameters();
            
            var result = _mainDbClient.Query<GetEdayBillId>("Service_BettransLT_Eday_BillId_Get", p);
            return result;
        }
        
        
        #region Model

        public class GetEdayBillId
        {
            public string billId { get; set; }
        }
        
        #endregion
    }
}