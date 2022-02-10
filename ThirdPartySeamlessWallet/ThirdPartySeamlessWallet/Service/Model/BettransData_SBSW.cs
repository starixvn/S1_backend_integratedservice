using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public enum SBSWReturnCode
    {
        Success = 0,
        UnknownError = -1,
        OperatorNotFound = 301,
        UserNotFound = 203,
        UnSupportedCurrency = 302
    }

    public class SBSWConfig
    {
        public string ApiUrl { get; set; }
        public string VendorId { get; set; }

        public SBSWConfig(Dictionary<string, string> inputs, string merchant)
        {
            try
            {
                ApiUrl = inputs.GetValue($"ApiUrl_{merchant}");
                VendorId = inputs.GetValue($"VendorId_{merchant}");
            }
            catch (Exception ex)
            {
                throw new SBSWException(SBSWReturnCode.OperatorNotFound, ex.Message);
            }
        }
    }

    public class SBSWReturnObj
    {
        public string status { get; set; }

        public string msg { get; set; }

        public SBSWReturnObj(SBSWReturnCode returnCode) 
        {
            status = returnCode.CodeToString();
            msg = returnCode.ToString();
        }
        public SBSWReturnObj(SBSWException exception)
        {
            status = exception.ErrorCode;
            msg = exception.ErrorMessage;
        }
    }

    public class SBSWRequest
    {
        public string key { get; set; }

        public dynamic message { get; set; }
    }


    public class SBSWBalanceObj : SBSWReturnObj
    {
        public string userId { get; set; }
        public decimal balance { get; set; }
        public string balanceTs { get; set; }

        public SBSWBalanceObj(SBSWReturnCode returnCode) : base(returnCode)
        {

        }
    }




}
