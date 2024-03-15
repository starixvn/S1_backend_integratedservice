namespace ThirdPartyCore.Model.enums
{
    public enum TransferStatus
    {
        Success = 0,
        Fail = 1,
        Pending = 2, // 建單，但未扣款,
        PendingD = 3 // 建單，主錢包已扣款
    }
}
