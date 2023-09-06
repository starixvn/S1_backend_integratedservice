namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class TCGException : Exception
    {
        public int ErrorCode { get; }
        public string ErrorMessage { get; set; }

        public TCGException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
            ErrorMessage = message;
        }

    }
}