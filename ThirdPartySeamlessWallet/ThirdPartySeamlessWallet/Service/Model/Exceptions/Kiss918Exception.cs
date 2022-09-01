using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions
{
    public class Kiss918Exception : Exception
    {
        public int playerID { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; set; }

        public Kiss918Exception()
        {
        }

        public Kiss918Exception(Kiss918ErrorCode errorCode, string message) : base(message)
        {
            ErrorCode = ((int)errorCode).ToString();
            ErrorMessage = message;
        }

        public Kiss918Exception(int playerID, Kiss918ErrorCode errorCode, string message) : base(message)
        {
            this.playerID = playerID;
            ErrorCode = ((int)errorCode).ToString();
            ErrorMessage = message;
        }
    }
}
