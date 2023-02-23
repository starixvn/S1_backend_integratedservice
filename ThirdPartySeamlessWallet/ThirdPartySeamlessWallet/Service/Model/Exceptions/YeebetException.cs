using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model.Exceptions;

public class YeebetException : Exception
{
    public string serialnumber { get; set; }
    public YeebetErrorCode result { get; }
    public string desc { get; }

    public YeebetException()
    {
    }

    public YeebetException(YeebetErrorCode errorCode, string message) : base(message)
    {
        result = errorCode;
        desc = message;
    }
}
