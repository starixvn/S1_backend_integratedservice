using System;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class RestrictedCustomerException : Exception
    {
        public RestrictedCustomerException()
        {
        }
        public RestrictedCustomerException(string message) : base(message)
        {
        }
    }
}
