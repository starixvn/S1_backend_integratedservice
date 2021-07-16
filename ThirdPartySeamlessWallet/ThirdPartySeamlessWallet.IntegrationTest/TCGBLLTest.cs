using NUnit.Framework;
using System;
using ThirdPartySeamlessWallet.IntegrationTest.Models;
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
            .ResultShouldBe(TCGReturnCode.Success);

        [TestCase(10)]
        [TestCase(20)]
        public void Debit(decimal amount) 
            => new TCGBLLSandbox(_merchantCode, _tpUserName, _transId)
            .Debit(_transId, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .ShouldBeHaveRecord(TCGBLLSandbox.TCGRecord(TCGTransType.Debit, _tpUserName, _transId, amount))
            .CheckTransactionStatus(_transId, amount)
            .StatusShouldBe(TransStatus.Success);
        
        [TestCase(10)]
        [TestCase(20)]
        public void Debit_Resend(decimal amount)
            => new TCGBLLSandbox(_merchantCode, _tpUserName, _transId)
            .Debit(_transId, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .Debit(_transId, amount)
            .ResultShouldBe(TCGReturnCode.TransactionAlreadyExists)
            .ShouldBeHaveRecord(TCGBLLSandbox.TCGRecord(TCGTransType.Debit, _tpUserName, _transId, amount))
            .CheckTransactionStatus(_transId, amount)
            .StatusShouldBe(TransStatus.Success);

        [TestCase(10)]
        [TestCase(20)]
        public void Credit(decimal amount)
            => new TCGBLLSandbox(_merchantCode, _tpUserName, _transId)
            .Credit(_transId, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .ShouldBeHaveRecord(TCGBLLSandbox.TCGRecord(TCGTransType.Credit, _tpUserName, _transId, amount))
            .CheckTransactionStatus(_transId, amount)
            .StatusShouldBe(TransStatus.Success);

        [TestCase(10)]
        [TestCase(20)]
        public void Credit_Resend(decimal amount)
            => new TCGBLLSandbox(_merchantCode, _tpUserName, _transId)
            .Credit(_transId, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .Credit(_transId, amount)
            .ResultShouldBe(TCGReturnCode.TransactionAlreadyExists)
            .ShouldBeHaveRecord(TCGBLLSandbox.TCGRecord(TCGTransType.Credit, _tpUserName, _transId, amount))
            .CheckTransactionStatus(_transId, amount)
            .StatusShouldBe(TransStatus.Success);

        [TestCase(10)]
        [TestCase(20)]
        public void Reserve(decimal amount)
            => new TCGBLLSandbox(_merchantCode, _tpUserName, _transId)
            .Reserve(_transId, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .ShouldBeHaveRecord(TCGBLLSandbox.TCGRecord(TCGTransType.Reserve, _tpUserName, _transId, amount))
            .CheckTransactionStatus(_transId, amount)
            .StatusShouldBe(TransStatus.Reserved);

        [TestCase(10)]
        [TestCase(20)]
        public void Reserve_Resend(decimal amount)
            => new TCGBLLSandbox(_merchantCode, _tpUserName, _transId)
            .Reserve(_transId, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .Reserve(_transId, amount)
            .ResultShouldBe(TCGReturnCode.TransactionAlreadyExists)
            .ShouldBeHaveRecord(TCGBLLSandbox.TCGRecord(TCGTransType.Reserve, _tpUserName, _transId, amount))
            .CheckTransactionStatus(_transId, amount)
            .StatusShouldBe(TransStatus.Reserved);

        [TestCase(10)]
        [TestCase(20)]
        public void Reserve_Then_CheckBalance(decimal amount)
        {
            var sandbox = new TCGBLLSandbox(_merchantCode, _tpUserName, _transId);
            var firstBalance = sandbox.GetBalance().ResultShouldBe(TCGReturnCode.Success).Balance;

            //Reserve Success
            sandbox.Reserve(_transId, amount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGBLLSandbox.TCGRecord(TCGTransType.Reserve, _tpUserName, _transId, amount));

            //afterBalance 
            sandbox.GetBalance().BalanceShouldBe(Convert.ToDecimal(firstBalance.balance) - amount);
        }

        [TestCase(10)]
        [TestCase(20)]
        public void Reserve_Then_Confirm(decimal amount)
            => new TCGBLLSandbox(_merchantCode, _tpUserName, _transId)
            .Reserve(_transId, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .ConfirmReserve(_transId, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .ShouldBeHaveRecord(TCGBLLSandbox.TCGRecord(TCGTransType.Reserve, _tpUserName, _transId, amount))
            .CheckTransactionStatus(_transId, amount)
            .StatusShouldBe(TransStatus.Success);

        [TestCase(10)]
        [TestCase(20)]
        public void Reserve_Then_Cancel_Then_Check_Balance(decimal amount)
        {
            var sandbox = new TCGBLLSandbox(_merchantCode, _tpUserName, _transId);
            var firstBalance = sandbox.GetBalance().ResultShouldBe(TCGReturnCode.Success).Balance;

            sandbox.Reserve(_transId, amount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGBLLSandbox.TCGRecord(TCGTransType.Reserve, _tpUserName, _transId, amount))
                .GetBalance().BalanceShouldBe(Convert.ToDecimal(firstBalance.balance) - amount);
            
            sandbox.CancelReserve(_transId, amount)
                .ResultShouldBe(TCGReturnCode.Success)
                .CheckTransactionStatus(_transId, amount)
                .StatusShouldBe(TransStatus.Canceled)
                .GetBalance().BalanceShouldBe(Convert.ToDecimal(firstBalance.balance));
        }

        [TestCase(10)]
        [TestCase(20)]
        public void Debit_Then_CheckBalance(decimal amount)
        {
            var sandbox = new TCGBLLSandbox(_merchantCode, _tpUserName, _transId);
            var firstBalance = sandbox.GetBalance().ResultShouldBe(TCGReturnCode.Success).Balance;

            //Debit Success
            sandbox.Debit(_transId, amount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGBLLSandbox.TCGRecord(TCGTransType.Debit, _tpUserName, _transId, amount)); 

            //afterBalance 
            sandbox.GetBalance().BalanceShouldBe(Convert.ToDecimal(firstBalance.balance) - amount);           
        }

        [TestCase(10)]
        [TestCase(20)]
        public void Debit_Then_Credit_Then_CheckBalance(decimal amount)
        {
            var debitSandbox = new TCGBLLSandbox(_merchantCode, _tpUserName, _transId);
            var firstBalance = debitSandbox.GetBalance().ResultShouldBe(TCGReturnCode.Success).Balance;

            //Debit Success
            debitSandbox.Debit(_transId, amount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGBLLSandbox.TCGRecord(TCGTransType.Debit, _tpUserName, _transId, amount));

            //Before Balance 
            debitSandbox.GetBalance().BalanceShouldBe(Convert.ToDecimal(firstBalance.balance) - amount);

            _transId = _transId.Replace("test", "ctest");
            var creditSandbox = new TCGBLLSandbox(_merchantCode, _tpUserName, _transId);

            //Credit Success
            creditSandbox.Credit(_transId, amount * 2)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGBLLSandbox.TCGRecord(TCGTransType.Credit, _tpUserName, _transId, amount * 2));

            //afterCreditBalance
            creditSandbox.GetBalance().BalanceShouldBe(Convert.ToDecimal(firstBalance.balance) + amount);
        }

        [TestCase(10)]
        [TestCase(20)]
        public void CheckStatus_NotFound(decimal amount)
            => new TCGBLLSandbox(_merchantCode, _tpUserName, _transId)
            .Debit(_transId, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .ShouldBeHaveRecord(TCGBLLSandbox.TCGRecord(TCGTransType.Debit, _tpUserName, _transId, amount))
            .CheckTransactionStatus(_transId, amount)
            .StatusShouldBe(TransStatus.Success)
            .CheckTransactionStatus("xxx", amount)
            .StatusShouldBe(TransStatus.NotFound);

    }
}