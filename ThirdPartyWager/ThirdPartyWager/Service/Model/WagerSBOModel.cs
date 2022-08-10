using System;
using System.Collections.Generic;

namespace ThirdPartyWager.Service.Model
{
    public class SboSetting
    {
        public string Agent { get; set; }
        public string ApiUrl { get; set; }
        public string ServerId { get; set; }
        public string CompanyKey { get; set; }
    }

    public class SboSubBet
    {
        public string betOption { get; set; }
        public string marketType { get; set; }
        public string sportType { get; set; }
        public decimal hdp { get; set; }
        public decimal odds { get; set; }
        public string league { get; set; }
        public string match { get; set; }
        public string status { get; set; }
        public DateTime winlostDate { get; set; }
        public string liveScore { get; set; }
        public string htScore { get; set; }
        public string ftScore { get; set; }
        public string customeizedBetType { get; set; }
        public DateTime kickOffTime { get; set; }
    }

    public class SboResult
    {
        public IList<SboSubBet> subBet { get; set; }
        public string sportsType { get; set; }
        public decimal odds { get; set; }
        public string oddsStyle { get; set; }
        public decimal actualStake { get; set; }
        public decimal turnover { get; set; }
        public bool isHalfWonLose { get; set; }
        public bool isLive { get; set; }
        public decimal maxWinWithoutActualStake { get; set; }
        public string ip { get; set; }
        public bool isSystemTagRisky { get; set; }
        public bool isCustomerTagRisky { get; set; }
        public string voidReason { get; set; }
        public DateTime orderTime { get; set; }
        public DateTime modifyDate { get; set; }
        public DateTime settleTime { get; set; }
        public DateTime winLostDate { get; set; }
        public string refNo { get; set; }
        public string username { get; set; }
        public string currency { get; set; }
        public decimal stake { get; set; }
        public decimal winLost { get; set; }
        public string status { get; set; }
        public string topDownline { get; set; }
    }

    public class SboError
    {
        public int id { get; set; }
        public string msg { get; set; }
    }

    public class SboWagers
    {
        public IList<SboResult> result { get; set; }
        public string serverId { get; set; }
        public SboError error { get; set; }
    }
}