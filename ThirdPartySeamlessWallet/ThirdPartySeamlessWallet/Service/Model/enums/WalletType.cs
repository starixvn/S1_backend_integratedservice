namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum WalletType
    {
        TransferWallet = 0,
        SeamlessWallet = 1
    }

    public enum WalletKind
    {
        // BalanceWallet: SetBalance 
        BalanceWallet = 1,
        // BonusWallet:PlaceBet、PayoutV2
        BonusWallet = 2
    }
}
