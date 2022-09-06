using System;
using System.Collections.Generic;

namespace CommonService.Service.Model
{
    public class IovationServicesConfig
    {
        public string ResourceURL { get; set; }
        public string SubscriberID { get; set; }
        public string SubscriberAccount { get; set; }
        public string Password { get; set; }
    }

    public class IovationChecksRequest
    {
        public int accountCode { get; set; }
        public string blackbox { get; set; }
        public string statedIp { get; set; }
        public Dictionary<string, string> transactionInsight { get; set; }
        public string type { get; set; }
    }
    
    public class IovationDetails
    {
        public Device device { get; set; }
        public RuleResults ruleResults { get; set; }
    }
    public class Device
    {
        public string alias { get; set; }
        public string type { get; set; }
    }
    public class RuleResults
    {
        public int score { get; set; }
        public int rulesMatched { get; set; }
        public IList<Rule> rules { get; set; }
    }
    public class Rule
    {
        public string type { get; set; }
        public string reason { get; set; }
        public int score { get; set; }
    }
    public class IovationResult
    {

        public int accountCode { get; set; }
        public IovationDetails details { get; set; }
        public string id { get; set; }
        public string reason { get; set; }
        public string result { get; set; }
        public string statedIp { get; set; }
        public long trackingNumber { get; set; }
        public string transactionId { get; set; }
        public string timestamp { get; set; }
        public string path { get; set; }
        public string type { get; set; }
        public int code { get; set; }
        public string message { get; set; }
    }

    public class IovationCheckResult
    {
        public string Result { get; set; }
        public string Reason { get; set; }
        public string DeviceId { get; set; }
        public string DeviceType { get; set; }
        public int Score { get; set; }
    }

}
