using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum SabaOnlineMemberStatus
    {
        [Description("Member Is Online")]
        MemberIsOnline = 0,
        [Description("Invalid Secret Key or Ip Address Not Allow")]
        InvalidSecretKeyOrIpAddressNotAllow = 1,
        [Description("Vendor MemberId NotFound")]
        VendorMemberIdNotFound = 2,
        [Description("Member Is Offline")]
        MemberIsOffline = 3
    }

}
