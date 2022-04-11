using System;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.SBSW;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class SBSWException: Exception
    {
        public string ErrorCode { get; }

        public string ErrorMessage { get; set; }

        public ReturnCode ReturnCode { get; set; }

    public SBSWException(ReturnCode returnCode, string message): base(message)
        {
            ErrorCode = returnCode.CodeToString();
            ErrorMessage = message;
            ReturnCode = returnCode;
        }

        public SBSWException(ReturnCode returnCode) 
        {
            ErrorCode = returnCode.CodeToString();
            ErrorMessage = returnCode.ToString();
            ReturnCode = returnCode;
        }
    }
}