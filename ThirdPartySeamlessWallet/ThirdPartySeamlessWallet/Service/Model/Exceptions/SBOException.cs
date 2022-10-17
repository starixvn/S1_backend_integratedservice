using System;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class SBOException : Exception
    {
        public SBOErrorCode SBOErrorCode { get; }
        public string ErrorMessage { get; set; }

        public SBOException()
        {
            
        }

        public SBOException(SBOErrorCode errorCode)
        {
            SBOErrorCode = errorCode;
            ErrorMessage = errorCode.GetEnumDescription();
        }

        public SBOException(SBOErrorCode errorCode, string message) : base(message)
        {
            SBOErrorCode = errorCode;
            ErrorMessage = $"{errorCode.GetEnumDescription()}:{message}";
        }
    }
}