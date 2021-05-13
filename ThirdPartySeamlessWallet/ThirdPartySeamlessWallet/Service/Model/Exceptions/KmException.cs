using System;
using Org.BouncyCastle.Asn1.Esf;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class KmException : Exception
    {
        public KMErrorCode KMErrorCode { get; }
        public string ErrorMessage { get; set; }
        public KmException()
        {
        }

        public KmException(KMErrorCode errorCode, string message) : base(message)
        {
            KMErrorCode = errorCode;
            ErrorMessage = $"{errorCode.GetEnumDescription()}:{message}";
        }

        public KmException(KMErrorCode errorCode)
        {
            KMErrorCode = errorCode;
            ErrorMessage = errorCode.GetEnumDescription();
        }
    }
}