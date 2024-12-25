using System.Xml.Serialization;

namespace ThirdPartySeamlessWallet.Service.Model
{

    #region Authenticate Model

    [XmlRoot(ElementName = "authenticate")]
    public class PNG_AuthenticateRequest
    {
        /// <summary>
        /// 运营商提供的会话令牌账户系统。
        /// </summary>
        [XmlElement(ElementName = "username")]
        public string AuthToken { get; set; }

        /// <summary>
        /// 为产品分配标识符。
        /// </summary>
        [XmlElement(ElementName = "productId")]
        public int ProductId { get; set; }

        /// <summary>
        /// 应在每次调用时验证的私人访问令牌。
        /// </summary>
        [XmlElement(ElementName = "accessToken")]
        public string AccessToken { get; set; }

        /// <summary>
        /// 遊戲標識符。請參閱遊戲詳情表，與登入遊戲的 GID 並不相同
        /// </summary>
        [XmlElement(ElementName = "gameId")]
        public string GameId { get; set; }

        /// <summary>
        /// 启动游戏所在的通道。
        /// 1 = 桌面
        /// 2 = 移动
        /// 3 = 迷你游戏
        /// 5 = 嵌入本地应用程序
        /// </summary>
        [XmlElement(ElementName = "channel")]
        public int Channel { get; set; }
    }

    [XmlRoot(ElementName = "authenticate", Namespace = "")]
    public class PNG_AuthenticateResponse : PNG_BaseResponse
    {
        #region Constructor 

        public PNG_AuthenticateResponse() { }

        public PNG_AuthenticateResponse(int statusCode, string statusMessage)
        {
            StatusCode = statusCode;
            StatusMessage = statusMessage;
        }
        #endregion

        /// <summary>
        /// 唯一的客户 ID。对于同一用户，此 ID 必须始终相同。区分大小写。
        /// </summary>
        [XmlElement(ElementName = "externalId")]
        public string TpUserName { get; set; }

        /// <summary>
        /// 玩家货币。请参阅游戏、货币、国家/地区和语言附录。
        /// </summary>
        [XmlElement(ElementName = "userCurrency")]
        public string UserCurrency { get; set; }

        /// <summary>
        /// 玩家国家/地区代码。请参阅游戏、货币、国家/地区和语言附录。特定司法辖区要求
        /// </summary>
        [XmlElement(ElementName = "country")]
        public string Country { get; set; }

        /// <summary>
        /// 用户的出生日期
        /// </summary>
        [XmlElement(ElementName = "birthdate")]
        public DateTime Birthdate { get; set; }

        /// <summary>
        /// 用户最初在原始系统中注册的日期。
        /// </summary>
        [XmlElement(ElementName = "registration")]
        public DateTime Registration { get; set; }

        /// <summary>
        /// 玩家的真钱余额
        /// </summary>
        [XmlElement(ElementName = "real")]
        public decimal Real { get; set; }
    }

    #endregion

    #region Balance Model

    [XmlRoot(ElementName = "balance")]
    public class PNG_BalanceRequest
    {
        /// <summary>
        /// 唯一的客户 ID。对于同一用户，此 ID 必须始终相同。区分大小写。
        /// </summary>
        [XmlElement(ElementName = "externalId")]
        public string TpUserName { get; set; }

        /// <summary>
        /// 分配给产品的标识符。
        /// </summary>
        [XmlElement(ElementName = "productId")]
        public int ProductId { get; set; }

        /// <summary>
        /// 返回余额必须使用的货币代码。请参阅游戏、货币、国家/地区和语言附录。
        /// </summary>
        [XmlElement(ElementName = "currency")]
        public string Currency { get; set; }

        /// <summary>
        /// 遊戲標識符。請參閱遊戲詳情表，與登入遊戲的 GID 並不相同
        /// </summary>
        [XmlElement(ElementName = "gameId")]
        public int GameId { get; set; }

        /// <summary>
        /// 应始终验证的私人访问令牌
        /// </summary>
        [XmlElement(ElementName = "accessToken")]
        public string AccessToken { get; set; }
    }

