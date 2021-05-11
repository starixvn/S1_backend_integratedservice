using System;
using Org.BouncyCastle.Asn1.Esf;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class KmException : Exception
    {
        public KmException()
        {
        }

        public KmException(string message) : base(message)
        {
            
        }
    }
}