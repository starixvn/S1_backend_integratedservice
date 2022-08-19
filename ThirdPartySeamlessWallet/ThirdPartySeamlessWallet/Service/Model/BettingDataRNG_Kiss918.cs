using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_Kiss918
    {
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
        public string token { get; set; }
        public string userName { get; set; }
        public string password { get; set; }
    }

    public class GetBalanceRequest_Kiss918
    {
        public string token { get; set; }
        public int playerID { get; set; }
    }

    public class BetRequest_Kiss918
    {
        public string token { get; set; }
        public int playerID { get; set; }
    }

    public class BetResultRequest_Kiss918
    {
        public string token { get; set; }
        public int playerID { get; set; }
    }

    public class RefundRequest_Kiss918
    {
        public string token { get; set; }
        public int playerID { get; set; }
    }

    public class FundRequestRequest_Kiss918
    {
        public string token { get; set; }
        public int playerID { get; set; }
    }

    public class FundReturnRequest_Kiss918
    {
        public string token { get; set; }
        public int playerID { get; set; }
    }

    public class FundBetResultRequest_Kiss918
    {
        public string token { get; set; }
        public int playerID { get; set; }
    }

    public class JackpotWinRequest_Kiss918
    {
        public string token { get; set; }
        public int playerID { get; set; }
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
    }

    /// <summary>
    /// 獲取餘額信息回覆
    /// </summary>
    public class GetBalanceResponse_Kiss918 : ResponseModel_Kiss918
    {
        public decimal balance { get; set; }
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
