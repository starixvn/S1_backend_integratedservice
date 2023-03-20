using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;
using ThirdPartySeamlessWallet.Service.Model.SBSW;
using Newtonsoft.Json;
using static ThirdPartySeamlessWallet.Controllers.SexyController;
using ThirdPartySeamlessWallet.Extensions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class TFGBLL : ITFGBLL
    {
        private readonly AuthBLL authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService settledService;
        private readonly ILogger<TFGBLL> _logger;

        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }

        public TFGBLL(IServiceProvider provider, ILogger<TFGBLL> logger)
        {
            ThirdParty = Tp.TFG;
            GameType = GameType.TFG_Esports;
            authBLL = provider.GetRequiredService<AuthBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingService = provider.GetRequiredService<BettingService>();
            settledService = provider.GetRequiredService<SettledService>();
            _logger = logger;
        }

        public async Task<TFGTransferResp> Transfer(TFGTransferReq req)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(req.loginName);

            if (req.placeBet)
            {
                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, req.loginName);
                if (tpCheck == null || tpCheck.Locked)
                {
                    _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                    throw new TFGException(404);
                }
                BetResult result;
                var placeBetTFG = new PlaceBet_TFG()
                {
                    loginName = req.loginName,
                    amount = req.amount,
                    placeBet = req.placeBet,
                    ticketNum = req.ticketNum,
                    description = req.description,
                    id = req.ticketDetail.id,
                    odds = req.ticketDetail.odds,
                    result = req.ticketDetail.result,
                    map_num = req.ticketDetail.map_num,
                    currency = req.ticketDetail.currency,
                    earnings = req.ticketDetail.earnings,
                    event_id = req.ticketDetail.event_id,
                    handicap = req.ticketDetail.handicap,
                    is_combo = req.ticketDetail.is_combo,
                    euro_odds = req.ticketDetail.euro_odds,
                    market_id = req.ticketDetail.market_id,
                    event_name = req.ticketDetail.event_name,
                    malay_odds = req.ticketDetail.malay_odds,
                    member_code = req.ticketDetail.member_code,
                    member_odds = req.ticketDetail.member_odds,
                    ticket_type = req.ticketDetail.ticket_type,
                    date_created = req.ticketDetail.date_created,
                    game_type_id = req.ticketDetail.game_type_id,
                    is_unsettled = req.ticketDetail.is_unsettled,
                    bet_selection = req.ticketDetail.bet_selection,
                    bet_type_name = req.ticketDetail.bet_type_name,
                    market_option = req.ticketDetail.market_option,
                    request_source = req.ticketDetail.request_source,
                    event_datetime = req.ticketDetail.event_datetime,
                    game_type_name = req.ticketDetail.game_type_name,
                    competition_name = req.ticketDetail.competition_name,
                    game_market_name = req.ticketDetail.game_market_name,
                    member_odds_style = req.ticketDetail.member_odds_style,
                    modified_datetime = req.ticketDetail.modified_datetime,
                    settlement_status = req.ticketDetail.settlement_status,
                    settlement_datetime = req.ticketDetail.settlement_datetime,
                    tickets = req.ticketDetail.is_combo ? JsonConvert.SerializeObject(req.ticketDetail.tickets) : String.Empty,
                    TpGameType = GameType
                };
                result = await _bettingService.PlaceBet_TFG(tpCustInfo, placeBetTFG);

                return result.result switch
                {
                    1 => new TFGTransferResp()
                    {
                        
                    },
                    -1 => throw new TFGException(404),
                    -2 => throw new TFGException(404),
                    _ => throw new TFGException(404)
                };
            }
            else
            {
                throw new TFGException(404);
            }
            //return Task.FromResult<TFGTransferResp>(null);
        }

        private (TpCustInfo, TpCurrency) GetTPInfo(string tpUserName)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, tpUserName);
            if (tpCustInfo == null)
            {
                throw new TFGException(404, $"Customer:{tpUserName} not found.");
            }

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate < 0)
            {
                throw new TFGException(404, $"CurrencyId:{tpCustInfo.CurrencyId} not supported.");
            }
            return (tpCustInfo, tpCurrency);
        }
    }
}
