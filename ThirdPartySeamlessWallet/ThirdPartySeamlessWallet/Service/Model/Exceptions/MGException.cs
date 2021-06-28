using System;
using System.Drawing;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class MGException: Exception
    {
        public MGErrorCode MGErrorCode { get; }
        public string ErrorMessage { get; set; }

        public MGException()
        {
            
        }

        public MGException(MGErrorCode errorCode, string message): base(message)
        {
            MGErrorCode = errorCode;
            ErrorMessage = message;
        }

        public MGException(MGErrorCode errorCode)
        {
            MGErrorCode = errorCode;
        }
    }
}