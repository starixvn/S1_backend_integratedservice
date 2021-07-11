using System;
using System.Drawing;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class TCGException: Exception
    {
        public string ErrorCode { get; }
        public string ErrorMessage { get; set; }

        public TCGException(string errorCode, string message): base(message)
        {
            ErrorCode = errorCode;
            ErrorMessage = message;
        }

    }
}