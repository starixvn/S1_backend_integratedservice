using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class NextspinException : Exception
    {
        public NextspinErrorCode SBOErrorCode { get; }
        public string ErrorMessage { get; set; }

        public NextspinException() {}

        public NextspinException(NextspinErrorCode errorCode)
        {
            SBOErrorCode = errorCode;
            ErrorMessage = errorCode.GetEnumDescription();
        }

        public NextspinException(NextspinErrorCode errorCode, string message) : base(message)
        {
            SBOErrorCode = errorCode;
            ErrorMessage = $"{errorCode.GetEnumDescription()}:{message}";
        }
    }
}
