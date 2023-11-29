using NUnit.Framework;

using ThirdPartySeamlessWallet.IntegrationTest.SandBox;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.IntegrationTest
{
    public class SettleTicketTest
    {
        private TpCustInfo _tpCustInfo;
        [SetUp]
        public void Setup()
        {
            _tpCustInfo = new TpCustInfo()
            {
                CustId = 249951,
                SiteId = 1,
                UserName = "hanstest01",
                CurrencyId = 2,
                TpUserName = "XNova88249951",
                TpPwd = "h5N3Bjn",
                TpCurrencyId = "2",
            };
        }

        [Test]
        public void SetDataTest()
        {          
            var betRecord = new BetRecordInfo();
            var SettleTicket = new SettleTicketSandbox().CreateSettleTicket(_tpCustInfo, betRecord, null);
            SettleTicket.SetData();
        }

   
    }
}