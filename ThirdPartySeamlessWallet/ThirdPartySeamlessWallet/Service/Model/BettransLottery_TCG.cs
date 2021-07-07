using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{

    public class TCGApiMethod
    {
        public const string GetBalance = "sgb";
    }
    public class TCGConfig
    {
        public string GameUrl { get; set; }
        public string ApiUrl { get; set; }
        public string MerchantCode { get; set; }
        public string DesKey { get; set; }
        public string SignKey { get; set; }
        public TCGConfig(Dictionary<string, string> inputs, string merchant_code)
        {
            var key = inputs.GetKey(merchant_code);
            var currency = key.Split("_")[1];
            GameUrl = inputs.GetValue("GameUrl");
            ApiUrl = inputs.GetValue("ApiUrl");
            MerchantCode = inputs.GetValue($"MerchantCode_{currency}");
            DesKey = inputs.GetValue($"DESKey_{currency}");
            SignKey = inputs.GetValue($"SHA256Key_{currency}");
        }
    }

    public class TCGBaseResult
    {
        public string status { get; set; }

        public string error_desc { get; set; } 
    }
}
