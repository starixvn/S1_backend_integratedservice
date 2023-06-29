using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class NextspinException : Exception
    {
        public string MerchantCode { get; }
        public NextspinErrorCode NextspinErrorCode { get; }
        public string ErrorMessage { get; }

        public NextspinException() {}

        public NextspinException(NextspinErrorCode errorCode)
        {
            NextspinErrorCode = errorCode;
            ErrorMessage = errorCode.GetEnumDescription();
        }

        public NextspinException(NextspinErrorCode errorCode, string message) : base(message)
        {
            NextspinErrorCode = errorCode;
            ErrorMessage = $"{errorCode.GetEnumDescription()}:{message}";
        }
    }
}
