using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{

    public class BettingDataBTIReserve
    {
        public string cust_id { get; set; }

        public string reserve_id { get; set; }

        public decimal amount { get; set; } = 0;

        public string req_id { get; set; }

        public string requestBody { get; set; }
    }

    public class BTiDebitReserveRequest
    {
        public BTiDebitReserveRequest(BettingDataBTIReserve request)
        {
            cust_id = request.cust_id;
            reserve_id = request.reserve_id;
            amount = request.amount;
            req_id = request.req_id;
            var bet = XDocument.Parse(request.requestBody).Descendants("Bet").FirstOrDefault();
            BetID = bet.Attribute("BetID")?.Value ?? "";
            BetTypeID = bet.Attribute("BetTypeID")?.Value ?? "";
            BetTypeName = bet.Attribute("BetTypeName")?.Value ?? "";
            CreationDate = bet.Attribute("CreationDate")?.Value ?? "";
            Stake = bet.Attribute("Stake")?.Value ?? "";
            Odds = bet.Attribute("Odds")?.Value ?? "";
            CommomStatusID = bet.Attribute("CommomStatusID")?.Value ?? "";
            Status = bet.Attribute("Status")?.Value ?? "";
            ComboBetNumersLines = bet.Attribute("ComboBetNumersLines")?.Value ?? "";
            PurchaseBetID = bet.Attribute("PurchaseBetID")?.Value ?? "";
            Gain = bet.Attribute("Gain")?.Value ?? "";
            BodyData = request.requestBody;
        }
        public string cust_id { get; set; }
        public string reserve_id { get; set; }
        public decimal amount { get; set; }
        public string req_id { get; set; }
        public string BetID { get; set; }
        public string BetTypeID { get; set; }
        public string BetTypeName { get; set; }
        public string CreationDate { get; set; }
        public string Stake { get; set; }
        public string Odds { get; set; }
        public string CommomStatusID { get; set; }
        public string Status { get; set; }
        public string ComboBetNumersLines { get; set; }
        public string PurchaseBetID { get; set; }
        public string Gain { get; set; }
        public string BodyData { get; set; }
    }
}
