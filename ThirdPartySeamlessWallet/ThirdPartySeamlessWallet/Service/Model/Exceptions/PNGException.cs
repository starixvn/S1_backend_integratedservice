namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class PNGException : Exception
    {
        public string ErrorMessage { get; set; }

        public PNG_StatsuCodeEnum StatsuCode { get; set; }

        public PNGException(string message) : base(message)
        {
            ErrorMessage = message;
        }

        public PNGException(PNG_StatsuCodeEnum statusCode)
        {
            StatsuCode = statusCode;
            ErrorMessage = statusCode.ToString();
        }

        public PNGException(PNG_StatsuCodeEnum statusCode,string errorMessage)
        {
            StatsuCode = statusCode;
            ErrorMessage = errorMessage;
        }
    }
}
