using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ThirdPartyWager.Service.Model
{
    public class YggConfig
    {
        /// <summary>
        /// 獲取注單位置
        /// </summary>
        public string WagerUrl { get; set; }

        /// <summary>
        /// 品牌名稱
        /// </summary>
        public string Org { get; set; }


        public string ContentType => "application/x-www-form-urlencoded";

        public YggConfig(Dictionary<string, string> inputs)
        {
            WagerUrl = inputs.ContainsKey("WagerUrl") && !string.IsNullOrEmpty(inputs["WagerUrl"])
                ? inputs["WagerUrl"]
                : throw new Exception();

            Org = inputs.ContainsKey("org") && !string.IsNullOrEmpty(inputs["org"])
                ? inputs["org"]
                : string.Empty;
        }
    }

    #region Model

    public class WagerYGGModel
    {
        [JsonProperty("org")]
        public string Org { get; set; }

        [JsonProperty("loginname")]
        public string Loginname { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("gamename")]
        public string Gamename { get; set; }

        [JsonProperty("reference")]
        public string Reference { get; set; }

        [JsonProperty("createtime")]
        public string Createtime { get; set; }

        [JsonProperty("DCGameID")]
        public string DCGameID { get; set; }
    }

    public class WagerYggResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public List<WagerYGGModel> Data { get; set; }
    }

    #endregion
}
