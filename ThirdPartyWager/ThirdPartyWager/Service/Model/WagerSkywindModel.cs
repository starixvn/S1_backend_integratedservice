using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Summary description for Class1
/// </summary>
namespace ThirdPartyWager.Service.Model
{
    public class SkywindConfig
    {
        public string ApiUrl { get; set; }
        public string SecretKey { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string MerchId { get; set; }
        public string MerchPwd { get; set; }
        public SkywindConfig(Dictionary<string, string> inputs)
        {
            ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"])
                ? inputs["ApiUrl"]
                : throw new Exception();

            SecretKey = inputs.ContainsKey("SecretKey") && !string.IsNullOrEmpty(inputs["SecretKey"])
                ? inputs["SecretKey"]
                : string.Empty;

            Username = inputs.ContainsKey("Username") && !string.IsNullOrEmpty(inputs["Username"])
                ? inputs["Username"]
                : string.Empty;

            Password = inputs.ContainsKey("Password") && !string.IsNullOrEmpty(inputs["Password"])
                ? inputs["Password"]
                : string.Empty;

            MerchId = inputs.ContainsKey("MerchId") && !string.IsNullOrEmpty(inputs["MerchId"])
                ? inputs["MerchId"]
                : string.Empty;

            MerchPwd = inputs.ContainsKey("MerchPwd") && !string.IsNullOrEmpty(inputs["MerchPwd"])
                ? inputs["MerchPwd"]
                : string.Empty;
        }
    }
    public class WagerSkywindModel
    {
        public bool finished { get; set; }
        public decimal revenue { get; set; }
        public string currency { get; set; }
        public decimal? debit { get; set; }
        public decimal? credit { get; set; }
        public string roundId { get; set; }
        public string brandId { get; set; }
        public string playerCode { get; set; }
        public string gameCode { get; set; }
        public string device { get; set; }
        public decimal win { get; set; }
        public decimal bet { get; set; }
        public decimal balanceBefore { get; set; }
        public decimal balanceAfter { get; set; }
        public string totalEvents { get; set; }
        public string firstTs { get; set; }
        public string ts { get; set; }
        public bool isTest { get; set; }
        public string insertedAt { get; set; }
        public string recoveryType { get; set; }
        public decimal totalJpContribution { get; set; }
        public decimal totalJpWin { get; set; }
    }

    public class BetSkywind
    {
        public int finished { get; set; }
        public decimal revenue { get; set; }
        public string currency { get; set; }
        public decimal debit { get; set; }
        public decimal credit { get; set; }
        public string roundId { get; set; }
        public string brandId { get; set; }
        public string playerCode { get; set; }
        public string gameCode { get; set; }
        public string device { get; set; }
        public decimal win { get; set; }
        public decimal bet { get; set; }
        public decimal balanceBefore { get; set; }
        public decimal balanceAfter { get; set; }
        public string totalEvents { get; set; }
        public DateTime firstTs { get; set; }
        public DateTime ts { get; set; }
        public int isTest { get; set; }
        public DateTime insertedAt { get; set; }
        public string recoveryType { get; set; }
        public decimal totalJpContribution { get; set; }
        public decimal totalJpWin { get; set; }
    }
}

