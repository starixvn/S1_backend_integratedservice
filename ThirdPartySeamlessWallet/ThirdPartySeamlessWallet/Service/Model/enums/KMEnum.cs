namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    /// <summary>
    /// 交易类型
    /// </summary>
    public enum KMTxType
    {
        /// <summary>
        /// 投注
        /// </summary>
        PlaceBet = 500,
        /// <summary>
        /// 贏錢
        /// </summary>
        WinBet = 510,
        /// <summary>
        /// 贏彩金
        /// </summary>
        WinJackpot = 511,
        /// <summary>
        /// 輸錢
        /// </summary>
        LoseBet = 520,
        /// <summary>
        /// 免費投注
        /// </summary>
        FreeBet = 530,
        /// <summary>
        /// 平手
        /// </summary>
        TieBet = 540,
        /// <summary>
        /// 取消交易
        /// </summary>
        CancelTransaction = 560,
        /// <summary>
        /// 結束局
        /// </summary>
        EndRound = 590,
        
        //There are 3 games will use extra transaction type (610.600.611)
        //1. Gao Gae
        //2. Kingmaker Pok Deng
        //3. Pai Kang
        
        /// <summary>
        /// 電子錢包加錢
        /// </summary>
        FundIn = 600,
        /// <summary>
        /// 電子錢包扣錢
        /// </summary>
        FundOut = 610,
        /// <summary>
        /// 取消電子錢包扣錢
        /// </summary>
        CancelFundOut = 611
    }

    public enum KMGameCode
    {
        Gao_Gae,
        Kingmaker_Pok_Deng,
        Pai_Kang
    }

    public enum KMErrorCode
    {
        ExpiredToken = 10,
        InvalidTokenScope = 19,
        UserBlocked = 20,
        InvalidCredential = 30,
        CurrencyMismatch = 50,
        InsufficientFunds = 100,
        InvalidArguments = 300,
        MissingToken = 400,
        IncorrectFormat = 500,
        TransactionDoesNotExist = 600,
        TransactionAlreadyCancelled = 610,
        OperationFailedDeterministically = 800,
        SystemError = 900,
        ConfiguredTimeout = 903
    }
}