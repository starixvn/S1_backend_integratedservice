using K4os.Hash.xxHash;
using System.Xml.Serialization;

namespace ThirdPartySeamlessWallet.Service.Model
{
    [XmlRoot(ElementName = "authenticate")]
    public class PNG_AuthenticateRequest
    {
        /// <summary>
        /// 运营商提供的会话令牌账户系统。
        /// </summary>
        [XmlElement(ElementName = "username")]
        public string Username { get; set; }

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
        /// 游戏标识符。请参阅游戏、货币、国家/地区和语言附录。
        /// </summary>
        [XmlElement(ElementName = "gameId")]
        public int GameId { get; set; }

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

    [XmlRoot(ElementName = "authenticate")]
    public class PNG_AuthenticateResponse
    {
        /// <summary>
        /// 唯一的客户 ID。对于同一用户，此 ID 必须始终相同。区分大小写。
        /// </summary>
        [XmlElement(ElementName = "externalId")]
        public string ExternalId { get; set; }

        /// <summary>
        /// 请求的状态。请参阅第 6.1 节中的状态代码。
        /// </summary>
        [XmlElement(ElementName = "statusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// 状态不正常时在客户端中显示的错误 string。
        /// </summary>
        [XmlElement(ElementName = "statusMessage")]
        public string StatusMessage { get; set; }

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

    public enum PNG_StatsuCode
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
}
