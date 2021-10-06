using System.Collections.Generic;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
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
        public void Normal_User_Play_Ag_LiveCasino()
        {
            var custId = 250032;
            var tpCategory = 3;
            var gameTypeId = 8;
            var betAmount = 5.0m;
            var expected = false;
            
            var custBetLimitSvc =  Substitute.For<ICustBetLimitService>();
            custBetLimitSvc.GetCustBetLimit(Arg.Any<int>()).ReturnsNull();
            var betLimitService = new RiskInspection(custBetLimitSvc);
            
            var actual = betLimitService.IsOutOfLimit(custId, tpCategory, gameTypeId, betAmount, out var minBet, out var maxBet);
            
            Assert.AreEqual(expected, actual);
        }
        
        [Test]
        public void LiveCasino_Risk_User_And_Play_Slot()
        {
            var custId = 250032;
            var tpCategory = 2;
            var gameTypeId = 7;
            var betAmount = 5.0m;
            var expected = false;
            
            var custBetLimitSvc =  Substitute.For<ICustBetLimitService>();
            custBetLimitSvc.GetCustBetLimit(Arg.Any<int>()).Returns(new CustBetLimit()
            {
                BetSettings = new List<BetSetting>()
                {
                    new BetSetting()
                    {
                        TpCategory = 3,
                        MinBet = 20,
                        MaxBet = 100,
                    }
                }
            });
            var betLimitService = new RiskInspection(custBetLimitSvc);
            
            var actual = betLimitService.IsOutOfLimit(custId,tpCategory, gameTypeId, betAmount, out var minBet, out var maxBet);
            
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void LiveCasino_Risk_User_And_Play_LivaCasino()
        {
            var custId = 250032;
            var tpCategory = 3;
            var gameTypeId = 8;
            var betAmount = 19.5m;
            var expected = true;
            
            var custBetLimitSvc =  Substitute.For<ICustBetLimitService>();
            custBetLimitSvc.GetCustBetLimit(Arg.Any<int>()).Returns(new CustBetLimit()
            {
                BetSettings = new List<BetSetting>()
                {
                    new BetSetting()
                    {
                        TpCategory = 3,
                        MinBet = 20,
                        MaxBet = 100,
                    }
                }
            });
            var betLimitService = new RiskInspection(custBetLimitSvc);
            
            var actual = betLimitService.IsOutOfLimit(custId,tpCategory, gameTypeId, betAmount, out var minBet, out var maxBet);
            
            Assert.AreEqual(expected, actual);
        }
        
        [Test]
        public void LiveCasino_Risk_User_And_Play_AG_LivaCasino()
        {
            var custId = 250032;
            var tpCategory = 3;
            var gameTypeId = 8;
            var betAmount = 25.5m;
            var expected = false;
            
            var custBetLimitSvc =  Substitute.For<ICustBetLimitService>();
            custBetLimitSvc.GetCustBetLimit(Arg.Any<int>()).Returns(new CustBetLimit()
            {
                BetSettings = new List<BetSetting>()
                {
                    new BetSetting()
                    {
                        TpCategory = 3,
                        MinBet = 20,
                        MaxBet = 100,
                        TpBetSetting = new List<TpBetSetting>()
                        {
                            new TpBetSetting()
                            {
                                GameTypeId = 10,
                                MinBet = 30,
                                MaxBet = 80,
                            }
                        }
                    }
                }
            });
            var betLimitService = new RiskInspection(custBetLimitSvc);
            
            var actual = betLimitService.IsOutOfLimit(custId,tpCategory, gameTypeId, betAmount, out var minBet, out var maxBet);
            
            Assert.AreEqual(expected, actual);
        }
    }
}