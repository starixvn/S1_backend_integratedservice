namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum SettleFlag
    {
        Running = 0, /* Status means StartGame(Bet) Or Running */
        Settled = 1, /* When Worker done */
        EndGame = 2 /* For Worker Condiction To Reach */
    }

}
