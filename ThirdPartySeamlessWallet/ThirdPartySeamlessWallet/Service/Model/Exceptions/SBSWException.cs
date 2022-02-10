using System;
using System.Drawing;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class SBSWException: Exception
    {
        public string ErrorCode { get; }

        public string ErrorMessage { get; set; }

        public SBSWException(SBSWReturnCode returnCode, string message): base(message)
        {
            ErrorCode = returnCode.CodeToString();
            ErrorMessage = message;
        }
    }
}