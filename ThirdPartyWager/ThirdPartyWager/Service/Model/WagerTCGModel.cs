using ThirdPartyWager.Extensions;

namespace ThirdPartyWager.Service.Model
{
    public class TCGConfig
    {
        public int SiteId { get; set; }
        public string WagerUrl { get; set; }
        public string WagerAccount { get; set; }
        public string WagerPassword { get; set; }
        public string WagerTypeList { get; set; }
        public string ApiUrl { get; set; }
        public string MerchantCode { get; set; }
        public string DesKey { get; set; }
        public string SignKey { get; set; }
        public string TpCurrencyId { get; set; }
        public TCGConfig(int siteId, Dictionary<string, string> inputs, string tpCurrencyId)
        {
            SiteId = siteId;
            TpCurrencyId = tpCurrencyId;
            WagerUrl = inputs.GetValue($"WagerUrl");
            WagerAccount = inputs.GetValue($"WagerAccount_{tpCurrencyId}");
            WagerPassword = inputs.GetValue($"WagerPassword_{tpCurrencyId}");
            WagerTypeList = inputs.GetValue($"WagerTypeList_{tpCurrencyId}");
            ApiUrl = inputs.GetValue("ApiUrl");
            MerchantCode = inputs.GetValue($"MerchantCode_{tpCurrencyId}");
            DesKey = inputs.GetValue($"DESKey_{tpCurrencyId}");
            SignKey = inputs.GetValue($"SHA256Key_{tpCurrencyId}");
        }
    }

    public class TCGQueryWagerObj<T>
    {
        public List<T> list { get; set; } = new List<T>();
        public PageInfo page { get; set; } = new PageInfo();
        public bool Pass = false;
    }

    public class PageInfo
    {
        public int currentPage { get; set; }
        public int pageSize { get; set; } = 1000;
        public int total { get; set; }
    }

    public class TCGSEAWagerData
    {
        public decimal betAmount { get; set; }
        public string gameCode { get; set; } = "";
        public string betOrderNo { get; set; } = "";
        public string betTime { get; set; } = "";
        public string transTime { get; set; } = "";
        public string betContentId { get; set; } = "";
        public string playCode { get; set; } = "";
        public string orderNum { get; set; } = "";
        public string chase { get; set; } = "";
        public string numero { get; set; } = "";
        public string merchantCode { get; set; } = "";
        public string bettingContent { get; set; } = "";
        public int playId { get; set; }
        public string freezeTime { get; set; } = "";
        public int multiple { get; set; }
        public string remark { get; set; } = "";

        public string playName
        {
            get
            {
                return _playName ?? "";
            }
            set
            {
                _playName = value;
            }
        }
        private string _playName { get; set; }

