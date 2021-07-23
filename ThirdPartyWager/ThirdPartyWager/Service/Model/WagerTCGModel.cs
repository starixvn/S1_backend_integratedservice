using System.Collections.Generic;
using ThirdPartyWager.Extensions;

namespace ThirdPartyWager.Service.Model
{
    public class TCGConfig
    {
        public string WagerUrl { get; set; }
        public string WagerAccount { get; set; }
        public string WagerPassword { get; set; }
        public string WagerTypeList { get; set; }
        public TCGConfig(Dictionary<string, string> inputs, string tpCurrencyId)
        {
            WagerUrl = inputs.GetValue($"WagerUrl");
            WagerAccount = inputs.GetValue($"WagerAccount_{tpCurrencyId}");
            WagerPassword = inputs.GetValue($"WagerPassword_{tpCurrencyId}");
            WagerTypeList = inputs.GetValue($"WagerTypeList_{tpCurrencyId}");
        }
    }


    public class TCGQueryWagerObj<T>
    {
        public List<T> list { get; set; } = new List<T>();
        public PageInfo page { get; set; } = new PageInfo();
    }

    public class PageInfo
    {
        public int currentPage { get; set; }
        public int pageSize { get; set; }
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
        public string playName { get; set; } = "";
        public string gameGroupName { get; set; } = "";
        public decimal winAmount { get; set; }
        public decimal netPNL { get; set; }
        public string betStatus { get; set; } = "";
        public string settlementTime { get; set; } = "";
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
        public string multiple { get; set; } = "";
        public string playName { get; set; } = "";
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
        public string drawTime { get; set; } = "";
        public decimal exceedWinAmount { get; set; }
        public string winningNumber { get; set; } = "";
        public string clientIp { get; set; } = "";
        public string single { get; set; } = "";
        public decimal playBonus { get; set; }
    }


}
