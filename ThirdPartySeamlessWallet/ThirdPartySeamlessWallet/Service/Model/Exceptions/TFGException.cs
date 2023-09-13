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
