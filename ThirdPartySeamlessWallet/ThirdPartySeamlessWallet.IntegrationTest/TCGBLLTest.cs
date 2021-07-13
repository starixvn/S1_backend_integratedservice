using FluentAssertions;
using NUnit.Framework;
using System;
using ThirdPartySeamlessWallet.IntegrationTest.SandBox;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.IntegrationTest
{
    public class TCGBLLTest
    {
        private string _merchantCode;
        private string _tpUserName;
        private string _transId;

        [SetUp]
        public void Setup()
        {
            _merchantCode = "xpnovamyr";
            _tpUserName = "xnova88x249951";
            _transId = $"test{DateTime.Now.ToFileTime()}";         
        }

        [Test]
        public void GetBalance() 
            => new TCGBLLSandbox(_merchantCode, _tpUserName, _transId)
            .GetBalance()
            .CheckResult(TCGReturnCode.Success);

        [TestCase(10)]
        public void Debit(decimal amount) 
            => new TCGBLLSandbox(_merchantCode, _tpUserName, _transId)
            .CreateDebit(_transId, amount)
            .CheckResult(TCGReturnCode.Success);
        
        [TestCase(10)]
        public void Debit_Resend(decimal amount)
            => new TCGBLLSandbox(_merchantCode, _tpUserName, _transId)
            .CreateDebit(_transId, amount)
            .CheckResult(TCGReturnCode.Success)
            .CreateDebit(_transId, amount)
            .CheckResult(TCGReturnCode.TransactionAlreadyExists);

        [TestCase(10)]
        public void Credit(decimal amount)
            => new TCGBLLSandbox(_merchantCode, _tpUserName, _transId)
            .CreateCredit(_transId, amount)
            .CheckResult(TCGReturnCode.Success);

        [TestCase(10)]
        public void Credit_Resend(decimal amount)
            => new TCGBLLSandbox(_merchantCode, _tpUserName, _transId)
            .CreateCredit(_transId, amount)
            .CheckResult(TCGReturnCode.Success)
            .CreateCredit(_transId, amount)
            .CheckResult(TCGReturnCode.TransactionAlreadyExists);

        [TestCase(10)]
        public void Reserve(decimal amount)
            => new TCGBLLSandbox(_merchantCode, _tpUserName, _transId)
            .CreateReserve(_transId, amount)
            .CheckResult(TCGReturnCode.Success);

        [TestCase(10)]
        public void Reserve_Resend(decimal amount)
            => new TCGBLLSandbox(_merchantCode, _tpUserName, _transId)
            .CreateReserve(_transId, amount)
            .CheckResult(TCGReturnCode.Success)
            .CreateReserve(_transId, amount)
            .CheckResult(TCGReturnCode.TransactionAlreadyExists);

        [TestCase(10)]
        public void Debit_Then_CheckBalance(decimal amount)
        {
            var sandbox = new TCGBLLSandbox(_merchantCode, _tpUserName, _transId);
            var firstBalance = sandbox.GetBalance().CheckResult(TCGReturnCode.Success).Balance;

            //Debit Success
            sandbox.CreateDebit(_transId, amount).CheckResult(TCGReturnCode.Success);

            var afterBalance = sandbox.GetBalance().CheckResult(TCGReturnCode.Success).Balance;
            afterBalance.balance.Should().Be((Convert.ToDecimal(firstBalance.balance) - amount).ToString());
        }

        [TestCase(10)]
        public void Debit_Then_Credit_Then_CheckBalance(decimal amount)
        {
            var debitSandbox = new TCGBLLSandbox(_merchantCode, _tpUserName, _transId);
            var firstBalance = debitSandbox.GetBalance().CheckResult(TCGReturnCode.Success).Balance;

            //Debit Success
            debitSandbox.CreateDebit(_transId, amount).CheckResult(TCGReturnCode.Success);

            var afterDebitBalance = debitSandbox.GetBalance().CheckResult(TCGReturnCode.Success).Balance;
            afterDebitBalance.balance.Should().Be((Convert.ToDecimal(firstBalance.balance) - amount).ToString());


            _transId = _transId.Replace("test", "ctest");
            var creditSandbox = new TCGBLLSandbox(_merchantCode, _tpUserName, _transId);

            //Credit Success
            creditSandbox.CreateCredit(_transId, amount * 2).CheckResult(TCGReturnCode.Success);
            var afterCreditBalance = creditSandbox.GetBalance().CheckResult(TCGReturnCode.Success).Balance;
            afterCreditBalance.balance.Should().Be((Convert.ToDecimal(firstBalance.balance) + amount).ToString());
        }
    }
}