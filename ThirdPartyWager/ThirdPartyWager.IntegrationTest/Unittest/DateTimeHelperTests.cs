using NUnit.Framework;

using ThirdPartyWager.Service.Library;

namespace ThirdPartyWager.IntegrationTest.Unittest
{
    [TestFixture]
    public class DateTimeHelperTests
    {
        [TestCase("2023-06-09T14:05:00")]
        public void SetCurrentTimeAndGetStartEndTime(DateTime dateTime)
        {
            var range = 15;
            var timezone = -4;
            var formate = "s";

            var helper = new DateTimeHelper();
            var actual = helper.GetTimeByMinuteRevert(range, timezone, formate, dateTime);

            var startTime = "2023-06-09T09:50:00";
            var endTime = "2023-06-09T10:05:00";

            Assert.AreEqual(startTime, actual.startTime);
            Assert.AreEqual(endTime, actual.endTime);
        }

        [TestCase("2023-06-09T13:55:37")]
        public void SetCurrentTimeAndGetStartEndTime2(DateTime dateTime)
        {
            var range = 15;
            var timezone = 8;
            var formate = "yyyyMMddTHHmmss";

            var helper = new DateTimeHelper();
            var actual = helper.GetTimeByMinuteRevert(range, timezone, formate, dateTime);

            var startTime = "20230609T214000";
            var endTime = "20230609T215500";

            Assert.AreEqual(startTime, actual.startTime);
            Assert.AreEqual(endTime, actual.endTime);
        }
    }
}