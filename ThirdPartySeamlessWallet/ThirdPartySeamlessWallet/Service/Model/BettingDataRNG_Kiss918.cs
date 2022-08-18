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

    #endregion

    #region Response Model

    public class ResponseModel_Kiss918
    {
        public string error { get; set; }
        public string description { get; set; }
    }

    public class AuthenticateResponse_Kiss918 : ResponseModel_Kiss918
    {
        public int playerID { get; set; }
        public decimal balance { get; set; }
    }

    public class GetBalanceResponse_Kiss918 : ResponseModel_Kiss918
    {
        public decimal balance { get; set; }
    }

    public class OperationResponse_Kiss918 : ResponseModel_Kiss918
    {
        public string transaction { get; set; }
        public decimal balance { get; set; }
    }

    public class FundBetResultResponse_Kiss918 : ResponseModel_Kiss918
    {
        public string transaction { get; set; }
    }

    #endregion
}
