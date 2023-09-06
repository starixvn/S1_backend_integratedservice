using System;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class NotSupportedGameTypeException : Exception
    {
        public NotSupportedGameTypeException()
        {
        }
        public NotSupportedGameTypeException(string message) : base(message)
        {
        }
    }
}
