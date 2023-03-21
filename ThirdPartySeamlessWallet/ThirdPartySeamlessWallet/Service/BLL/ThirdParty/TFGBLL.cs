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
using Dapper;
using Serilog;
using System.Data;
using ThirdPartySeamlessWallet.Service.Library;
using System.Diagnostics;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class TFGBLL : ITFGBLL
    {
        private readonly AuthBLL authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly ILogger<TFGBLL> _logger;
        private readonly MySqlClient _mainDbClient;
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
            _settledService = provider.GetRequiredService<SettledService>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _logger = logger;
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public async Task<TFGTransferResp> Transfer(TFGTransferReq req)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(req.loginName);
            req.amount *= tpCurrency.ExchangeRate;

            //下注
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
                result = await Bet(tpCustInfo, placeBetTFG);

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
            else //stttle,unsettle
            {
                SettleResult result;
                var settleTFG = new Settle_TFG()
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
                result = await Settle(tpCustInfo, settleTFG);
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
        }

        public async Task<TFGRallBackResp> Rollback(TFGRallBackReq req)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(req.loginName);
            req.amount *= tpCurrency.ExchangeRate;

            BetResult result;
            var placeBetTFG = new PlaceBet_TFG()
            {
                amount = req.amount,
                ticketNum = req.ticketNum,
                description = req.description,
                id = req.ticketDetail.id
            };

            result = await RollBack(tpCustInfo, placeBetTFG);

            return result.result switch
            {
                1 => new TFGRallBackResp()
                {

                },
                -1 => throw new TFGException(404),
                -2 => throw new TFGException(404),
                _ => throw new TFGException(404)
            };
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

        private async Task<BetResult> Bet(TpCustInfo tpCustInfo, PlaceBet_TFG dto)
        {
            try
            {
                var bonusId = 0;
                await using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                await using var trans = await conn.BeginTransactionAsync();

                dto.TicketType = (int)BettransType.Bet;
                dto.TicketStatus = (int)TicketStatus.UnSettled;
                var p = new DynamicParameters(dto);

                var check = conn.Query<Check_TFG>("Service_BettransSports_TFG_Check", new { id = dto.id }, commandType: CommandType.StoredProcedure);
                if (check.Any())
                {
                    return new BetResult { result = -1 };
                }

                var result = await conn.ExecuteAsync("Service_BettransSports_TFG_Bet", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result < 1)
                {
                    _logger.LogError($"loginName:{dto.loginName}\r\nticketNum:{dto.ticketNum}\r\nPlaceBet_TFG");
                    return new BetResult { result = -1 };
                }
                result = _bettingService.PlaceBetV2(
                        tpCustInfo: tpCustInfo,
                        amount: Math.Abs(dto.amount) * -1,
                        tpId: (int)Tp.TFG,
                        tpTransId: dto.id,
                        gameType: (int)dto.TpGameType,
                        refId: dto.id,
                        betData: null,
                        tpCategory: TpCategory.VirtualSports,
                        afterAmount: out var afterAmount,
                        bonusId: ref bonusId,
                        conn: conn,
                        trans: trans
                );
                if (result != 1)
                {
                    _logger.LogError($"PlaceBet_TFG\r\nPlaceBetV2Error");
                    return new BetResult() { result = -3 };
                }
                var tfgResult = new BetResult
                {
                    result = 1
                };
                await trans.CommitAsync();
                return tfgResult;
            }
            catch (Exception ex)
            {
                Log.Error($"PlaceBet_TFG\r\n{ex.ToString()}");
                return new BetResult { result = -2 };
            }
        }

        private async Task<SettleResult> Settle(TpCustInfo tpCustInfo, Settle_TFG settle)
        {
            try
            {
                await using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                await using var trans = await conn.BeginTransactionAsync();

                //取得下注資料
                var check = conn.Query<Check_TFG>("Service_BettransSports_TFG_Check", new { id = settle.id }, commandType: CommandType.StoredProcedure);
                if (!check.Any())
                {
                    return new SettleResult { result = -1 };
                }

                //寫入Settle資料
                var p = new DynamicParameters(settle);
                var result = await conn.ExecuteAsync("Service_BettransSports_TFG_Settle", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result < 1)
                {
                    _logger.LogError($"loginName:{settle.loginName}\r\nticketNum:{settle.ticketNum}\r\nSettle");
                    return new SettleResult { result = -1 };
                }
                var betAmount = check.Sum(x => x.amount);
                //var amount = settle.amount - betAmount;
                TransType transType = settle.description switch
                {
                    "Settlement" => TransType.Payout,
                    "Loss" => TransType.Payout,
                    "Unsettlement" => TransType.Undo
                };
                if (settle.amount != 0)
                {
                    result = _settledService.PayoutV2(
                            tpCustInfo: tpCustInfo,
                            amount: settle.amount,
                            tpId: (int)ThirdParty,
                            tpTransId: settle.ticketNum,
                            gameType: (int)GameType,
                            tpCategory: TpCategory.VirtualSports,
                            refId: settle.ticketNum,
                            transType,
                            betData: null,
                            out decimal afterAmount,
                            settle.description,
                            out int bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow,
                            SeamlessBonusType.TpId);

                    if (result < 1)
                    {
                        _logger.LogError($"loginName:{settle.loginName}\r\nticketNum:{settle.ticketNum}\r\nPayoutV2");
                        return new SettleResult { result = -1 };
                    }
                }

                result = _tpUnsettleService.InsertTpUnsettle(Tp.SBO, GameType, settle.ticketNum, trans);

                if (result < 1)
                {
                    _logger.LogError($"loginName:{settle.loginName}\r\nticketNum:{settle.ticketNum}\r\nPayoutV2");
                    return new SettleResult { result = -1 };
                }

                trans.Commit();
                return new SettleResult { result = 1 };
            }
            catch (Exception ex)
            {
                Log.Error($"Settle_TFG\r\n{ex.ToString()}");
                return new SettleResult { result = -2 };
            }
        }

        private async Task<BetResult> RollBack(TpCustInfo tpCustInfo, PlaceBet_TFG dto)
        {
            try
            {
                await using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                await using var trans = await conn.BeginTransactionAsync();

                //確定住單狀態是否為UnSettled
                var check = conn.Query<Check_TFG>("Service_BettransSports_TFG_Check", new { id = dto.id }, commandType: CommandType.StoredProcedure)?.FirstOrDefault(x => x.TicketStatus == (int)TicketStatus.UnSettled);
                if (check == null)
                {
                    return new BetResult { result = -1 };
                }

                var result = await conn.ExecuteAsync("Service_BettransSports_TFG_Cancel", new { id = dto.id }, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result < 1)
                {
                    return new BetResult { result = -1 };
                }

                result = _settledService.PayoutV2(
                            tpCustInfo: tpCustInfo,
                            amount: dto.amount * -1,
                            tpId: (int)ThirdParty,
                            tpTransId: dto.ticketNum,
                            gameType: (int)GameType,
                            tpCategory: TpCategory.VirtualSports,
                            refId: dto.ticketNum,
                            TransType.Refunds,
                            betData: null,
                            out decimal afterAmount,
                            dto.description,
                            out int bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow,
                            SeamlessBonusType.TpId);

                if (result != 1)
                {
                    _logger.LogError($"PlaceBet_TFG\r\nPlaceBetV2Error");
                    return new BetResult() { result = -3 };
                }
                var tfgResult = new BetResult
                {
                    result = 1
                };
                await trans.CommitAsync();
                return tfgResult;
            }
            catch (Exception ex)
            {
                Log.Error($"PlaceBet_TFG\r\n{ex.ToString()}");
                return new BetResult { result = -2 };
            }
        }
    }
}
