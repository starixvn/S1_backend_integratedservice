using NUnit.Framework;
using System;
using ThirdPartySeamlessWallet.IntegrationTest.Models;
using ThirdPartySeamlessWallet.IntegrationTest.SandBox;

namespace ThirdPartySeamlessWallet.IntegrationTest
{
    public class BtiServiceTest
    {

        private string _reserve_id;

        [SetUp]
        public void Setup()
        {
            _reserve_id = DateTime.Now.ToFileTime().ToString();
        }

        [TestCase(10)]
        public void ReserveTest(decimal amount)
            => new BtiServiceSandbox(_reserve_id)
                .CreateReserve(_reserve_id, amount)
                .ShouldBeHaveRecord(BtiServiceSandbox.ReserveRecord(_reserve_id, amount));
    }
}