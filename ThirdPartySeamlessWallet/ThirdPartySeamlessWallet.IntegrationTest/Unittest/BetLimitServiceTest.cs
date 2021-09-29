using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.DAL.Interface;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.IntegrationTest.Unittest
{
    [TestFixture]
    public class BetLimitServiceTest
    {
        [Test]
        public void Cust_Bet_10_And_In_Bet_Limit()
        {
            var expected = false;
            var betAmount = 5.0m;
            var custBetLimitSvc =  Substitute.For<ICustBetLimitService>();
            custBetLimitSvc.GetCustBetLimit(Arg.Any<int>()).Returns(new CustBetLimit());
            var betLimitService = new BetLimitService(custBetLimitSvc);
            
            var actual = betLimitService.IsOutOfLimit(250032, 49, 4, betAmount);
            
            Assert.AreEqual(expected, actual);
        }
        
        [Test]
        public void DG_LiveCasino_Bet_10_And_In_Bet_Limit()
        {
            var expected = false;
            var custBetLimitSvc =  Substitute.For<ICustBetLimitService>();
            custBetLimitSvc.GetCustBetLimit(Arg.Any<int>()).Returns(new CustBetLimit()
            {
                CustId = 250140,
                SiteId = 1
            });
            var betLimitService = new BetLimitService(custBetLimitSvc);
            
            var actual = betLimitService.IsOutOfLimit(250140,3, 4, 60);
            
            Assert.AreEqual(expected, actual);
        }
    }
}