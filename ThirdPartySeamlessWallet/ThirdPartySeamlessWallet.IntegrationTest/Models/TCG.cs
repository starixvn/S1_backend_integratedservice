using System;
using System.Collections.Generic;
using System.Text;

namespace ThirdPartySeamlessWallet.IntegrationTest.Models
{
    public class TCGRecord
    {
        public string username { get; set; }
        public string ref_no { get; set; }     
        public decimal amount { get; set; }  
        public string TransType { get; set; }
    }

    public class TransStatus
    {
        public const string Success = "SUCCESS";
        public const string NotFound = "NOT_FOUND";
        public const string Failed = "FAILED";
        public const string Pending = "PENDING";
        public const string Canceled = "CANCELLED";
        public const string Reserved = "RESERVED";
    }

    public class TCGWagerData
    {
        public decimal betAmount { get; set; }
        public string gameCode { get; set; } = "";
        public string betOrderNo { get; set; } = "";
        public string betTime { get; set; } = "";
        public string transTime { get; set; } = "";
        public string betContentId { get; set; } = "";
        public string playCode { get; set; } = "";
        public string orderNum { get; set; } = "";
        public string chase { get; set; } = "";
        public string numero { get; set; } = "";
        public string merchantCode { get; set; } = "";
        public string bettingContent { get; set; } = "";
        public int playId { get; set; }
        public string freezeTime { get; set; } = "";
        public int multiple { get; set; }
        public string remark { get; set; } = "";
        public string playName { get; set; } = "";
        public string gameGroupName { get; set; } = "";
        public decimal winAmount { get; set; }
        public decimal netPNL { get; set; }
        public string betStatus { get; set; } = "";
        public string settlementTime { get; set; } = "";
        public decimal actualBetAmount { get; set; }
        public decimal exceedWinAmount { get; set; }
        public string details { get; set; } = "";
        public string winningNumber { get; set; } = "";
        public string clientIp { get; set; } = "";
        public decimal playBonus { get; set; }
        public string username { get; set; } = "";
        public string productType { get; set; } = "";
        public string single { get; set; } = "";
        public int detailStatusId { get; set; }
    }
}
