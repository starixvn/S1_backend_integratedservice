using NUnit.Framework;

using ThirdPartySeamlessWallet.IntegrationTest.SandBox;

namespace ThirdPartySeamlessWallet.IntegrationTest
{
    public class BTITest
    {

        private string _reserve_id;

        [SetUp]
        public void Setup()
        {
            _reserve_id = DateTime.Now.ToFileTime().ToString();
        }

        [TestCase(10)]
        public void ReserveTest(decimal amount)
            => new BTIBLLSandbox(_reserve_id)
                .CreateReserve(_reserve_id, amount)
                .ShouldBeHaveRecord(BTIBLLSandbox.ReserveRecord(_reserve_id, amount));
    }
}