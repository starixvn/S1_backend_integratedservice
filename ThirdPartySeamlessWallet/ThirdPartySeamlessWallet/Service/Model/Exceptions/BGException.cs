using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions;

public class BGException : Exception
{
    public string serialnumber { get; set; }
    public BGErrorCode result { get; }
    public string desc { get; }

    public BGException(BGErrorCode errorCode, string message) : base(message)
    {
        result = errorCode;
        desc = message;
    }
}
