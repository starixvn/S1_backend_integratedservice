using NUnit.Framework;

namespace ThirdPartySeamlessWallet.IntegrationTest;

[TestFixture]
public class MathTests
{
    [TestCase(18.53, 18.5)]
    [TestCase(1.77, 1.80)]
    [TestCase(0.63, 0.6)]
    [TestCase(0.001, 0)]
    [TestCase(1022932.1910, 1022932.20)]
    [TestCase(99943.4500, 99943.50000000)]
    public void RoundTest(decimal num, decimal expect)
    {
        var actual = Math.Round(num, 1, MidpointRounding.AwayFromZero);
        Assert.AreEqual(expect, actual);
    }
}