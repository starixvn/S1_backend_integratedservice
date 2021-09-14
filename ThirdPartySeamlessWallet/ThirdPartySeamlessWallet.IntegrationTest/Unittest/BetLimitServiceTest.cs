using System.Collections.Generic;
using NUnit.Framework;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.IntegrationTest.Unittest
{
    [TestFixture]
    public class BetLimitServiceTest
    {
        [Test]
        public void Cust_Bet_10_And_In_Bet_Limit()
        {
            var expected = true;
            var betSetting = new CustBetLimit()
            {
                CustId = 250032,
                SiteId = 1,
                BetSettings = new List<BetSetting>()
                {
                    new BetSetting()
                    {
                        TpCategory = 3,
                        MinBet = 5,
                        MaxBet = 100
                    }
                }
            };
            var betAmount = 5.0m;
            var betLimitService = new BetLimitService();
            
            var actual = betLimitService.SetBetLimit(3, 4, betSetting).IsInBetLimit(betAmount);
            
            Assert.AreEqual(expected, actual);
        }
        
        [Test]
        public void DG_LiveCasino_Bet_10_And_In_Bet_Limit()
        {
            var expected = false;
            var betSetting = new CustBetLimit()
            {
                CustId = 250032,
                SiteId = 1,
                BetSettings = new List<BetSetting>()
                {
                    new BetSetting()
                    {
                        TpCategory = 3,
                        MinBet = 5,
                        MaxBet = 100,
                        TpBetLimits = new List<TpBetLimit>()
                        {
                            new TpBetLimit()
                            {
                                TpId = 4,
                                MinBet = 20,
                                MaxBet = 50
                            }
                        }
                    }
                }
            };
            var betLimitService = new BetLimitService();
            
            var actual = betLimitService.SetBetLimit(3, 4, betSetting).IsInBetLimit(60);
            
            Assert.AreEqual(expected, actual);
        }
    }
}