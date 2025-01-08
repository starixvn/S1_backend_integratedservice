using BetDetailApiHost.Models.ConfirmBet;
using BetDetailApiHost.Models.ConfirmbetParlay;
using BetDetailApiHost.Models.GetBetDetail;
using BetDetailApiHost.Models.PlaceBet;
using BetDetailApiHost.Models.PlaceBetParlay;
using BetDetailApiHost.Models.Resettle;
using BetDetailApiHost.Models.Settle;
using BetDetailApiHost.Models.Unsettle;

namespace BetDetailApiHost.Helper;

public interface IMemoryDb
{
    Task SetBetDetailKeyAsync(PlaceBetParlayRequest source);

    Task SetBetDetailKeyAsync(PlaceBetRequest confirmBetRequest);

    Task SetBetDetailKeyAsync(ConfirmBetRequest confirmBetRequest);

    Task SetBetDetailKeyAsync(ConfirmBetParlayRequest source);

    Task SetBetDetailKeyAsync(ResettleRequest source);

    Task SetBetDetailKeyAsync(UnsettleRequest source);

    Task SetBetDetailKeyAsync(SettleRequest settleRequest);

    Task ClearTicketsAsync();

    Task ClearSettleTicketsAsync();

    Task<List<BetDetail>> GetBetDetailsAsync();
}

public class MemoryDb : IMemoryDb
{
    private static readonly Dictionary<string, BetDetail> BetStorage = [];

    private static long LastVersionKey = 0;
    private static bool IsUpdated = false;

    private ConcurrentList<BetDetail> _placebetTicket = new();
    private ConcurrentList<BetDetail> _confirmBetTicket = new();
    private ConcurrentList<BetDetail> _settledTicket = new();

    public async Task ClearTicketsAsync()
    {
        await _placebetTicket.Clear();
        await _confirmBetTicket.Clear();
        await _settledTicket.Clear();
    }

    public async Task ClearSettleTicketsAsync()
    {
        await _settledTicket.Clear();
    }

    public async Task<List<BetDetail>> GetBetDetailsAsync()
    {
        var confirmBetTickets = await _confirmBetTicket.GetAll();
        var settleBetTickets = await _settledTicket.GetAll();

        var result = confirmBetTickets.Concat(settleBetTickets)
                                                     .Where(bet => bet.version_key >= LastVersionKey)
                                                     .GroupBy(bet => bet.trans_id)
                                                     .SelectMany(group => group)
                                                     .ToList();

        if (IsUpdated)
        {
            IsUpdated = false;
            LastVersionKey++;
        }

        return result;
    }

    private GetBetDetailResponse GenerateResponse(List<BetDetail> betDetails)
    {
        var resposne = new GetBetDetailResponse()
        {
            ErrorCode = 0,
            Message = "Success",
            Data = new()
            {
                LastVersionKey = LastVersionKey,
                BetDetails = betDetails,
                BetNumberDetails = [],
                BetVirtualSportDetails = [],
                BetCasinoDetails = [],
            }
        };

        return resposne;
    }

    public async Task SetBetDetailKeyAsync(SettleRequest request)
    {
        foreach (var transaction in request.message.Txns)
        {
            var savedTicket = await _confirmBetTicket.Find(ticket => ticket.trans_id.Equals(transaction.TxId));
            savedTicket.version_key = (int)LastVersionKey;
            savedTicket.ticket_status = transaction.Status;
            savedTicket.ticket_extra_status = transaction.ExtraStatus;
            savedTicket.settlement_time = DateTime.Parse(transaction.SettlementTime).ToUniversalTime().AddHours(-4).ToString();
            savedTicket.winlost_datetime = DateTime.Parse(transaction.WinlostDate).ToUniversalTime().AddHours(-4);
            savedTicket.winlost_amount = transaction.Payout - savedTicket.stake;

            await _settledTicket.Add(savedTicket);
            await _confirmBetTicket.Remove(savedTicket);
        }
        IsUpdated = true;
    }

    public async Task SetBetDetailKeyAsync(ResettleRequest request)
    {
        foreach (var transaction in request.message.Txns)
        {
            var savedTicket = await _settledTicket.Find(ticket => ticket.trans_id.Equals(transaction.TxId));
            savedTicket.version_key = (int)LastVersionKey;
            savedTicket.ticket_status = transaction.Status;
            savedTicket.ticket_extra_status = transaction.ExtraStatus;
            savedTicket.settlement_time = DateTime.Parse(transaction.SettlementTime).ToUniversalTime().AddHours(-4).ToString();
            savedTicket.winlost_datetime = DateTime.Parse(transaction.WinlostDate).ToUniversalTime().AddHours(-4);
            savedTicket.winlost_amount = transaction.Payout - savedTicket.stake;
        }
        IsUpdated = true;
    }

    public async Task SetBetDetailKeyAsync(UnsettleRequest request)
    {
        foreach (var transaction in request.message.Txns)
        {
            var savedTicket = await _settledTicket.Find(ticket => ticket.trans_id.Equals(transaction.TxId));
            await _settledTicket.Remove(savedTicket);

            savedTicket.version_key = (int)LastVersionKey;
            await _confirmBetTicket.Add(savedTicket);
        }
        IsUpdated = true;
    }

