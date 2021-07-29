using System.ComponentModel;

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
        Pai_Kang,
        Blackjack
    }

    public enum KMErrorCode
    {
        [Description("Invalid or expired token")]
        ExpiredToken = 10,
        [Description("Invalid token scope")]
        InvalidTokenScope = 19,
        [Description("User blocked")]
        UserBlocked = 20,
        [Description("Invalid Credential")]
        InvalidCredential = 30,
        [Description("Currency Mismatch")]
        CurrencyMismatch = 50,
        [Description("Insufficient funds to perform the operation")]
        InsufficientFunds = 100,
        [Description("Invalid arguments")]
        InvalidArguments = 300,
        [Description("Missing token")]
        MissingToken = 400,
        [Description("Incorrect format")]
        IncorrectFormat = 500,
        [Description("Transaction does not exist")]
        TransactionDoesNotExist = 600,
        [Description("Transaction already cancelled")]
        TransactionAlreadyCancelled = 610,
        [Description("Operation Failed Deterministically")]
        OperationFailedDeterministically = 800,
        [Description("System Error")]
        SystemError = 900,
        [Description("Configured Timeout Exceeded")]
        ConfiguredTimeout = 903
    }
}