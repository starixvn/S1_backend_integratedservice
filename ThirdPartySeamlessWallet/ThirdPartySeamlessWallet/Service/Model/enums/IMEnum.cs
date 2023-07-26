using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum IMResultCode
    {
        Success = 0,
        PlayerDoesNotExist = 504,
        NotEnoughBalance=510,
        PlayerIsInactive = 542,
        InvalidArgument = 612,
        Exception = 999
    }
    
}