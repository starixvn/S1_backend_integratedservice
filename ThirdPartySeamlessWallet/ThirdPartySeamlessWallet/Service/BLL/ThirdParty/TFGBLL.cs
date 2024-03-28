using System.Data;
using Dapper;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class TFGBLL : ITFGBLL
    {
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
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _logger = logger;
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public async Task<Result> Transfer(TFGTransferReq req)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, req.loginName);
            if (tpCustInfo == null)
            {
                return new Result() { Code = ResultCode.UserLockedOrNotFound, Message = ResultCode.UserLockedOrNotFound.ToString() };
            }
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate < 0)
            {
                return new Result() { Code = ResultCode.CurrencyError, Message = ResultCode.CurrencyError.ToString() };
            }
            req.amount *= tpCurrency.ExchangeRate;
            Result result;
            //下注
            if (req.placeBet)
            {
                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, req.loginName);
                if (tpCheck == null || tpCheck.Locked)
                {
                    return new Result() { Code = ResultCode.UserLockedOrNotFound, Message = ResultCode.UserLockedOrNotFound.ToString() };
                }

                #region placeBetTFG
                var placeBetTFG = new PlaceBet_TFG()
                {
                    loginName = req.loginName,
                    amount = Math.Abs(req.amount),
                    placeBet = req.placeBet,
                    ticketNum = req.ticketNum,
                    description = req.description,
                    id = req.ticketDetail.id,
                    odds = req.ticketDetail.odds,
                    result = req.ticketDetail.result,
                    map_num = string.IsNullOrWhiteSpace(req.ticketDetail.map_num) ? String.Empty : req.ticketDetail.map_num,
                    currency = req.ticketDetail.currency,
                    event_id = req.ticketDetail.event_id,
                    handicap = req.ticketDetail.handicap,
                    is_combo = req.ticketDetail.is_combo,
                    euro_odds = req.ticketDetail.euro_odds == null ? 0 : req.ticketDetail.euro_odds,
                    market_id = req.ticketDetail.market_id,
                    event_name = req.ticketDetail.event_name,
                    malay_odds = req.ticketDetail.malay_odds == null ? 0 : req.ticketDetail.malay_odds,
                    member_code = req.ticketDetail.member_code,
                    member_odds = req.ticketDetail.member_odds,
                    ticket_type = req.ticketDetail.ticket_type,
                    date_created = req.ticketDetail.date_created == null ? DateTime.MinValue : req.ticketDetail.date_created?.DateTime,
                    game_type_id = req.ticketDetail.game_type_id,
                    is_unsettled = req.ticketDetail.is_unsettled,
                    bet_selection = req.ticketDetail.bet_selection,
                    bet_type_name = string.IsNullOrWhiteSpace(req.ticketDetail.bet_type_name) ? String.Empty : req.ticketDetail.bet_type_name,
                    market_option = string.IsNullOrWhiteSpace(req.ticketDetail.market_option) ? String.Empty : req.ticketDetail.market_option,
                    request_source = req.ticketDetail.request_source,
                    event_datetime = req.ticketDetail.event_datetime == null ? DateTime.MinValue : req.ticketDetail.event_datetime?.DateTime,
                    game_type_name = req.ticketDetail.game_type_name,
                    competition_name = req.ticketDetail.competition_name,
                    game_market_name = string.IsNullOrWhiteSpace(req.ticketDetail.game_market_name) ? String.Empty : req.ticketDetail.game_market_name,
                    member_odds_style = req.ticketDetail.member_odds_style,
                    modified_datetime = req.ticketDetail.modified_datetime == null ? DateTime.MinValue : req.ticketDetail.modified_datetime?.DateTime,
                    settlement_status = req.ticketDetail.settlement_status,
                    settlement_datetime = req.ticketDetail.settlement_datetime == null ? DateTime.MinValue : req.ticketDetail.settlement_datetime?.DateTime,
                    tickets = req.ticketDetail.is_combo ? JsonConvert.SerializeObject(req.ticketDetail.tickets) : "{}",
                    TpGameType = GameType
                };
                #endregion

                result = await Bet(tpCustInfo, placeBetTFG);
            }
            else //stttle,unsettle
            {
                #region settleTFG
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
                    map_num = string.IsNullOrWhiteSpace(req.ticketDetail.map_num) ? String.Empty : req.ticketDetail.map_num,
                    currency = req.ticketDetail.currency,
                    earnings = req.ticketDetail.earnings == null ? 0 : req.ticketDetail.earnings,
                    event_id = req.ticketDetail.event_id,
                    handicap = req.ticketDetail.handicap,
                    is_combo = req.ticketDetail.is_combo,
                    euro_odds = req.ticketDetail.euro_odds == null ? 0 : req.ticketDetail.euro_odds,
                    market_id = req.ticketDetail.market_id,
                    event_name = req.ticketDetail.event_name,
                    malay_odds = req.ticketDetail.malay_odds == null ? 0 : req.ticketDetail.malay_odds,
                    member_code = req.ticketDetail.member_code,
                    member_odds = req.ticketDetail.member_odds,
                    ticket_type = req.ticketDetail.ticket_type,
                    date_created = req.ticketDetail.date_created == null ? DateTime.MinValue : req.ticketDetail.date_created?.DateTime,
                    game_type_id = req.ticketDetail.game_type_id,
                    is_unsettled = req.ticketDetail.is_unsettled,
                    bet_selection = req.ticketDetail.bet_selection,
                    bet_type_name = string.IsNullOrWhiteSpace(req.ticketDetail.bet_type_name) ? String.Empty : req.ticketDetail.bet_type_name,
                    market_option = string.IsNullOrWhiteSpace(req.ticketDetail.market_option) ? String.Empty : req.ticketDetail.market_option,
                    request_source = req.ticketDetail.request_source,
                    event_datetime = req.ticketDetail.event_datetime == null ? DateTime.MinValue : req.ticketDetail.event_datetime?.DateTime,
                    game_type_name = req.ticketDetail.game_type_name,
                    competition_name = req.ticketDetail.competition_name,
                    game_market_name = string.IsNullOrWhiteSpace(req.ticketDetail.game_market_name) ? String.Empty : req.ticketDetail.game_market_name,
                    member_odds_style = req.ticketDetail.member_odds_style,
                    modified_datetime = req.ticketDetail.modified_datetime == null ? DateTime.MinValue : req.ticketDetail.modified_datetime?.DateTime,
                    settlement_status = req.ticketDetail.settlement_status,
                    settlement_datetime = req.ticketDetail.settlement_datetime == null ? DateTime.MinValue : req.ticketDetail.settlement_datetime?.DateTime,
                    tickets = req.ticketDetail.is_combo ? JsonConvert.SerializeObject(req.ticketDetail.tickets) : "{}",
                    TpGameType = GameType,
                    result_status = string.IsNullOrWhiteSpace(req.ticketDetail.result_status) ? String.Empty : req.ticketDetail.result_status,
                };
                #endregion

                result = await Settle(tpCustInfo, settleTFG);
            }

            return result;
        }

        public async Task<Result> Rollback(TFGRallBackReq req)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, req.loginName);
            if (tpCustInfo == null)
            {
                return new Result() { Code = ResultCode.UserLockedOrNotFound, Message = ResultCode.UserLockedOrNotFound.ToString() };
            }
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate < 0)
            {
                return new Result() { Code = ResultCode.CurrencyError, Message = ResultCode.CurrencyError.ToString() };
            }
            req.amount *= tpCurrency.ExchangeRate;

            var placeBetTFG = new PlaceBet_TFG()
            {
                amount = req.amount,
                ticketNum = req.ticketNum,
                description = req.description,
                id = req.ticketDetail.id
            };

            var result = await RollBack(tpCustInfo, placeBetTFG);

            return result;
        }

        private async Task<Result> Bet(TpCustInfo tpCustInfo, PlaceBet_TFG bet)
        {
            try
            {
                var bonusId = 0;
                await using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                await using var trans = await conn.BeginTransactionAsync();

                bet.TicketType = (int)BettransType.Bet;
                bet.TicketStatus = (int)TicketStatus.UnSettled;
                var p = new DynamicParameters(bet);

                var check = conn.Query<Check_TFG>("Service_BettransSports_TFG_Check", new { id = bet.id }, commandType: CommandType.StoredProcedure);
                if (check.Any())
                {
                    return new Result { Code = ResultCode.TransactionDuplicated, Message = ResultCode.TransactionDuplicated.ToString() };
                }

                var result = await conn.ExecuteAsync("Service_BettransSports_TFG_Bet_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result < 1)
                {
                    return new Result { Code = ResultCode.DatabaseError, Message = "Service_BettransSports_TFG_Bet_Ins" };
                }
                result = _bettingService.PlaceBet(
                        tpCustInfo: tpCustInfo,
                        amount: Math.Abs(bet.amount) * -1,
                        tpId: (int)Tp.TFG,
                        tpTransId: bet.id,
                        gameType: (int)bet.TpGameType,
                        refId: bet.id,
                        afterAmount: out var afterAmount,
                        bonusId: ref bonusId,
                        useBonus: out _,
                        conn: conn,
                        trans: trans
                );
                if (result != 1)
                {
                    return new Result { Code = ResultCode.PlaceBetError, Message = ResultCode.PlaceBetError.ToString() };
                }

                result = _tpUnsettleService.InsertTpUnsettle(Tp.TFG, GameType, bet.id, trans);

                if (result < 1)
                {
                    return new Result { Code = ResultCode.InsertTpUnsettleError, Message = ResultCode.InsertTpUnsettleError.ToString() };
                }

                await trans.CommitAsync();
                return new Result { Code = ResultCode.Success };
            }
            catch (Exception ex)
            {
                return new Result { Code = ResultCode.UnknownError, Message = $"Bet\r\n{ex.ToString()}" };
            }
        }

        private async Task<Result> Settle(TpCustInfo tpCustInfo, Settle_TFG settle)
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
                    return new Result { Code = ResultCode.TransactionNotFound, Message = ResultCode.TransactionNotFound.ToString() };
                }

                //寫入Settle資料
                var p = new DynamicParameters(settle);
                var result = await conn.ExecuteAsync("Service_BettransSports_TFG_Settle_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result < 1)
                {
                    return new Result { Code = ResultCode.DatabaseError, Message = "Service_BettransSports_TFG_Settle_Ins" };
                }

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
                            tpTransId: settle.id,
                            gameType: (int)GameType,
                            tpCategory: TpCategory.Esports,
                            refId: settle.id,
                            transType,
                            betData: null,
                            out decimal afterAmount,
                            settle.description,
                            out int bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow);

                    if (result < 1)
                    {
                        return new Result { Code = ResultCode.PayoutError, Message = ResultCode.PayoutError.ToString() };
                    }
                }

                result = _tpUnsettleService.InsertTpUnsettle(Tp.TFG, GameType, settle.id, trans);

                if (result < 1)
                {
                    return new Result { Code = ResultCode.InsertTpUnsettleError, Message = ResultCode.InsertTpUnsettleError.ToString() };
                }

                trans.Commit();
                return new Result { Code = ResultCode.Success };
            }
            catch (Exception ex)
            {
                return new Result { Code = ResultCode.UnknownError, Message = $"Settle\r\n{ex.ToString()}" };
            }
        }

        private async Task<Result> RollBack(TpCustInfo tpCustInfo, PlaceBet_TFG bet)
        {
            try
            {
                await using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                await using var trans = await conn.BeginTransactionAsync();

                //確定住單狀態是否為UnSettled
                var check = conn.Query<Check_TFG>("Service_BettransSports_TFG_Check", new { id = bet.id }, commandType: CommandType.StoredProcedure)?.FirstOrDefault(x => x.TicketStatus == (int)TicketStatus.UnSettled);
                if (check == null)
                {
                    return new Result { Code = ResultCode.TransactionDuplicated, Message = ResultCode.TransactionDuplicated.ToString() };
                }

                var result = await conn.ExecuteAsync("Service_BettransSports_TFG_Cancel_Set", new { id = bet.id }, transaction: trans, commandType: CommandType.StoredProcedure);


                if (result < 1)
                {
                    return new Result { Code = ResultCode.DatabaseError, Message = "Service_BettransSports_TFG_Cancel_Set" };
                }

                result = _settledService.PayoutV2(
                            tpCustInfo: tpCustInfo,
                            amount: bet.amount * -1,
                            tpId: (int)ThirdParty,
                            tpTransId: bet.id,
                            gameType: (int)GameType,
                            tpCategory: TpCategory.Esports,
                            refId: bet.id,
                            TransType.Refunds,
                            betData: null,
                            out decimal afterAmount,
                            bet.description,
                            out int bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow);

                if (result != 1)
                {
                    return new Result { Code = ResultCode.PayoutError, Message = ResultCode.PayoutError.ToString() };
                }

                result = _tpUnsettleService.InsertTpUnsettle(Tp.TFG, GameType, bet.id, trans);

                if (result < 1)
                {
                    return new Result { Code = ResultCode.InsertTpUnsettleError, Message = ResultCode.InsertTpUnsettleError.ToString() };
                }

                trans.Commit();
                return new Result { Code = ResultCode.Success };
            }
            catch (Exception ex)
            {
                return new Result { Code = ResultCode.UnknownError, Message = $"RollBack\r\n{ex.ToString()}" };
            }
        }
    }
}
