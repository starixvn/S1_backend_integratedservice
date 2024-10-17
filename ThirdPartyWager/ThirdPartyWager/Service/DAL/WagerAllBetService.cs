using Dapper;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerAllBetService
    {
        private readonly MySqlClient _wagerDbClient;
        private readonly MySqlClient _mainDbClient;
        private readonly TpUnsettleService _tpUnsettleService;

        public WagerAllBetService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
        }

        public bool InsertWager(BetAllBet bet)
        {
            var parameters = new DynamicParameters(bet);
            //將新api回傳欄位對應回舊的table欄位
            parameters.AddDynamicParams(new
            {
                client = bet.player,
                winOrLoss = bet.winOrLossAmount,
                state = bet.status,
                depositAmount = bet.deposit
            });
            var result = _wagerDbClient.Execute("Service_TpWagerAllBet_Ins_V2", parameters);
            if (result && bet.status == 111)
            {
                _tpUnsettleService.InsertTpUnsettle(Tp.Allbet, GameType.Allbet_LiveCasino, bet.betNum.ToString());
            }
            return result;
        }

        public IEnumerable<WagerAllBetTotal> GetWagerTotal(long beginTime, long endTime)
        {
            return _wagerDbClient.Query<WagerAllBetTotal>("Service_TpWagerAllBetTotal_Get_V2", new DynamicParameters(new { BeginTime = beginTime, EndTime = endTime }));
        }

        public Task<RewardAllBetData> GetRewardRecord(long eventRecordNum)
        {
            return _mainDbClient.QueryFirstAsync<RewardAllBetData>("Service_RewardLC_Allbet_Get", new DynamicParameters(new { eventRecordNum }));
        }

        public Task<IEnumerable<UnsettleCheck>> GetUnsettleCheckList()
        {
            return _mainDbClient.QueryAsync<UnsettleCheck>("Service_BettransLC_Allbet_Unsettle_Check", null);
        }
    }
}
