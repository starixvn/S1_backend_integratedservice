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
}
