using ThirdPartySeamlessWallet.Service.Model.enums;
using static ThirdPartySeamlessWallet.Controllers.APController;
using ThirdPartySeamlessWallet.Service.Model.SBSW;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class TFGException : Exception
    {
        public TFGException()
        {

        }
        public TFGException(int errorCode)
        {
            errorCode = errorCode;
        }
        public TFGException(int errorCode, string message) : base(message)
        {
            errorCode = errorCode;
            message = message;
        }
    }
}
