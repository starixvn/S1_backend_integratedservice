using NSubstitute;

using NUnit.Framework;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.IntegrationTest.Unittest
{
    [TestFixture]
    public class ReportTimeTests
    {
        [Test]
        public void GetReportTimeTest()
        {
            var expectStart = 1686499200;
            var expectEnd = 1686585599;

            var _dateTimeHelper = new DateTimeHelper();
            var thirdPartyBll = Substitute.For<IThirdPartyBLL>();
            thirdPartyBll.GetThirdPartyTimer(Arg.Any<int>(), Arg.Any<int>()).Returns(x
                => new ThirdPartyTimerModel { TpId = 50, Type = 4, StartTime = new DateTime(2023, 6, 12, 23, 59, 59) });

            var execTime = thirdPartyBll.GetThirdPartyTimer(50, 4);
            execTime.StartTime.AddSeconds(1);
            var reportTime = ReportTime.ConvertTo(_dateTimeHelper, execTime.StartTime, 8, 0);

            Assert.AreEqual(expectStart, reportTime.startUnix);
            Assert.AreEqual(expectEnd, reportTime.endUnix);
        }
    }
}