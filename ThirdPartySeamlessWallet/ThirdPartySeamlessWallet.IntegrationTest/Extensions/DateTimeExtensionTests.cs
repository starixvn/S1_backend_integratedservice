using NUnit.Framework;
using System;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ThirdPartySeamlessWallet.Service.Model;
using System.Linq;
using ThirdPartySeamlessWallet.Extensions;

namespace ThirdPartySeamlessWallet.IntegrationTest.Extensions
{
    [TestFixture()]
    public class DateTimeExtensionTests
    {
        [TestCase("2022-01-05 00:00", 3, ExpectedResult = 1)]
        [TestCase("2022-01-05 00:00", 5, ExpectedResult = 2)]
        [TestCase("2022-01-05 00:00", 7, ExpectedResult = 2)]
        public int GetPartitionRange_Test(DateTime partitionTime, int partitionCheckDay)
        {
            return partitionTime.GetPartitionRange(partitionCheckDay).Count();
        }

        [TestCase("2022-01-05 00:00", ExpectedResult = "202201")]
        [TestCase("2022-02-02 00:00", ExpectedResult = "202202")]
        public string GetPartitionKey_Test(DateTime partitionTime)
        {
            return partitionTime.GetPartitionKey();
        }


    }
}
