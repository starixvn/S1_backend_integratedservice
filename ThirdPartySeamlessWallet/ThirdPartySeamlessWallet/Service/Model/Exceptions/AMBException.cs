using System;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class AMBException : Exception
    {
        public AMBErrorCode ErrorCode { get; }
        public string ErrorMessage { get; set; }

        public AMBException(AMBErrorCode errorCode)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorCode.GetEnumDescription();
        }
    }
}
