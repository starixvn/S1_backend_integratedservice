using Dapper;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerBNGService
    {
        private readonly MySqlClient _wagerDbClient;
        public WagerBNGService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public Task<int> InsertWager(item data)
        {
            var para = new WagerBNGModel()
            {
                transaction_id = data.transaction_id,
                original_transaction_id = data.original_transaction_id,
                type = data.type,
                player_id = data.player_id,
                brand = data.brand,
                currency = data.currency,
                mode = data.mode,
                is_test = data.is_test,
                platform = data.platform,
                tag = data.tag,
                round_id = data.round_id,
                round_started = data.round_started,
                round_finished = data.round_finished,
                bet = data.bet != null ? Convert.ToDecimal(data.bet) : 0,
                win = data.win != null ? Convert.ToDecimal(data.win) : 0,
                balance_after = data.balance_after != null ? Convert.ToDecimal(data.balance_after) : 0,
                auto_paid = data.auto_paid,
                provider_id = data.provider_id,
                provider_name = data.provider_name,
                game_id = data.game_id,
                game_name = data.game_name,
                status = data.status,
                c_at = data.c_at.DateTime,
                bonus_id = data.bonus_id != null ? data.bonus_id.Value : 0,
                campaign = data.campaign,
                bonus_source = data.bonus_source,
                ext_bonus_id = data.ext_bonus_id,
                bonus_event = data.bonus_event,
                bonus_type = data.bonus_type
            };
            return _wagerDbClient.ExecuteAsync("Service_TpWager_BNG_Ins", new DynamicParameters(para));
        }
    }
}
