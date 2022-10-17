using System;
using NUnit.Framework;
using ThirdPartyWager.Service.Library;

namespace ThirdPartyWager.IntegrationTest.Unittest
{
    [TestFixture]
    public class DateTimeHelperTests
    {
        [TestCase("2022-06-09T19:45:45")]
        public void SetCurrentTimeAndGetStartEndTime(DateTime dateTime)
        {
            var range = 15;
            var timezone = -4;
            var formate = "s";

            var helper = new DateTimeHelper();
            var actual = helper.GetTimeByMinuteRevert(range, timezone, formate, dateTime);

            var startTime = "2022-06-09T07:30:00";
            var endTime = "2022-06-09T07:45:00";
            
            Assert.AreEqual(startTime, actual.startTime);
            Assert.AreEqual(endTime, actual.endTime);
        }
    }
}