using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.Model;

public class GetBetListByTimeParam
{
    public IMSPRIBEConfig Setting { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }
    public int Page { get; set; }
    public string Currency { get; set; }
    public IMProductWalletEnum ProductWallet { get; set; }
    public int DateFilterType { get; set; }
    public int? Product { get; set; }
}

public class IMGetSportsBetLogReq
{
    public string MerchantCode { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public int Page { get; set; }
    public int ProductWallet { get; set; }
    public int DateFilterType { get; set; }
    public string Language { get; set; }
    public int? Product { get; set; }
}

public class IMGetBetLogReq
{
    public string MerchantCode { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int ProductWallet { get; set; }
    public string Currency { get; set; }
}

public class IMPagination
{
    public int CurrentPage { get; set; }
    public int TotalPage { get; set; }
    public int ItemPerPage { get; set; }
    public int TotalCount { get; set; }
}

public class IMWagerModel<T>
{
    public string Code { get; set; }
    public string Message { get; set; }
    public IEnumerable<T> Result { get; set; }
    public IMPagination Pagination { get; set; }
}

public class IMSlotModel
{
    public string Provider { get; set; }
    public string GameId { get; set; }
    public string GameName { get; set; }
    public string ChineseGameName { get; set; }
    public string RoundId { get; set; }
    public string ExternalRoundId { get; set; }
    public string PlayerId { get; set; }
    public string ProviderPlayerId { get; set; }
    public decimal BetAmount { get; set; }
    public decimal WinLoss { get; set; }
    public decimal ProgressiveBet { get; set; }
    public decimal ProgressiveWin { get; set; }
    public decimal Bonus { get; set; }
    public decimal ProviderBonus { get; set; }
    public string Status { get; set; }
    public string Platform { get; set; }
    public string DateCreated { get; set; }
    public string GameDate { get; set; }
    public string LastUpdatedDate { get; set; }
    public string BetDate { get; set; }
    public string SettlementDate { get; set; }
}

public class IMSportsbookModel
{
    public string Provider { get; set; }
    public string GameId { get; set; }
    public string BetId { get; set; }
    public string WagerCreationDateTime { get; set; }
    public string WagerEventDateTime { get; set; }
    public string PlayerId { get; set; }
    public string ProviderPlayerId { get; set; }
    public string Currency { get; set; }
    public decimal StakeAmount { get; set; }
    public decimal TurnOver { get; set; }
    public decimal MemberExposure { get; set; }
    public decimal PayoutAmount { get; set; }
    public decimal WinLoss { get; set; }
    public string ResultStatus { get; set; }
    public string OddsType { get; set; }
    public string WagerType { get; set; }
    public string Platform { get; set; }
    public string IsSettled { get; set; }
    public string IsResettled { get; set; }
    public string IsConfirmed { get; set; }
    public string IsCancelled { get; set; }
    public string BetTradeStatus { get; set; }
    public decimal BetTradeCommission { get; set; }
    public decimal BetTradeBuybackAmount { get; set; }
    public string ComboType { get; set; }
    public string LastUpdatedDate { get; set; }
    public string SettlementDate { get; set; }
    public string betTradeSuccessDateTime { get; set; }
    public IEnumerable<object> DetailItems { get; set; }
}

public class IMESportsModel
{
    public string Provider { get; set; }
    public string GameId { get; set; }
    public string BetId { get; set; }
    public string WagerCreationDateTime { get; set; }
    public string LastUpdatedDate { get; set; }
    public string PlayerId { get; set; }
    public string ProviderPlayerId { get; set; }
    public string ProviderOperatorId { get; set; }
    public string Currency { get; set; }
    public decimal StakeAmount { get; set; }
    public decimal WinLoss { get; set; }
    public decimal TurnOver { get; set; }
    public string OddsType { get; set; }
    public decimal TotalOdds { get; set; }
    public string WagerType { get; set; }
    public string Platform { get; set; }
    public string IsSettled { get; set; }
    public string IsResettled { get; set; }
    public string IsConfirmed { get; set; }
    public string IsCancelled { get; set; }
    public string SettlementDateTime { get; set; }
    public string Tolerance { get; set; }
    public string FlagCategory { get; set; }
    public IEnumerable<object> DetailItems { get; set; }
}

public class IMSlotWager
{
    public IMSlotWager() { }

