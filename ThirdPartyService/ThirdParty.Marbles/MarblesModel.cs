using ThirdPartyCore.Model;

namespace ThirdParty.Marbles
{
    public class MarblesConfig : TpSiteConfig
        {
            public string MerchantId { get; set; }
            public string MerchantCode { get; set; }
            public string AgentKey { get; set; }
            public string AgentId { get; set; }
        }

        public class LoginRequest
        {
            public string handle { get; set; }
            public string lineCode { get; set; }
            public string agent { get; set; }
            public string account { get; set; }
            //public string gameCode { get; set; }
            public string ip { get; set; }
            //public string backUrl { get; set; }
            public string lang { get; set; }
            public string playerSession { get; set; }
            public string currency { get; set; }
            //public string ctype { get; set; }
        }
        public class LoginResponse
        {
            public string code { get; set; }
            public string msg { get; set; }
            public LoginResponseData data { get; set; }
        }
        public class LoginResponseData
        {
            public string url { get; set; }
        }
        public enum MarblesErrorCode
        {
            /// <summary>
            /// 业务异常
            /// </summary>
            E000,

            /// <summary>
            /// 不合法的请求
            /// </summary>
            E010,

            /// <summary>
            /// 不合法的Handle
            /// </summary>
            E011,

            /// <summary>
            /// 参数不合法
            /// </summary>
            E012,

            /// <summary>
            /// 解密异常
            /// </summary>
            E013,

            /// <summary>
            /// 填充参数出错.{0}
            /// </summary>
            E014,

            /// <summary>
            /// handle处理异常
            /// </summary>
            E015,

            /// <summary>
            /// 域名不存在
            /// </summary>
            E016,

            /// <summary>
            /// IP限制,禁止访问
            /// </summary>
            E017,

            /// <summary>
            /// 请求太频繁，请{0}秒后再试！
            /// </summary>
            E018,

            /// <summary>
            /// 密码错误
            /// </summary>
            E019,

            /// <summary>
            /// Token错误
            /// </summary>
            E020,

            /// <summary>
            /// 中心域名不存在
            /// </summary>
            E021,

            /// <summary>
            /// 站点号不存在
            /// </summary>
            E022,

            /// <summary>
            /// 代理号不存在
            /// </summary>
            E023,

            /// <summary>
            /// 帐号已经存在
            /// </summary>
            E031,

            /// <summary>
            /// 帐号不存在
            /// </summary>
            E032,

            /// <summary>
            /// 保存失败
            /// </summary>
            E033,

            /// <summary>
            /// 交易号已存在
            /// </summary>
            E034,

            /// <summary>
            /// 交易号不存在
            /// </summary>
            E035,

            /// <summary>
            /// 交易失败
            /// </summary>
            E036,

            /// <summary>
            /// 余额不足
            /// </summary>
            E037,

            /// <summary>
            /// 帐号已停用(转入，转出交易失败)
            /// </summary>
            E038,

            /// <summary>
            /// 帐号已冻结(转入，转出交易失败)
            /// </summary>
            E039,

            /// <summary>
            /// 帐号未激活/未审核
            /// </summary>
            E040,

            /// <summary>
            /// 帐号审核失败
            /// </summary>
            E041,

            /// <summary>
            /// 当天取款超出限额
            /// </summary>
            E042,

            /// <summary>
            /// 会员在玩游戏，不能做取款操作
            /// </summary>
            E043,

            /// <summary>
            /// 帐号登录错误次数超过{0}次，当天将限制访问
            /// </summary>
            E044,

            /// <summary>
            /// 站长分数不足，请咨询站长
            /// </summary>
            E045,

            /// <summary>
            /// 商户处理失败
            /// </summary>
            ET001,

            /// <summary>
            /// 系统维护中
            /// </summary>
            M001
        }
}