    #region PlaceBet

    public async Task SetBetDetailKeyAsync(PlaceBetParlayRequest request)
    {
        foreach (var transaction in request.message.Txns)
        {
            foreach (var comboInfoDetail in transaction.Detail)
            {
                var ticket = new BetDetail()
                {
                    //TODO: Double check later
                    operator_id = "Starix1SW",
                    winlost_amount = -transaction.BetAmount,
                    ref_id = transaction.RefId,
                    vendor_member_id = request.message.UserId,
                    original_stake = transaction.BetAmount,
                    bet_type = 29,
                    bettypename = new List<Bettypename>
                {
                    new ()
                    {
                        lang = "en",
                        name = "System Parlay"
                    },
                    new ()
                    {
                        lang = "cs",
                        name = "系统串关"
                    }
                }
                };

                var singleBetDetail = comboInfoDetail as SingleBetDetail;

                if (singleBetDetail is not null)
                {
                    ticket.stake = transaction.BetAmount;

                    var match = request.message.TicketDetail.Find(x => x.MatchId == singleBetDetail.MatchId);
                    ticket.league_id = match.LeagueId;
                    ticket.match_id = match.MatchId;
                    ticket.home_id = match.HomeId;
                    ticket.away_id = match.AwayId;
                    ticket.match_datetime = match.MatchDateTime;
                    ticket.sport_type = match.SportType;
                    ticket.bet_type = match.BetType;
                    ticket.odds_type = match.OddsType;
                    ticket.bet_team = match.BetTeam;
                    ticket.home_score = match.HomeScore;
                    ticket.away_score = match.AwayScore;
                    ticket.betchoice = match.BetChoice;
                    ticket.leaguename = [new Leaguename { name = match.LeagueName, lang = "en" }];
                    ticket.sportname = [new Sportname { name = match.SportTypeName, lang = "en" }];
                    ticket.bettypename = [new Bettypename { name = match.BetTypeName, lang = "en" }];
                    await _placebetTicket.Add(ticket);
                    continue;
                }

                var parlayBetDetail = comboInfoDetail as ParlayDetail;
                if (parlayBetDetail != null)
                {
                    ticket.odds = parlayBetDetail.Odds ?? 0;
                    ticket.stake = parlayBetDetail.Stake;
                    ticket.bet_team = parlayBetDetail.Type.ToString();
                    await _placebetTicket.Add(ticket);
                    continue;
                }
            }
        }
    }

    public async Task SetBetDetailKeyAsync(PlaceBetRequest request)
    {
        await _placebetTicket.Add(new BetDetail()
        {
            ref_id = request.message.RefId,

            operator_id = request.message.OperationId,
            vendor_member_id = request.message.UserId,
            league_id = request.message.LeagueId,
            match_id = request.message.MatchId,
            home_id = request.message.HomeId,
            away_id = request.message.AwayId,
            match_datetime = request.message.MatchDateTime,
            sport_type = request.message.SportType,
            bet_type = request.message.BetType,
            odds = request.message.Odds,
            stake = request.message.BetAmount,
            transaction_time = request.message.BetTime,
            currency = request.message.Currency,
            odds_type = request.message.OddsType,
            bet_team = request.message.BetTeam,
            home_score = request.message.HomeScore,
            away_score = request.message.AwayScore,
            betfrom = request.message.BetFrom,
            betchoice = request.message.BetChoice,
            winlost_amount = request.message.Odds < 0 ? request.message.Odds * request.message.BetAmount : -request.message.BetAmount,
            leaguename = new List<Leaguename> { new Leaguename { name = request.message.LeagueName, lang = "en" } },
            sportname = new List<Sportname> { new Sportname { name = request.message.SportTypeName, lang = "en" } },
            bettypename = new List<Bettypename> { new Bettypename { name = request.message.BetTypeName, lang = "en" } },
        });
    }

    #endregion PlaceBet

    #region ConfirmBet

    public async Task SetBetDetailKeyAsync(ConfirmBetParlayRequest request)
    {
        foreach (var transaction in request.message.Txns)
        {
            try
            {
                var ticket = await _placebetTicket.Find(x => x.ref_id == transaction.RefId);
                ticket.version_key = (int)LastVersionKey;
                ticket.trans_id = transaction.TxId;
                ticket.transaction_time = DateTime.Parse(request.message.TransactionTime).ToUniversalTime().AddHours(-4).ToString();
                ticket.ticket_status = "running";
                await _confirmBetTicket.Add(ticket);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        IsUpdated = true;
    }

    public async Task SetBetDetailKeyAsync(ConfirmBetRequest request)
    {
        foreach (var transaction in request.message.Txns)
        {
            try
            {
                var ticket = await _placebetTicket.Find(x => x.ref_id == transaction.RefId);
                ticket.version_key = (int)LastVersionKey;
                ticket.trans_id = transaction.TxId;
                ticket.transaction_time = DateTime.Parse(request.message.TransactionTime).ToUniversalTime().AddHours(-4).ToString();
                ticket.ticket_status = "running";
                await _confirmBetTicket.Add(ticket);
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        }

        IsUpdated = true;
    }

    #endregion ConfirmBet
}