    public IMSlotWager(IMSlotModel m)
    {
        Provider = m.Provider;
        GameId = m.GameId;
        GameName = m.GameName;
        ChineseGameName = m.ChineseGameName;
        RoundId = m.RoundId;
        ExternalRoundId = m.ExternalRoundId;
        PlayerId = m.PlayerId;
        ProviderPlayerId = m.ProviderPlayerId;
        BetAmount = m.BetAmount;
        WinLoss = m.WinLoss;
        ProgressiveBet = m.ProgressiveBet;
        ProgressiveWin = m.ProgressiveWin;
        Bonus = m.Bonus;
        ProviderBonus = m.ProviderBonus;
        Status = m.Status;
        Platform = m.Platform;
        DateCreated = DateTimeOffset.Parse(m.DateCreated).UtcDateTime;
        GameDate = DateTimeOffset.Parse(m.GameDate).UtcDateTime;
        LastUpdatedDate = DateTimeOffset.Parse(m.LastUpdatedDate).UtcDateTime;
        BetDate = DateTimeOffset.Parse(m.BetDate).UtcDateTime;
        SettlementDate = string.IsNullOrEmpty(m.SettlementDate) ? null
            : DateTimeOffset.Parse(m.SettlementDate).UtcDateTime;
    }

    public string Provider { get; set; }
    public string GameId { get; set; }
    public string GameName { get; set; }
    public string ChineseGameName { get; set; }
    public string RoundId { get; set; }
    public string ExternalRoundId { get; set; }
    public string PlayerId { get; set; }
    public string ProviderPlayerId { get; set; }
    public decimal BetAmount { get; set; }
    public decimal WinLoss { get; set; }
    public decimal ProgressiveBet { get; set; }
    public decimal ProgressiveWin { get; set; }
    public decimal Bonus { get; set; }
    public decimal ProviderBonus { get; set; }
    public string Status { get; set; }
    public string Platform { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime GameDate { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public DateTime BetDate { get; set; }
    public DateTime? SettlementDate { get; set; }
}

public class IMESportsWager
{
    public IMESportsWager() { }

    public IMESportsWager(IMESportsModel m)
    {
        Provider = m.Provider;
        GameId = m.GameId;
        BetId = m.BetId;
        WagerCreationDateTime = DateTimeOffset.Parse(m.WagerCreationDateTime).UtcDateTime;
        LastUpdatedDate = DateTimeOffset.Parse(m.LastUpdatedDate).UtcDateTime;
        PlayerId = m.PlayerId;
        ProviderPlayerId = m.ProviderPlayerId;
        ProviderOperatorId = m.ProviderOperatorId;
        Currency = m.Currency;
        StakeAmount = m.StakeAmount;
        WinLoss = m.WinLoss;
        TurnOver = m.TurnOver;
        OddsType = m.OddsType;
        TotalOdds = m.TotalOdds;
        WagerType = m.WagerType;
        Platform = m.Platform;
        IsSettled = m.IsSettled;
        IsResettled = m.IsResettled;
        IsConfirmed = m.IsConfirmed;
        IsCancelled = m.IsCancelled;
        SettlementDateTime = DateTimeOffset.TryParse(m.SettlementDateTime, out var settleDateTime)
            ? settleDateTime.UtcDateTime
            : null;
        Tolerance = m.Tolerance;
        FlagCategory = m.FlagCategory;
        DetailItems = m.DetailItems;
    }

    public string Provider { get; set; }
    public string GameId { get; set; }
    public string BetId { get; set; }
    public DateTime WagerCreationDateTime { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public string PlayerId { get; set; }
    public string ProviderPlayerId { get; set; }
    public string ProviderOperatorId { get; set; }
    public string Currency { get; set; }
    public decimal StakeAmount { get; set; }
    public decimal WinLoss { get; set; }
    public decimal TurnOver { get; set; }
    public string OddsType { get; set; }
    public decimal TotalOdds { get; set; }
    public string WagerType { get; set; }
    public string Platform { get; set; }
    public string IsSettled { get; set; }
    public string IsResettled { get; set; }
    public string IsConfirmed { get; set; }
    public string IsCancelled { get; set; }
    public DateTime? SettlementDateTime { get; set; }
    public string Tolerance { get; set; }
    public string FlagCategory { get; set; }
    public IEnumerable<object> DetailItems { get; set; }
}


public class IMSportsWager
{
    public IMSportsWager() { }

    public IMSportsWager(IMSportsbookModel m)
    {
        Provider = m.Provider;
        GameId = m.GameId;
        BetId = m.BetId;
        WagerCreationDateTime = DateTimeOffset.Parse(m.WagerCreationDateTime).UtcDateTime;
        WagerEventDateTime = DateTimeOffset.Parse(m.WagerEventDateTime).UtcDateTime;
        LastUpdatedDate = DateTimeOffset.Parse(m.LastUpdatedDate).UtcDateTime;
        PlayerId = m.PlayerId;
        ProviderPlayerId = m.ProviderPlayerId;
        Currency = m.Currency;
        StakeAmount = m.StakeAmount;
        TurnOver = m.TurnOver;
        MemberExposure = m.MemberExposure;
        PayoutAmount = m.PayoutAmount;
        WinLoss = m.WinLoss;
        ResultStatus = m.ResultStatus;
        OddsType = m.OddsType;
        WagerType = m.WagerType;
        Platform = m.Platform;
        IsSettled = m.IsSettled;
        IsResettled = m.IsResettled;
        IsConfirmed = m.IsConfirmed;
        IsCancelled = m.IsCancelled;
        BetTradeStatus = m.BetTradeStatus;
        BetTradeCommission = m.BetTradeCommission;
        BetTradeBuybackAmount = m.BetTradeBuybackAmount;
        ComboType = m.ComboType;
        SettlementDate = DateTimeOffset.TryParse(m.SettlementDate, out var settleDateTime)
            ? settleDateTime.UtcDateTime
            : null;
        // SettlementDate = string.IsNullOrEmpty(m.SettlementDate) ? null
        //     : DateTimeOffset.Parse(m.SettlementDate).UtcDateTime;
        betTradeSuccessDateTime = DateTimeOffset.Parse(m.betTradeSuccessDateTime).UtcDateTime;
        DetailItems = m.DetailItems;
    }

    public string Provider { get; set; }
    public string GameId { get; set; }
    public string BetId { get; set; }
    public DateTime WagerCreationDateTime { get; set; }
    public DateTime WagerEventDateTime { get; set; }
    public string PlayerId { get; set; }
    public string ProviderPlayerId { get; set; }
    public string Currency { get; set; }
    public decimal StakeAmount { get; set; }
    public decimal TurnOver { get; set; }
    public decimal MemberExposure { get; set; }
    public decimal PayoutAmount { get; set; }
    public decimal WinLoss { get; set; }
    public string ResultStatus { get; set; }
    public string OddsType { get; set; }
    public string WagerType { get; set; }
    public string Platform { get; set; }
    public string IsSettled { get; set; }
    public string IsResettled { get; set; }
    public string IsConfirmed { get; set; }
    public string IsCancelled { get; set; }
    public string BetTradeStatus { get; set; }
    public decimal BetTradeCommission { get; set; }
    public decimal BetTradeBuybackAmount { get; set; }
    public string ComboType { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public DateTime? SettlementDate { get; set; }
    public DateTime betTradeSuccessDateTime { get; set; }
    public IEnumerable<object> DetailItems { get; set; }
}