    [XmlRoot(ElementName = "balance")]
    public class PNG_BalanceResponse : PNG_BaseResponse
    {
        #region Constructor
        public PNG_BalanceResponse() { }

        public PNG_BalanceResponse(int statusCode, string statusMessage)
        {
            StatusCode = statusCode;
            StatusMessage = statusMessage;
        }

        #endregion

        [XmlElement(ElementName = "real")]
        public decimal Real { get; set; }
    }


    #endregion

    #region Reserve Model

    #region PNG_ReserveRequest Sub Model

    [XmlRoot(ElementName = "jackpot")]
    public class PNGJackpot
    {
        /// <summary>
        /// 累积大奖 ID。
        /// </summary>
        [XmlElement(ElementName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// 累积大奖贡献金额，此欄位只有在 Reserve/CancelReserve(投注、取消投注)時，才會出現的欄位
        /// </summary>
        [XmlElement(ElementName = "loss")]
        public decimal Loss { get; set; }

        /// <summary>
        /// 贏得的累積大獎金額，此欄位只有在 Release(派彩)時，才會出現的欄位
        /// </summary>
        [XmlElement(ElementName = "win")]
        public decimal Win { get; set; }
    }

    [XmlRoot(ElementName = "jackpots")]
    public class PNGJackpots
    {

        [XmlElement(ElementName = "jackpot")]
        public List<PNGJackpot> PNGJackpot { get; set; }
    }

    #endregion

    [XmlRoot(ElementName = "reserve")]
    public class PNG_ReserveRequest
    {
        /// <summary>
        /// 唯一的客户 ID。对于同一用 户 ， 此 ID 必 须 始 终相同。
        /// </summary>
        [XmlElement(ElementName = "externalId")]
        public string TpUserName { get; set; }

        /// <summary>
        /// 分配给产品的标识符。
        /// </summary>
        [XmlElement(ElementName = "productId")]
        public string ProductId { get; set; }

        /// <summary>
        /// 唯一的交易 ID。
        /// </summary>
        [XmlElement(ElementName = "transactionId")]
        public string TransactionId { get; set; }

        /// <summary>
        /// 投注中使用的"真钱"金额。如果是免费游戏回合，则为 0。
        /// </summary>
        [XmlElement(ElementName = "real")]
        public decimal Real { get; set; }

        /// <summary>
        /// 货币代码。请参阅游戏、货币、国家/地区和语言附录。
        /// </summary>
        [XmlElement(ElementName = "currency")]
        public string Currency { get; set; }

        /// <summary>
        /// 遊戲標識符。請參閱遊戲詳情表，與登入遊戲的 GID 並不相同
        /// </summary>
        [XmlElement(ElementName = "gameId")]
        public string GameId { get; set; }

        /// <summary>
        /// 游戏会话标识符由 Play’n GO 生成。
        /// </summary>
        [XmlElement(ElementName = "gameSessionId")]
        public string GameSessionId { get; set; }

        /// <summary>
        /// 可选如身份验证请求中所指定。
        /// </summary>
        [XmlElement(ElementName = "contextId")]
        public string ContextId { get; set; }

        /// <summary>
        /// 应始终验证的私人访问令牌。
        /// </summary>
        [XmlElement(ElementName = "accessToken")]
        public string AccessToken { get; set; }

        /// <summary>
        /// 应始终验证的私人访问令牌。
        /// </summary>
        [XmlElement(ElementName = "roundId")]
        public long RoundId { get; set; }

        /// <summary>
        /// 可选如运营商帐户系统在身份验证响应中指定
        /// </summary>
        [XmlElement(ElementName = "externalGameSessionId")]
        public string ExternalGameSessionId { get; set; }

        /// <summary>
        /// 启动游戏所在的通道。
        /// </summary>
        [XmlElement(ElementName = "channel")]
        public int Channel { get; set; }

        [XmlElement(ElementName = "freegameExternalId")]
        public string FreeGameExternalId { get; set; }

        /// <summary>
        /// 注额的实际值。同时用于正常投注和免费游戏。对于免费游戏，是为免费游戏配置的注额。对于正常投注，这将与"real"中的相同。
        /// </summary>
        [XmlElement(ElementName = "actualValue")]
        public decimal ActualAmount { get; set; }

        /// <summary>
        /// 标记包含一个或多个累积大奖标记。
        /// </summary>
        [XmlElement(ElementName = "jackpots")]
        public PNGJackpots PngJackpots { get; set; }
    }

    [XmlRoot(ElementName = "reserve")]
    public class PNG_ReserveResponse : PNG_BaseResponse
    {
        #region Constructor

        public PNG_ReserveResponse() { }

        public PNG_ReserveResponse(int statusCode, string statusMessage)
        {
            StatusCode = statusCode;
            StatusMessage = statusMessage;
        }

        #endregion

        /// <summary>
        /// 可选此交易的运营商帐户系统唯一 ID。
        /// </summary>
        [XmlElement(ElementName = "externalTransactionId")]
        public string ExternalTransactionId { get; set; }

        /// <summary>
        /// 交易后客户的真钱余额。
        /// </summary>
        [XmlElement(ElementName = "real")]
        public decimal AfterAmount { get; set; }
    }

    #endregion

    #region CancelReserve Model

    [XmlRoot(ElementName = "cancelReserve")]
    public class PNG_CancelReserveRequest
    {
        /// <summary>
        /// 唯一的客户 ID。对于同一用 户 ， 此 ID 必 须 始 终相同。
        /// </summary>
        [XmlElement(ElementName = "externalId")]
        public string TpUserName { get; set; }

        /// <summary>
        /// 分配给产品的标识符。
        /// </summary>
        [XmlElement(ElementName = "productId")]
        public string ProductId { get; set; }

        /// <summary>
        /// 唯一的交易 ID。
        /// </summary>
        [XmlElement(ElementName = "transactionId")]
        public string TransactionId { get; set; }

        /// <summary>
        /// 投注中使用的"真钱"金额。如果是免费游戏回合，则为 0。
        /// </summary>
        [XmlElement(ElementName = "real")]
        public decimal Real { get; set; }

        /// <summary>
        /// 货币代码。请参阅游戏、货币、国家/地区和语言附录。
        /// </summary>
        [XmlElement(ElementName = "currency")]
        public string Currency { get; set; }

        /// <summary>
        /// 遊戲標識符。請參閱遊戲詳情表，與登入遊戲的 GID 並不相同
        /// </summary>
        [XmlElement(ElementName = "gameId")]
        public string GameId { get; set; }

        /// <summary>
        /// 游戏会话标识符由 Play’n GO 生成。
        /// </summary>
        [XmlElement(ElementName = "gameSessionId")]
        public string GameSessionId { get; set; }

        /// <summary>
        /// 可选如身份验证请求中所指定。
        /// </summary>
        [XmlElement(ElementName = "contextId")]
        public string ContextId { get; set; }

        /// <summary>
        /// 应始终验证的私人访问令牌。
        /// </summary>
        [XmlElement(ElementName = "accessToken")]
        public string AccessToken { get; set; }

        /// <summary>
        /// 应始终验证的私人访问令牌。
        /// </summary>
        [XmlElement(ElementName = "roundId")]
        public long RoundId { get; set; }

        /// <summary>
        /// 可选如运营商帐户系统在身份验证响应中指定
        /// </summary>
        [XmlElement(ElementName = "externalGameSessionId")]
        public string ExternalGameSessionId { get; set; }

        /// <summary>
        /// 启动游戏所在的通道。
        /// </summary>
        [XmlElement(ElementName = "channel")]
        public int Channel { get; set; }

        [XmlElement(ElementName = "freegameExternalId")]
        public string FreeGameExternalId { get; set; }

        /// <summary>
        /// 注额的实际值。同时用于正常投注和免费游戏。对于免费游戏，是为免费游戏配置的注额。对于正常投注，这将与"real"中的相同。
        /// </summary>
        [XmlElement(ElementName = "actualValue")]
        public decimal ActualAmount { get; set; }

        /// <summary>
        /// 标记包含一个或多个累积大奖标记。
        /// </summary>
        [XmlElement(ElementName = "jackpots")]
        public PNGJackpots PngJackpots { get; set; }
    }

    [XmlRoot(ElementName = "cancelReserve")]
    public class PNG_CancelReserveResponse : PNG_BaseResponse
    {
        #region Constructor

        public PNG_CancelReserveResponse() { }

        public PNG_CancelReserveResponse(int statusCode, string statusMessage)
        {
            StatusCode = statusCode;
            StatusMessage = statusMessage;
        }

        #endregion

        /// <summary>
        /// 可选此交易的运营商帐户系统唯一 ID。
        /// </summary>
        [XmlElement(ElementName = "externalTransactionId")]
        public string ExternalTransactionId { get; set; }
    }

    #endregion

    #region Release Model

    [XmlRoot(ElementName = "release")]
    public class PNG_ReleaseRequest
    {
        /// <summary>
        /// 唯一的客户 ID。区分大小写。
        /// </summary>
        [XmlElement(ElementName = "externalId")]
        public string TpUserName { get; set; }

        /// <summary>
        /// 分配给提供商/产品的标识符。
        /// </summary>
        [XmlElement(ElementName = "productId")]
        public string ProductId { get; set; }

        /// <summary>
        /// 唯一的交易 ID。
        /// </summary>
        [XmlElement(ElementName = "transactionId")]
        public string TransactionId { get; set; }

        /// <summary>
        /// 派彩金額，如果輸則是 0 
        /// </summary>
        [XmlElement(ElementName = "real")]
        public decimal Real { get; set; }

        /// <summary>
        /// 3个字符的货币代码
        /// </summary>
        [XmlElement(ElementName = "currency")]
        public string Currency { get; set; }

        /// <summary>
        /// 由 Play'n GO 生成的 游戏会话标识符。
        /// </summary>
        [XmlElement(ElementName = "gameSessionId")]
        public string GameSessionId { get; set; }

        /// <summary>
        /// 0 = 游戏会话仍打开。 1 = 游戏会话已关闭。 请参阅第 0 节中的会话状态。
        /// </summary>
        [XmlElement(ElementName = "state")]
        public int State { get; set; }

        /// <summary>
        /// 可选游戏会话的投注金额，不包括来自免费游戏的任何金额。
        /// </summary>
        [XmlElement(ElementName = "totalLoss")]
        public decimal TotalLoss { get; set; }

        /// <summary>
        /// 可选游戏会话获得的奖金金额，包括来自免费游戏的任何金额。
        /// </summary>
        [XmlElement(ElementName = "totalGain")]
        public decimal TotalGain { get; set; }

        /// <summary>
        /// 可选游戏会话中进行的总回合数。
        /// </summary>
        [XmlElement(ElementName = "numRounds")]
        public int NumRounds { get; set; }

        /// <summary>
        /// OPEN 0 游戏会话仍打开。CLOSED 1 游戏会话已关闭。
        /// </summary>
        [XmlElement(ElementName = "type")]
        public int Type { get; set; }

        /// <summary>
        /// 遊戲標識符。請參閱遊戲詳情表，與登入遊戲的 GID 並不相同
        /// </summary>
        [XmlElement(ElementName = "gameId")]
        public string GameId { get; set; }

        /// <summary>
        /// 应在每次调用时验证的私人访问令牌。
        /// </summary>
        [XmlElement(ElementName = "accessToken")]
        public string AccessToken { get; set; }

        /// <summary>
        /// 已玩回合的 ID。 注意：如果在关闭会话时发送释 放， 则 roundID 设置为 0。
        /// </summary>
        [XmlElement(ElementName = "roundId")]
        public long RoundId { get; set; }

        /// <summary>
        /// 从累积大奖赢得的金额。
        /// </summary>
        [XmlElement(ElementName = "jackpotGain")]
        public decimal JackpotGain { get; set; }

        /// <summary>
        /// 贡献给累积大奖的金额。
        /// </summary>
        [XmlElement(ElementName = "jackpotLoss")]
        public decimal JackpotLoss { get; set; }

        /// <summary>
        /// 累积大奖被赢走后重设的金额。
        /// </summary>
        [XmlElement(ElementName = "jackpotGainSeed")]
        public decimal JackpotGainSeed { get; set; }

        /// <summary>
        /// 被赢走并重设的累积大奖 ID。
        /// </summary>
        [XmlElement(ElementName = "jackpotGainId")]
        public int JackpotGainId { get; set; }

        /// <summary>
        /// 可选 免费游戏参考号 ID（在免费游戏赔付 的情况下发送，即 type = Promotional）。
        /// </summary>
        [XmlElement(ElementName = "freegameExternalId")]
        public string FreeGameExternalId { get; set; }

        /// <summary>
        /// 可选来自免费游戏设置的投注额倍数（仅在最后一次免费游戏赔付时，即finished=1）。
        /// </summary>
        [XmlElement(ElementName = "turnover")]
        public int Turnover { get; set; }

        /// <summary>
        /// 可选 表示释放是否为免费游戏中的最后一个 回合（finished=1 表示最后一个免费游 戏回合）。
        /// </summary>
        [XmlElement(ElementName = "freegameFinished")]
        public int FreeGameFinished { get; set; }

        /// <summary>
        /// 可选 在免费游戏中赢得的总金额。此金额 也包含在 totalGain 属性中。
        /// </summary>
        [XmlElement(ElementName = "freegameGain")]
        public decimal FreeGameGain { get; set; }

        /// <summary>
        /// 可选 在免费游戏中输掉的总金额。此金额 不包含在 totalLoss 属性中。
        /// </summary>
        [XmlElement(ElementName = "freegameLoss")]
        public decimal FreeGameLoss { get; set; }

        /// <summary>
        /// 启动游戏所在的通道。 1 = 桌面 2 = 移动 3 = 迷你游戏 5 = 嵌入本地应用程序
        /// </summary>
        [XmlElement(ElementName = "channel")]
        public int Channel { get; set; }

        /// <summary>
        /// 免费游戏期间的总收益（如果是免费游戏回合）。
        /// </summary>
        [XmlElement(ElementName = "freegameTotalGain")]
        public string FreeGameTotalGain { get; set; }

        /// <summary>
        /// 可选标记包含关于累积大奖的信息。
        /// </summary>
        [XmlElement(ElementName = "jackpots")]
        public PNGJackpots PngJackpots { get; set; }
    }

    [XmlRoot(ElementName = "release")]
    public class PNG_ReleaseResponse : PNG_BaseResponse
    {
        #region Constructor

        public PNG_ReleaseResponse() { }

        public PNG_ReleaseResponse(int statusCode, string statusMessage)
        {
            StatusCode = statusCode;
            StatusMessage = statusMessage;
        }

        #endregion

        [XmlElement(ElementName = "externalTransactionId")]
        public string ExternalTransactionId { get; set; }

        [XmlElement(ElementName = "real")]
        public decimal AfterAmount { get; set; }
    }

    #endregion

    #region BaseResponse

    public class PNG_BaseResponse
    {
        /// <summary>
        /// 请求的状态。请参阅第 6.1 节中的状态代码
        /// </summary>
        [XmlElement(ElementName = "statusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// 可选指示失败的消息。
        /// </summary>
        [XmlElement(ElementName = "statusMessage")]
        public string StatusMessage { get; set; }
    }

    #endregion

    #region Enum

    public enum PNG_StatsuCodeEnum
    {
        /// <summary>
        /// 请求成功。
        /// </summary>
        Ok = 0,

        /// <summary>
        /// 用户未登录。
        /// </summary>
        NoUser = 1,

        /// <summary>
        /// 内部服务器错误。
        /// </summary>
        Internal = 2,

        /// <summary>
        /// 指定的货币不受支持。
        /// </summary>
        InvalidCurrency = 3,

        /// <summary>
        /// 用户名或密码错误。
        /// </summary>
        WrongUserNamePassword = 4,

        /// <summary>
        /// 帐户已被锁定。 
        /// </summary>
        AccountLocked = 5,

        /// <summary>
        /// 帐户已被禁用。
        /// </summary>
        AccountDisabled = 6,

        /// <summary>
        /// 请求的数额过高或过低。
        /// </summary>
        NotEnoughMoney = 7,

        /// <summary>
        /// 系统对此请求不可用。请稍后再试。
        /// </summary>
        MaxConcurrentCalls = 8,

        /// <summary>
        /// 已超出负责任博彩限制（金额）。
        /// </summary>
        SpendingBudgetExceeded = 9,

        /// <summary>
        /// 玩家的会话已过期。
        /// </summary>
        SessionExpired = 10,

        /// <summary>
        ///已超出负责任博彩限制（时间）。
        /// </summary>
        TimeBudgetExceeded = 11,

        /// <summary>
        /// 服务因任何原因不可用但能够响应，类似于 MaxConcurrentCalls（Service UnAvailable 的特殊状况）。
        /// </summary>
        ServiceUnavailable = 12
    }

    #endregion

    #region Data Transfer Model

    public class BettingDataRNG_PNG
    {
        /// <summary>
        /// 玩家名稱
        /// </summary>
        public string TpUserName { get; set; }

        /// <summary>
        /// 分配给產品的標示符
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// 唯一交易Id
        /// </summary>
        public string TransactionId { get; set; }

        /// <summary>
        /// 投注中使用的"真錢"金額。如果是免費遊戲回合，則為 0
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 貨幣代碼
        /// </summary>
        public string CurrencyIsoCode { get; set; }

        /// <summary>
        /// 遊戲標識符。請參閱遊戲詳情表，與登入遊戲的 GID 並不相同
        /// </summary>
        public string GameId { get; set; }

        /// <summary>
        /// 遊戲會話標識符由 Play’n GO 生成。
        /// </summary>
        public string GameSessionId { get; set; }

        /// <summary>
        /// 回合ID
        /// </summary>
        public long RoundId { get; set; }

        /// <summary>
        /// 啟動遊戲所在的通道。 1 = 桌面，2 = 移動，3 = 迷你游戲，5 = 嵌入本地應用程序
        /// </summary>
        public int Channel { get; set; }

        /// <summary>
        /// 免費遊戲參考號 ID。每個用戶和免費遊戲使用唯一的 ID
        /// </summary>
        public string FreeGameExternalId { get; set; }

        /// <summary>
        /// 注額的實際值。同時用於正常投注和免費遊戲。對於免費遊戲，是為免費遊戲配置的注額。對於正常投注，這將與“real”中的相同。
        /// </summary>
        public decimal ActualAmount { get; set; }

        /// <summary>
        /// 累積大獎贏得的金額
        /// </summary>
        public decimal JackpotWin { get; set; }

        /// <summary>
        /// 累積大獎貢獻金額
        /// </summary>
        public decimal JackpotLoss { get; set; }

        public int TicketType { get; set; }
    }

    public class PNGResult
    {
        public string ExternalTransactionId { get; set; }

        public decimal AfterAmount { get; set; }
    }

    #endregion

    #region SettleData

    public class SettleDataRNG_PNG
    {
        public string TpUserName { get; set; }
        public string TransId { get; set; }
        public string GameId { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinLostAmount { get; set; }
        public decimal ProgressiveBet { get; set; }
        public decimal ProgressiveWin { get; set; }
        public long SettledTime { get; set; }
        public long TransTime { get; set; }
    }


    #endregion

    #region Config

    public class PNGConfig
    {
        public string ApiUrl { get; set; }

        public string Password { get; set; }

        public string UserName { get; set; }

        public string AccessToken { get; set; }

        public string GameUrl { get; set; }

        public string GameHistoryMobile { get; set; }

        public string GameHistoryDesktop { get; set; }

        public int Pid { get; set; }
    }

    #endregion

}
