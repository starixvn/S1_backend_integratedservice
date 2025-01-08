export const GetParlayConfirmBetDetail = (
  placeBetParlayRequest,
  confirmBetParlayRequest
) => {
  const betDetails = [];

  placeBetParlayRequest.message.Txns.forEach((transaction) => {
    transaction.Detail.forEach((comboInfoDetail) => {
      const ticket = {
        operator_id: "Starix1SW",
        winlost_amount: -transaction.BetAmount,
        ref_id: transaction.RefId,
        vendor_member_id: placeBetParlayRequest.message.UserId,
        original_stake: transaction.BetAmount,
        bet_type: 29,
        bettypename: [
          { lang: "en", name: "System Parlay" },
          { lang: "cs", name: "系统串关" },
        ],
      };

      // Check if comboInfoDetail is a SingleBetDetail
      if (comboInfoDetail.Type === "SingleBetDetail") {
        const match = placeBetParlayRequest.message.TicketDetail.find(
          (x) => x.MatchId === comboInfoDetail.MatchId
        );

        if (match) {
          Object.assign(ticket, {
            stake: transaction.BetAmount,
            league_id: match.LeagueId,
            match_id: match.MatchId,
            home_id: match.HomeId,
            away_id: match.AwayId,
            match_datetime: match.MatchDateTime,
            sport_type: match.SportType,
            bet_type: match.BetType,
            odds_type: match.OddsType,
            bet_team: match.BetTeam,
            home_score: match.HomeScore,
            away_score: match.AwayScore,
            betchoice: match.BetChoice,
            leaguename: [{ name: match.LeagueName, lang: "en" }],
            sportname: [{ name: match.SportTypeName, lang: "en" }],
            bettypename: [{ name: match.BetTypeName, lang: "en" }],
          });
        }
        betDetails.push(ticket);
        return;
      }

      // Check if comboInfoDetail is a ParlayDetail
      if (comboInfoDetail.Type === "ParlayDetail") {
        Object.assign(ticket, {
          odds: comboInfoDetail.Odds || 0,
          stake: comboInfoDetail.Stake,
          bet_team: comboInfoDetail.Type.toString(),
        });
        betDetails.push(ticket);
        return;
      }
    });
  });

  confirmBetParlayRequest.message.Txns.forEach((transaction) => {
    const ticket = betDetails.find((x) => x.ref_id === transaction.RefId);

    if (ticket) {
      Object.assign(ticket, {
        version_key: 0, 
        trans_id: transaction.TxId,
        transaction_time: confirmBetParlayRequest.message.TransactionTime,
        ticket_status: "running",
      });
    }
  });

  return betDetails;
};

export const GetSingleConfirmBetDetail = (placeBetRequest, confirmBetRequest) => {
  const placeBetDetails = {
    ref_id: placeBetRequest.message.RefId,
    operator_id: placeBetRequest.message.OperationId,
    vendor_member_id: placeBetRequest.message.UserId,
    league_id: placeBetRequest.message.LeagueId,
    match_id: placeBetRequest.message.MatchId,
    home_id: placeBetRequest.message.HomeId,
    away_id: placeBetRequest.message.AwayId,
    match_datetime: placeBetRequest.message.MatchDateTime,
    sport_type: placeBetRequest.message.SportType,
    bet_type: placeBetRequest.message.BetType,
    odds: placeBetRequest.message.Odds,
    stake: placeBetRequest.message.BetAmount,
    transaction_time: placeBetRequest.message.BetTime,
    currency: placeBetRequest.message.Currency,
    odds_type: placeBetRequest.message.OddsType,
    bet_team: placeBetRequest.message.BetTeam,
    home_score: placeBetRequest.message.HomeScore,
    away_score: placeBetRequest.message.AwayScore,
    betfrom: placeBetRequest.message.BetFrom,
    betchoice: placeBetRequest.message.BetChoice,
    winlost_amount:
      placeBetRequest.message.Odds < 0
        ? placeBetRequest.message.Odds * placeBetRequest.message.BetAmount
        : -placeBetRequest.message.BetAmount,
    leaguename: [{ name: placeBetRequest.message.LeagueName, lang: "en" }],
    sportname: [{ name: placeBetRequest.message.SportTypeName, lang: "en" }],
    bettypename: [{ name: placeBetRequest.message.BetTypeName, lang: "en" }],
  };

  const confirmDetails = confirmBetRequest.message.Txns.map((transaction) => ({
    trans_id: transaction.TxId,
    ref_id: transaction.RefId,
    version_key: 0,
    ticket_status: "running",
    transaction_time: confirmBetRequest.message.TransactionTime,
  }));

  return confirmDetails.map((confirm) => ({
    ...placeBetDetails,
    ...confirm,
  }))[0];
};

export const GetSingleSettleBetDetail = (betDetail, settleBetRequest) => {
  const transaction = settleBetRequest.message.Txns.find(
    (txn) => txn.TxId == betDetail.trans_id
  );

  return {
    ...betDetail,
    ticket_status: transaction.Status,
    ticket_extra_status: transaction.ExtraStatus,
    settlement_time: transaction.SettlementTime,
    winlost_datetime: transaction.WinlostDate,
    winlost_amount: transaction.Payout - betDetail.stake,
  };
};