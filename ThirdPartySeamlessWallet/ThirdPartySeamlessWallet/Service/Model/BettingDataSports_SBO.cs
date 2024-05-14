using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataSports_SBO
    {

    }

    public class SBOResult
    {
        public int Result { get; set; }

        public long Trx_id { get; set; }

        public decimal AfterAmount { get; set; }
    }

    public class SBOBalanceRequest
    {
        public string CompanyKey { get; set; }
        public string Username { get; set; }
        public SBOProductType ProductType { get; set; }
        public int GameType { get; set; }
        public int Gpid { get; set; }
    }

    public class SBOBalanceResponse
    {
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SBODeductRequest
    {
        public string CompanyKey { get; set; }
        public string Username { get; set; }
        public decimal Amount { get; set; }
        public string TransferCode { get; set; }
        public string TransactionId { get; set; }
        public DateTime BetTime { get; set; }
        public SBOProductType ProductType { get; set; }
        public string GameRoundId { get; set; }
        public string GamePeriodId { get; set; }
        public string OrderDetail { get; set; }
        public string PlayerIp { get; set; }
        public string GameTypeName { get; set; }
        public int Gpid { get; set; }
        public int GameId { get; set; }
    }

    public class SBODeductResponse
    {
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public decimal BetAmount { get; set; }
    }

    public class PlaceBetSportDto_SBO
    {
        public string CompanyKey { get; set; }
        public string Username { get; set; }
        public decimal Amount { get; set; }
        public string TransferCode { get; set; }
        public string TransactionId { get; set; }
        public DateTime BetTime { get; set; }
        public int ProductType { get; set; }
        public string GameRoundId { get; set; }
        public string GamePeriodId { get; set; }
        public string OrderDetail { get; set; }
        public string PlayerIp { get; set; }
        public string GameTypeName { get; set; }
        public int Gpid { get; set; }
        public int GameId { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public GameType TpGameType { get; set; }
    }

    public class SBOSettleRequest
    {
        public string CompanyKey { get; set; }
        public string Username { get; set; }
        public string TransferCode { get; set; }
        public decimal WinLoss { get; set; }
        public SBOResultType ResultType { get; set; }
        public DateTime ResultTime { get; set; }
        public SBOProductType ProductType { get; set; }
        public int GameType { get; set; }
        public string GameResult { get; set; }
        public decimal CommissionStake { get; set; }
        public int Gpid { get; set; }
    }

    public class SBOSettleResponse
    {
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PayoutSportDto_SBO
    {
        public string Username { get; set; }
        public string TransferCode { get; set; }
        public decimal WinLoss { get; set; }
        public int ResultType { get; set; }
        public DateTime ResultTime { get; set; }
        public int ProductType { get; set; }
        public int GameType { get; set; }
        public string GameResult { get; set; }
        public decimal CommissionStake { get; set; }
        public int Gpid { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public GameType TpGameType { get; set; }
    }

    public class SBOGetBetStatusRequest
    {
        public string CompanyKey { get; set; }
        public string Username { get; set; }
        public string TransferCode { get; set; }
        public string TransactionId { get; set; }
        public SBOProductType ProductType { get; set; }
        public int Gpid { get; set; }
    }

    public class SBOGetBetStatusResponse
    {
        public string TransferCode { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public decimal WinLoss { get; set; }
        public decimal Stake { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SBOGetBetStatusData
    {
        public string TransferCode { get; set; }
        public string TransactionId { get; set; }
        public decimal Stake { get; set; }
        public decimal WinLoss { get; set; }
        public int Status { get; set; }
        public DateTime? ResultTime { get; set; }
        public int TicketStatus { get; set; }
    }

    public class SBOCancelRequest
    {
        public string CompanyKey { get; set; }
        public string Username { get; set; }
        public string TransferCode { get; set; }
        public SBOProductType ProductType { get; set; }
        public bool IsCancelAll { get; set; }
        public string TransactionId { get; set; }
        public int Gpid { get; set; }
    }
    public class SBOCancelResponse
    {
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CancelSportDto_SBO
    {
        public string Username { get; set; }
        public string TransferCode { get; set; }
        public decimal Amount { get; set; }
        public DateTime? ResultTime { get; set; }
        public int Status { get; set; }
        public GameType TpGameType { get; set; }
    }

    public class SBORollbackRequest
    {
        public string CompanyKey { get; set; }
        public string Username { get; set; }
        public string TransferCode { get; set; }
        public SBOProductType ProductType { get; set; }
        public int Gpid { get; set; }
    }
    public class SBORollbackResponse
    {
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class RollbackSportDto_SBO
    {
        public string Username { get; set; }
        public string TransferCode { get; set; }
        public decimal Amount { get; set; }
        public DateTime? ResultTime { get; set; }
        public int Status { get; set; }
        public GameType TpGameType { get; set; }
    }

    public class SBOLiveCoinTransactionRequest
    {
        public string CompanyKey { get; set; }
        public string Username { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionTime { get; set; }
        public string TransferCode { get; set; }
        public string TransactionId { get; set; }
        public SBOProductType ProductType { get; set; }
        public string Selection { get; set; }
    }

    public class SBOLiveCoinTransactionResponse
    {
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class LiveCoinTransactionDto_SBO
    {
        public string CompanyKey { get; set; }
        public string Username { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionTime { get; set; }
        public string TransferCode { get; set; }
        public string TransactionId { get; set; }
        public int ProductType { get; set; }
        public string Selection { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
    }

    public class SBOLiveCoinTransactionData
    {
        public string Username { get; set; }
        public decimal Amount { get; set; }
        public string TransferCode { get; set; }
        public string TransactionId { get; set; }
        public DateTime TransactionTime { get; set; }
        public int ProductType { get; set; }
        public string Selection { get; set; }
        public int Status { get; set; }
        public int TicketStatus { get; set; }
    }

    public class SBOConfig : TpSiteConfig
    {
        public string CompanyKey { get; set; }
        public string Agent { get; set; }
        public string ServerId { get; set; }
    }
}