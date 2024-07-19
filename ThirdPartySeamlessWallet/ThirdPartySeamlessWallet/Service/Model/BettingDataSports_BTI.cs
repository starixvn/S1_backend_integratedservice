using System.Xml.Linq;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BTIRefreshObj
    {
        public string status { get; set; }
        public string message { get; set; }
        public string balance { get; set; }

        public BTIRefreshObj(bool result)
        {
            status = "failure";
            message = "fail.";
            balance = "0";
        }
        public BTIRefreshObj() { }

    }

    public class BTIStatusObj
    {
        public string status { get; set; }
        public string message { get; set; }
        public string balance { get; set; }
        public string uid { get; set; }
        public string token { get; set; }

        public BTIStatusObj(string token)
        {
            this.token = token;
            uid = Guid.NewGuid().ToString();
            status = "anon";
            message = "fail.";
            balance = "0";
        }

        public BTIStatusObj() { }
    }

    public class BTIRequestObj
    {
        public string cust_id { get; set; }

        public string reserve_id { get; set; }

        public decimal amount { get; set; } = 0;

        public string req_id { get; set; }

        public string purchase_id { get; set; }

        public string requestBody { get; set; }
    }

    public class BTIResult
    {
        public int SPResult { get; set; } = -1;

        public int SetBalanceResult { get; set; } = -1;

        public long Trx_id { get; set; }

        public decimal AfterAmount { get; set; }

        public decimal ReturnAmount { get; set; }
    }

    public class BTIDebitRequest
    {
        public BTIDebitRequest(BTIRequestObj request)
        {
            cust_id = request.cust_id;
            reserve_id = request.reserve_id;
            amount = request.amount;
            req_id = request.req_id;
            purchase_id = request.purchase_id;
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
        public string purchase_id { get; set; }
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

    public class BTICreditRequest
    {
        public BTICreditRequest(BTIRequestObj request)
        {
            cust_id = request.cust_id;
            amount = request.amount;
            req_id = request.req_id;
            purchase_id = request.purchase_id;
            var xd = XDocument.Parse(request.requestBody);
            BetID = FindSettleBetID(xd);
            var purchase = xd.Descendants("Purchase").FirstOrDefault();
            reserve_id = purchase.Attribute("ReserveID").Value;
            PurchaseID = purchase.Attribute("PurchaseID").Value;
            CurrentStatus = purchase.Attribute("CurrentStatus").Value;
            seq_num = purchase.Attribute("seq_num").Value;
            BodyData = request.requestBody;
            TicketType = (int)BettransType.Payout;
            TicketStatus = (int)enums.TicketStatus.UnSettled;
            SettleFlag = CurrentStatus.Equals("Closed", StringComparison.OrdinalIgnoreCase) ?
                (int)enums.SettleFlag.EndGame : (int)enums.SettleFlag.Running;
        }
        public string cust_id { get; set; }
        public string reserve_id { get; set; }
        public decimal amount { get; set; }
        public string req_id { get; set; }
        public string purchase_id { get; set; }
        public string BetID { get; set; }
        public string PurchaseID { get; set; }
        public string CurrentStatus { get; set; }
        public string seq_num { get; set; }
        public string BodyData { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }

        private string FindSettleBetID(XDocument XD)
        {
            try
            {
                foreach (XElement item in XD.Descendants("Bet"))
                {
                    //比對狀態不同的回傳ID
                    if (item.Attribute("OldStatus").Value != item.Attribute("NewStatus").Value)
                    {
                        return item.Attribute("ID").Value;
                    }
                }
                //都沒找到使用第一筆找到的
                return XD.Descendants("Bet").FirstOrDefault().Attribute("ID").Value;
            }
            catch (Exception)
            {
                return "";
            }

        }
    }

    public class BTISettleData : SettleData
    {
        public string BetID { get; set; }

        public decimal DebitAmount { get; set; }

        public decimal CreditAmount { get; set; }

        public string CurrentStatus { get; set; }

        public string BodyData { get; set; }

        public int seq_num { get; set; }

        public string GetSettleStatus()
        {
            try
            {
                var XD = XDocument.Parse(BodyData);
                foreach (XElement item in XD.Descendants("Change"))
                {
                    //比對狀態不同的回傳ID
                    if (item.Attribute("OldStatus").Value != item.Attribute("NewStatus").Value)
                    {
                        return item.Attribute("NewStatus").Value;
                    }
                }
                //都沒找到使用第一筆找到的
                return XD.Descendants("Change").FirstOrDefault().Attribute("NewStatus").Value;
            }
            catch (Exception)
            {
                return "";
            }

        }
    }

    public class BTIDebitData
    {
        public string reserve_id { get; set; }

        public decimal amount { get; set; }

        public string BetID { get; set; }

        public string BodyData { get; set; }
    }
}