        public string gameGroupName { get; set; } = "";
        public decimal winAmount { get; set; }
        public decimal netPNL { get; set; }
        public string betStatus { get; set; } = "";
        public DateTime settlementTime { get; set; }
        public decimal actualBetAmount { get; set; }
        public decimal exceedWinAmount { get; set; }
        public string details { get; set; } = "";
        public string winningNumber { get; set; } = "";
        public string clientIp { get; set; } = "";
        public decimal playBonus { get; set; }
        public string username { get; set; } = "";
        public string productType { get; set; } = "";
        public string single { get; set; } = "";
        public int detailStatusId { get; set; }
        public long SettleTimeUnix
        {
            get
            {
                if (settlementTime == default)
                {
                    return 0;
                }
                return new DateTimeOffset(settlementTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public TCGSEAWagerData() { }
        public TCGSEAWagerData(TCGSEAUnSettleData unSettleData)
        {
            betAmount = unSettleData.betAmount;
            gameCode = unSettleData.gameCode;
            betOrderNo = unSettleData.betOrderNo;
            betTime = unSettleData.betTime;
            transTime = unSettleData.transTime;
            betContentId = unSettleData.betContentId;
            playCode = unSettleData.playCode;
            orderNum = unSettleData.orderNum;
            chase = unSettleData.chase;
            numero = unSettleData.numero;
            merchantCode = "";
            bettingContent = unSettleData.bettingContent;
            playId = unSettleData.playId;
            freezeTime = unSettleData.freezeTime;
            multiple = unSettleData.multiple;
            remark = unSettleData.remark;
            playName = unSettleData.playName;
            gameGroupName = unSettleData.gameGroupName;
            winAmount = 0;
            netPNL = 0;
            betStatus = "0";
            settlementTime = default;
            actualBetAmount = 0;
            exceedWinAmount = 0;
            details = "";
            winningNumber = "";
            clientIp = "";
            playBonus = 0;
            username = unSettleData.username;
            productType = unSettleData.productType;
            single = unSettleData.single;
            detailStatusId = 0;
        }
    }

    public class TCGVNWagerData
    {
        public string orderDetailId { get; set; } = "";
        public string gameCode { get; set; } = "";
        public string numero { get; set; } = "";
        public string orderNum { get; set; } = "";
        public string username { get; set; } = "";
        public string merchantCode { get; set; } = "";
        public string gameGroupName { get; set; } = "";
        public string orderContentId { get; set; } = "";
        public string orderMasterId { get; set; } = "";
        public decimal? betAmount { get; set; }
        public string bettingTime { get; set; } = "";
        public string chase { get; set; } = "";
        public int multiple { get; set; }

        public string playName
        {
            get
            {
                return _playName ?? "";
            }
            set
            {
                _playName = value;
            }
        }
        private string _playName { get; set; }

        public string remark { get; set; } = "";
        public decimal totalBet { get; set; }
        public int series { get; set; }
        public int maxSeries { get; set; }
        public string bettingContent { get; set; } = "";
        public string bettingApproach { get; set; } = "";
        public int playId { get; set; }
        public string freezeTime { get; set; } = "";
        public decimal actualBetAmount { get; set; }
        public decimal winAmount { get; set; }
        public decimal planWinAmount { get; set; }
        public decimal rebateAmount { get; set; }
        public int result { get; set; }
        public DateTime drawTime { get; set; }
        public decimal exceedWinAmount { get; set; }
        public string winningNumber { get; set; } = "";
        public string clientIp { get; set; } = "";
        public string single { get; set; } = "";
        public decimal playBonus { get; set; }

        public long SettleTimeUnix
        {
            get
            {
                if (drawTime == default)
                {
                    return 0;
                }
                return new DateTimeOffset(drawTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public TCGVNWagerData() { }
        public TCGVNWagerData(TCGVNUnSettleData unSettleData)
        {
            orderDetailId = unSettleData.orderDetailId;
            gameCode = unSettleData.gameCode;
            numero = unSettleData.numero;
            orderNum = unSettleData.orderNum;
            username = unSettleData.username;
            merchantCode = unSettleData.merchantCode;
            gameGroupName = unSettleData.gameGroupName;
            orderContentId = unSettleData.orderContentId;
            orderMasterId = unSettleData.orderMasterId;
            betAmount = unSettleData.betAmount;
            bettingTime = unSettleData.bettingTime;
            chase = unSettleData.chase;
            multiple = unSettleData.multiple;
            playName = unSettleData.playName;
            remark = unSettleData.remark;
            totalBet = unSettleData.totalBet;
            series = unSettleData.series;
            maxSeries = unSettleData.maxSeries;
            bettingContent = unSettleData.bettingContent;
            bettingApproach = unSettleData.bettingApproach;
            playId = unSettleData.playId;
            freezeTime = unSettleData.freezeTime;
            actualBetAmount = 0;
            winAmount = 0;
            planWinAmount = 0;
            rebateAmount = 0;
            result = 0;
            drawTime = default;
            exceedWinAmount = 0;
            winningNumber = "";
            clientIp = "";
            single = unSettleData.single;
            playBonus = 0;
        }
    }

    public class TCGVNCancelWagerData
    {
        public string orderDetailId { get; set; } = "";
        public string gameCode { get; set; } = "";
        public string numero { get; set; } = "";
        public string orderNum { get; set; } = "";
        public string username { get; set; } = "";
        public string merchantCode { get; set; } = "";
        public string gameGroupName { get; set; } = "";
        public decimal planBetAmount { get; set; }
        public string orderContentId { get; set; } = "";
        public string cancelTime { get; set; } = "";
        public int cancelType { get; set; }
        public string bettingContent { get; set; } = "";
        public string freezeTime { get; set; } = "";
        public int multiple { get; set; }
        public string bettingTime { get; set; } = "";
        private string _playName { get; set; }
        public string playName
        {
            get
            {
                return _playName ?? "";
            }
            set
            {
                _playName = value;
            }
        }
        public string remark { get; set; } = "";
        public string single { get; set; } = "";
    }
    
    public class TCGUnSettleObj
    {
        public int status { get; set; }
        public List<TCGVNUnSettleData> orders { get; set; }
        public List<TCGSEAUnSettleData> details { get; set; }
        public string error_desc { get; set; }
        public Page_Info page_info { get; set; }
    }

    public class Page_Info
    {
        public int totalPage { get; set; }
        public int currentPage { get; set; }
        public int totalCount { get; set; }
    }

    public class TCGVNUnSettleData
    {
        public string orderDetailId { get; set; }
        public string gameCode { get; set; }
        public string numero { get; set; }
        public string orderNum { get; set; }
        public string username { get; set; }
        public string merchantCode { get; set; }
        public string orderContentId { get; set; }
        public string orderMasterId { get; set; }
        public decimal betAmount { get; set; }
        public string bettingTime { get; set; }
        public string chase { get; set; }
        public int multiple { get; set; }
        public string playName { get; set; }
        public string remark { get; set; }
        public decimal totalBet { get; set; }
        public int series { get; set; }
        public int maxSeries { get; set; }
        public string bettingContent { get; set; }
        public string bettingApproach { get; set; }
        public int playId { get; set; }
        public string freezeTime { get; set; }
        public string gameGroupName { get; set; }
        public string odds { get; set; }
        public string single { get; set; }
    }

    public class TCGSEAUnSettleData
    {
        public decimal betAmount { get; set; }
        public string gameCode { get; set; }
        public object gameName { get; set; }
        public string betOrderNo { get; set; }
        public string betTime { get; set; }
        public string transTime { get; set; }
        public string betContentId { get; set; }
        public string playCode { get; set; }
        public string orderNum { get; set; }
        public string chase { get; set; }
        public string numero { get; set; }
        public string bettingContent { get; set; }
        public int playId { get; set; }
        public string freezeTime { get; set; }
        public int multiple { get; set; }
        public string remark { get; set; }
        public string playName { get; set; }
        public string gameGroupName { get; set; }
        public string device { get; set; }
        public string odds { get; set; }
        public string username { get; set; }
        public string productType { get; set; }
        public string single { get; set; }
    }

    public class TCGRedoObj
    {
        public string TpCurrencyId { get; set; }

        public int SiteId { get; set; }

        public TCGRedoObj(string tpCurrencyId, int siteId)
        {
            TpCurrencyId = tpCurrencyId;
            SiteId = siteId;
        }
    }
}
