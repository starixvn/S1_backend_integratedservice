using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_Kiss918
    {
        public int CustId { get; set; }
        public int playerID { get; set; }
        public string gameID { get; set; } = "";
        public string playSessionID { get; set; } = "";
        public string referenceID { get; set; } = "";
        public decimal amount { get; set; } = 0;
        public int jackpotModule { get; set; } = 0;
        public decimal jackpotContributionAmt { get; set; } = 0;
        public string resultUrl { get; set; } = "";
        public string roundDetails { get; set; } = "";
        public string platform { get; set; } = "";
        public int type { get; set; } = 0;
        public BettransRng_Kiss918TypeEnum FromAPIType { get; set; }
        public BettransType TicketType { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public string NewRoundId { get; set; } = "";
    }

    public class SettleData_Kiss918
    {
        public string uuid { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinLostAmount { get; set; }
        public string Account { get; set; }
        public long SettleTime { get; set; }
    }

    #region Request Model

    public class AuthenticateRequest_Kiss918
    {
        public string userName { get; set; }
        public string password { get; set; }
    }

    public class BetRequest_Kiss918
    {
        public int playerID { get; set; }
        public string gameID { get; set; }
        public string playSessionID { get; set; }
        public string referenceID { get; set; }
        public decimal betAmount { get; set; }
        public string roundDetails { get; set; }
        public string platform { get; set; }
    }

    public class BetResultRequest_Kiss918
    {
        public int playerID { get; set; }
        public string gameID { get; set; }
        public string playSessionID { get; set; }
        public string referenceID { get; set; }
        public decimal winAmount { get; set; }
        public int jackpotModule { get; set; }
        public decimal jackpotContributionAmt { get; set; }
        public string resultUrl { get; set; }
        public string roundDetails { get; set; }
        public string platform { get; set; }
    }

    public class RefundRequest_Kiss918
    {
        public int playerID { get; set; }
        public string gameID { get; set; }
        public string playSessionID { get; set; }
        public string referenceID { get; set; }
        public decimal betAmount { get; set; }
        public string platform { get; set; }
    }


    public class JackpotWinRequest_Kiss918 : BetResultRequest_Kiss918
    {
    }

    #endregion

    #region Response Model

    /// <summary>
    /// Common Response Model
    /// </summary>
    public class ResponseModel_Kiss918
    {
        public string error { get; set; }
        public string description { get; set; }
    }

    /// <summary>
    /// 玩家身份驗證回覆
    /// </summary>
    public class AuthenticateResponse_Kiss918 : ResponseModel_Kiss918
    {
        public int playerID { get; set; }
        public decimal balance { get; set; }

        public AuthenticateResponse_Kiss918(Kiss918ErrorCode errorCode, string message = "", int playerID = 0, decimal balance = 0)
        {
            this.playerID = playerID;
            this.balance = balance;
            this.error = ((int)errorCode).ToString();
            this.description = message;
        }
    }

    /// <summary>
    /// 獲取餘額信息回覆
    /// </summary>
    public class GetBalanceResponse_Kiss918 : ResponseModel_Kiss918
    {
        public decimal balance { get; set; }
        public GetBalanceResponse_Kiss918(Kiss918ErrorCode errorCode, string message = "", decimal balance = 0)
        {
            this.balance = balance;
            this.error = ((int)errorCode).ToString();
            this.description = message;
        }
    }

    /// <summary>
    /// 操作回覆(操作類型參考remark)
    /// </summary>
    /// <remarks>
    /// Bet
    /// BetResult
    /// Refund
    /// FundRequest
    /// FundReturn
    /// JackpotWin
    /// </remarks>
    public class OperationResponse_Kiss918 : ResponseModel_Kiss918
    {
        public string transaction { get; set; }
        public decimal balance { get; set; }

        public OperationResponse_Kiss918(Kiss918ErrorCode errorCode, string message = "", string transaction = "", decimal balance = 0)
        {
            this.transaction = transaction;
            this.balance = balance;
            this.error = ((int)errorCode).ToString();
            this.description = message;
        }
    }

    /// <summary>
    /// FundBetResult回覆
    /// </summary>
    public class FundBetResultResponse_Kiss918 : ResponseModel_Kiss918
    {
        public string transaction { get; set; }
    }

    #endregion
}
