using System;
using System.Collections.Generic;

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
}
