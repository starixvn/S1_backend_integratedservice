using System;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class RewardRNG_Joker
    {
        public int SQ { get; set; }
        public string appid { get; set; }
        public string hash { get; set; }
        public string id { get; set; }
        public string tournamentid { get; set; }
        public decimal amount { get; set; }
        public string username { get; set; }
        public string extendedinfo { get; set; }
        public long timestamp { get; set; }
        public RewardRNG_JokerTypeEnum FromAPIType { get; set; }
        public BettransType TicketType { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }


    #region Global Model

    public class JokerRequestModel
    {
        public string appid { get; set; }
        public string hash { get; set; }
        /// <summary>
        /// Unique by member
        /// </summary>
        public string id { get; set; }
        public string username { get; set; }
        public long timestamp { get; set; }
        /// <summary>
        /// Do not use this property in Hash.IsJSON string. 
        /// </summary>
        /// <remarks>
        /// Client can deserialize this value to object if need to consume.
        /// For example: JSON.parse(value)
        /// </remarks>
        public string extendedinfo { get; set; }
    }

    public class JokerResponse
    {
        public JokerErrorCode Status { get; set; }
        public string Message { get; set; }
        public decimal Balance { get; set; }
    }

    #endregion

    public class JoinAndWinRequest : JokerRequestModel
    {
        /// <summary>
        /// (+)
        /// </summary>
        public decimal amount { get; set; }
        public string tournamentid { get; set; }
    }

    public class CancelRequest : JokerRequestModel
    {
        public string jointournamentid { get; set; }
    }

}
