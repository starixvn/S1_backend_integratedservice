using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataLC_WM
    {
        public string user { get; set; }
        public decimal money { get; set; }
        public string dealid { get; set; }
        public string gtype { get; set; }
        public string type { get; set; }
        public string betdetail { get; set; }
        public string gameno { get; set; }
        public string code { get; set; }
        public string category { get; set; }
        public DateTime requestDate { get; set; }
        public string cmd { get; set; }
        public string signature { get; set; }

        public decimal RealBetAmount { get; set; }
        public decimal DetailSumAmount
        {
            get
            {
                try
                {
                    var dic = JsonConvert.DeserializeObject<Dictionary<string, decimal>>(betdetail);
                    return dic.Sum(i => i.Value);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }
    }

    public class SettleData_WM : SettleData
    {
        public decimal RealBetAmount { get; set; }
    }
}
