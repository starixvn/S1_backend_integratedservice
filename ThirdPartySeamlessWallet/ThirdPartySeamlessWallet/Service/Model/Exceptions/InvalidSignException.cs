using System;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class InvalidSignException : Exception
    {
        public InvalidSignException()
        {
        }
        public InvalidSignException(string message) : base(message)
        {
        }
    }
}
