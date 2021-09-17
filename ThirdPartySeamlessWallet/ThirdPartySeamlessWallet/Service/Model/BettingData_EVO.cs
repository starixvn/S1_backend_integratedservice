using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingData_EVO
    {
        public Transaction_EVO transaction { get; set; }
        public string sid { get; set; }
        public string userId { get; set; }
        public string uuid { get; set; }
        public string currency { get; set; }
        public Game_EVO game { get; set; }
    }

    public class Transaction_EVO
    {
        public string id { get; set; }
        public string refId { get; set; }
        public decimal amount { get; set; }
    }

    public class Game_EVO
    {
        public string id { get; set; }
        public string type { get; set; }
        public Details_EVO details { get; set; }
    }

    public class Details_EVO
    {
        public Table_EVO table { get; set; }
    }

    public class Table_EVO
    {
        public string id { get; set; }
        public object vid { get; set; }
    }

}
