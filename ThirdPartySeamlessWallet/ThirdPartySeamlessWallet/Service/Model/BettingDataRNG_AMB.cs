using System.ComponentModel.DataAnnotations.Schema;

using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    [Table("BettingDataRNG_AMB")]
    public class BettingDataRNG_AMB
    {
        public int SQ { get; set; }
        public string username { get; set; }
        public string agentId { get; set; }
        public string game { get; set; }
        public string gameId { get; set; }
        public string roundId { get; set; }
        public decimal bet { get; set; }
        public decimal amount { get; set; }
        public decimal winlose { get; set; }
        public decimal turnover { get; set; }
        public decimal roundAmount { get; set; }
        public string currency { get; set; }
        public string transactionId { get; set; }
        public DateTime timestamp { get; set; }
        public bool featureBuy { get; set; }
        public bool isEndRound { get; set; }
        public string type { get; set; }
        public string tranId { get; set; }
        public BettransRng_AMBTypeEnum FromAPIType { get; set; }
        public BettransType TicketType { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    public class SettleDataRNG_AMB : SettleData
    {
        public string Categories { get; set; }
        public decimal Bet { get; set; }
        public decimal WinLost { get; set; }
        public decimal Turnover { get; set; }
    }

    #region Global Response Model

    public class AMBSlotStatusModel
    {
        /// <summary>
        /// AMB ErrorCode(Reference AMBEnum)
        /// </summary>
        public AMBErrorCode code { get; set; }
        /// <summary>
        /// Error message(Supplementary Instructions)
        /// </summary>
        public string message { get; set; }
    }

    public class AMBSlotDataModelForBalance
    {
        /// <summary>
        /// User wallet balance (THB)
        /// </summary>
        public decimal balance { get; set; }
    }

    public class AMBSlotDataModelForBetPayoutCancel
    {
        /// <summary>
        /// Bet:Username that deduct balance to bet
        /// Payout:Username that was payout
        /// Cancel:Username that deposit to
        /// </summary>
        public string username { get; set; }

        /// <summary>
        /// Contain latest balance and last update time
        /// </summary>
        public AMBSlotWalletModel wallet { get; set; }

        /// <summary>
        /// Contain before and after transaction balance
        /// </summary>
        public AMBSlotBalanceModel balance { get; set; }

        /// <summary>
        /// Reference id that refer to this transaction
        /// </summary>
        public string refId { get; set; }

    }

    /// <summary>
    /// Contain latest balance and last update time
    /// </summary>
    public class AMBSlotWalletModel
    {
        public decimal balance { get; set; }

        public string lastUpdate { get; set; }
    }

    /// <summary>
    /// Contain before and after transaction balance
    /// </summary>
    public class AMBSlotBalanceModel
    {
        public decimal before { get; set; }

        public decimal after { get; set; }
    }

    #endregion

    #region Global Request Model

    public class AMBSlotRequestModel
    {
        /// <summary>
        /// Player username
        /// </summary>
        public string username { get; set; }
        /// <summary>
        /// Agent Id that AMBSlot has provided
        /// </summary>
        public string agentId { get; set; }
        /// <summary>
        /// Agent Id that AMBSlot has provided
        /// </summary>
        public string agent { get; set; }
        /// <summary>
        /// Game name that user played
        /// </summary>
        public string game { get; set; }
        /// <summary>
        /// Game code that user played
        /// </summary>
        public string gameId { get; set; }
        /// <summary>
        /// Round Id for each bet transaction
        /// </summary>
        public string roundId { get; set; }
        /// <summary>
        /// Currency of amount
        /// </summary>
        public string currency { get; set; }
        /// <summary>
        /// reference id that refer to this transaction (unique)
        /// </summary>
        public string transactionId { get; set; }
        /// <summary>
        /// Timestamp of this transaction
        /// </summary>
        public string timestamp { get; set; }

        //The following don't refer from TP Doc. but exit when testing in tp UAT.
        public decimal? roundAmount { get; set; } //Tp maybe send "", so can't define to decimal
        public string type { get; set; }
        public string tranId { get; set; }
    }

    #endregion

    public class ANBSlotGetBalanceRequest
    {
        /// <summary>
        /// Member username
        /// </summary>
        public string username { get; set; }
        /// <summary>
        /// Agent Id that AMBSlot has provided
        /// </summary>
        public string agentId { get; set; }
        /// <summary>
        /// Agent Id that AMBSlot has provided
        /// </summary>
        public string agent { get; set; }
    }

    public class AMBSlotGetBalanceResponse
    {
        public AMBSlotStatusModel status { get; set; }

        public AMBSlotDataModelForBalance data { get; set; }
    }

    public class AMBSlotBetRequest : AMBSlotRequestModel
    {
        /// <summary>
        /// Amount number that user bet (+)
        /// </summary>
        public decimal amount { get; set; }
        /// <summary>
        /// Feature buy freespin
        /// </summary>
        public bool featureBuy { get; set; }
        /// <summary>
        /// If true. It’s last payout of current round
        /// </summary>
        public bool isEndRound { get; set; }
    }

    public class AMBSlotBetResponse
    {
        public AMBSlotStatusModel status { get; set; }

        public AMBSlotDataModelForBetPayoutCancel data { get; set; }
    }

    public class AMBSlotPayoutRequest : AMBSlotRequestModel
    {
        /// <summary>
        /// Payout Amount number that received 
        /// </summary>
        public decimal amount { get; set; }
        /// <summary>
        /// Amount that user bet
        /// </summary>
        public decimal turnover { get; set; }
        /// <summary>
        /// If true. It’s last payout of current round
        /// </summary>
        public bool isEndRound { get; set; }
        /// <summary>
        /// Feature buy freespin
        /// </summary>
        public bool featureBuy { get; set; }
    }

    public class AMBSlotPayoutResponse
    {
        public AMBSlotStatusModel status { get; set; }

        public AMBSlotDataModelForBetPayoutCancel data { get; set; }
    }

    public class AMBSlotCancelRequest : AMBSlotRequestModel
    {
        /// <summary>
        /// Amount number that user bet (+)
        /// </summary>
        public decimal amount { get; set; }
    }

    public class AMBSlotCancelResponse
    {
        public AMBSlotStatusModel status { get; set; }

        public AMBSlotDataModelForBetPayoutCancel data { get; set; }
    }

    public class AMBSlotSettleRequest
    {
        public decimal roundAmount { get; set; }
        public string type { get; set; }
        public string tranId { get; set; }

        /// <summary>
        /// Member username
        /// </summary>
        public string username { get; set; }
        /// <summary>
        /// Agent id that AMBSlot has provided
        /// </summary>
        public string agent { get; set; }
        public string agentId { get; set; }
        /// <summary>
        /// Game name that user played
        /// </summary>
        public string game { get; set; }
        /// <summary>
        /// Game code that user played
        /// </summary>
        public string gameId { get; set; }
        /// <summary>
        /// Round id for each bet transaction
        /// </summary>
        public string roundId { get; set; }
        /// <summary>
        /// Payout Amount number that received
        /// </summary>
        public decimal amount { get; set; }
        /// <summary>
        /// Amount number that user bet (+) 
        /// </summary>
        public decimal bet { get; set; }

        /// <summary>
        /// Amount that player win
        /// </summary>
        public decimal winlose { get; set; }
        /// <summary>
        /// Currency of amount
        /// </summary>
        public string currency { get; set; }
        /// <summary>
        /// Reference id that refer to this transaction (unique)
        /// </summary>
        public string transactionId { get; set; }
        /// <summary>
        /// Timestamp of this transaction
        /// </summary>
        public string timestamp { get; set; }
        /// <summary>
        /// Amount that user bet
        /// </summary>
        public decimal turnover { get; set; }
        /// <summary>
        /// If true. It’s last payout of current round
        /// </summary>
        public bool isEndRound { get; set; }
        /// <summary>
        /// Feature buy freespin
        /// </summary>
        public bool featureBuy { get; set; }
    }

    public class AMBSlotData
    {
        public string username { get; set; }
        public AMBSlotWalletModel wallet { get; set; }
        public AMBSlotBalanceModel balance { get; set; }
        public string refId { get; set; }
    }

    public class AMBSlotSettleResponse
    {
        public AMBSlotStatusModel status { get; set; }
        public AMBSlotData data { get; set; }
    }
}
