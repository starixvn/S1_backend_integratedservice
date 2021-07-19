using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.IntegrationTest.Models;
using ThirdPartySeamlessWallet.IntegrationTest.SandBox;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.IntegrationTest
{
    public class TCGTest
    {
        private Dictionary<string, string> _inputs;
        [SetUp]
        public void Setup()
        {
            var now = DateTime.Now;
            _inputs = new Dictionary<string, string>
            {
                { "MerchantCode", "xpnovamyr" },
                { "TpUserName", "xnova88x249951" },
                { "DebitID", $"debit{now.ToFileTime()}" },//下注交易號
                { "CreditID", $"credit{now.ToFileTime()}" },//派彩交易號
                { "ReserveID", $"reserve{now.ToFileTime()}" },//圈存交易號
                { "OrderNo", $"order{now.ToFileTime()}" },//注單號
                { "WagerID", $"wager{now.ToFileTime()}" }//注單紀錄號
            };
        }

        [Test]
        public void GetBalance()
            => new TCGBLLSandbox(_inputs)
            .GetBalance()
            .ResultShouldBe(TCGReturnCode.Success);

        [TestCase(10)]
        [TestCase(20)]
        public void Debit(decimal amount)
            => new TCGBLLSandbox(_inputs)
            .Debit(_inputs, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .ShouldBeHaveRecord(TCGTransType.Debit, _inputs, amount)
            .CheckTransactionStatus(_inputs["DebitID"], amount)
            .StatusShouldBe(TransStatus.Success);

        [TestCase(10)]
        [TestCase(20)]
        public void Debit_Resend(decimal amount)
            => new TCGBLLSandbox(_inputs)
            .Debit(_inputs, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .Debit(_inputs, amount)
            .ResultShouldBe(TCGReturnCode.TransactionAlreadyExists)
            .ShouldBeHaveRecord(TCGTransType.Debit, _inputs, amount)
            .CheckTransactionStatus(_inputs["DebitID"], amount)
            .StatusShouldBe(TransStatus.Success);

        [TestCase(10)]
        [TestCase(20)]
        public void Credit(decimal amount)
            => new TCGBLLSandbox(_inputs)
            .Credit(_inputs, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .ShouldBeHaveRecord(TCGTransType.Credit, _inputs, amount)
            .CheckTransactionStatus(_inputs["CreditID"], amount)
            .StatusShouldBe(TransStatus.Success);

        [TestCase(10)]
        [TestCase(20)]
        public void Credit_Resend(decimal amount)
            => new TCGBLLSandbox(_inputs)
            .Credit(_inputs, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .Credit(_inputs, amount)
            .ResultShouldBe(TCGReturnCode.TransactionAlreadyExists)
            .ShouldBeHaveRecord(TCGTransType.Credit, _inputs, amount)
            .CheckTransactionStatus(_inputs["CreditID"], amount)
            .StatusShouldBe(TransStatus.Success);

        [TestCase(10)]
        [TestCase(20)]
        public void Reserve(decimal amount)
            => new TCGBLLSandbox(_inputs)
            .Reserve(_inputs, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .ShouldBeHaveRecord(TCGTransType.Reserve, _inputs, amount)
            .CheckTransactionStatus(_inputs["ReserveID"], amount)
            .StatusShouldBe(TransStatus.Reserved);

        [TestCase(10)]
        [TestCase(20)]
        public void Reserve_Resend(decimal amount)
            => new TCGBLLSandbox(_inputs)
            .Reserve(_inputs, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .Reserve(_inputs, amount)
            .ResultShouldBe(TCGReturnCode.TransactionAlreadyExists)
            .ShouldBeHaveRecord(TCGTransType.Reserve, _inputs, amount)
            .CheckTransactionStatus(_inputs["ReserveID"], amount)
            .StatusShouldBe(TransStatus.Reserved);

        [TestCase(10)]
        [TestCase(20)]
        public void Reserve_Then_CheckBalance(decimal amount)
        {
            var sandbox = new TCGBLLSandbox(_inputs);
            var firstBalance = sandbox.GetBalance().ResultShouldBe(TCGReturnCode.Success).Balance;

            //Reserve Success
            sandbox.Reserve(_inputs, amount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGTransType.Reserve, _inputs, amount);

            //afterBalance 
            sandbox.GetBalance().BalanceShouldBe(Convert.ToDecimal(firstBalance.balance) - amount);
        }

        [TestCase(10)]
        [TestCase(20)]
        public void Reserve_Then_Confirm(decimal amount)
            => new TCGBLLSandbox(_inputs)
            .Reserve(_inputs, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .ConfirmReserve(_inputs, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .ShouldBeHaveRecord(TCGTransType.Reserve, _inputs, amount)
            .CheckTransactionStatus(_inputs["ReserveID"], amount)
            .StatusShouldBe(TransStatus.Success);

        [TestCase(10)]
        [TestCase(20)]
        public void Reserve_Then_Cancel_Then_Check_Balance(decimal amount)
        {
            var sandbox = new TCGBLLSandbox(_inputs);
            var firstBalance = sandbox.GetBalance().ResultShouldBe(TCGReturnCode.Success).Balance;

            sandbox.Reserve(_inputs, amount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGTransType.Reserve, _inputs, amount)
                .GetBalance().BalanceShouldBe(Convert.ToDecimal(firstBalance.balance) - amount);

            sandbox.CancelReserve(_inputs, amount)
                .ResultShouldBe(TCGReturnCode.Success)
                .CheckTransactionStatus(_inputs["ReserveID"], amount)
                .StatusShouldBe(TransStatus.Canceled)
                .GetBalance().BalanceShouldBe(Convert.ToDecimal(firstBalance.balance));
        }

        [TestCase(10)]
        [TestCase(20)]
        public void Debit_Then_CheckBalance(decimal amount)
        {
            var sandbox = new TCGBLLSandbox(_inputs);
            var firstBalance = sandbox.GetBalance().ResultShouldBe(TCGReturnCode.Success).Balance;

            //Debit Success
            sandbox.Debit(_inputs, amount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGTransType.Debit, _inputs, amount);

            //afterBalance 
            sandbox.GetBalance().BalanceShouldBe(Convert.ToDecimal(firstBalance.balance) - amount);
        }

        [TestCase(10)]
        [TestCase(20)]
        public void Debit_Then_Credit_Then_CheckBalance(decimal amount)
        {
            var sandbox = new TCGBLLSandbox(_inputs);
            var firstBalance = sandbox.GetBalance().ResultShouldBe(TCGReturnCode.Success).Balance;

            sandbox.Debit(_inputs, amount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGTransType.Debit, _inputs, amount)
                .GetBalance()
                .BalanceShouldBe(Convert.ToDecimal(firstBalance.balance) - amount)//Debit Balance 
                .Credit(_inputs, amount * 2)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGTransType.Credit, _inputs, amount * 2)
                .GetBalance()
                .BalanceShouldBe(Convert.ToDecimal(firstBalance.balance) + amount); //Credit Balance
        }

        [TestCase(10)]
        [TestCase(20)]
        public void CheckStatus_NotFound(decimal amount)
            => new TCGBLLSandbox(_inputs)
            .Debit(_inputs, amount)
            .ResultShouldBe(TCGReturnCode.Success)
            .ShouldBeHaveRecord(TCGTransType.Debit, _inputs, amount)
            .CheckTransactionStatus(_inputs["DebitID"], amount)
            .StatusShouldBe(TransStatus.Success)
            .CheckTransactionStatus("xxx", amount)
            .StatusShouldBe(TransStatus.NotFound);


        [TestCase(10, 30, ExpectedResult = 20)]
        [TestCase(20, 0, ExpectedResult = -20)]
        public decimal Debit_Then_Credit_Then_InsertWager_Then_Settle_Then_CheckAll(decimal debitAmount, decimal creditAmount)
        {
            //Debit and Credit
            new TCGBLLSandbox(_inputs)
                .Debit(_inputs, debitAmount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGTransType.Debit, _inputs, debitAmount)
                .Credit(_inputs, creditAmount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGTransType.Credit, _inputs, creditAmount);

            //InsertWager and Settle 
            var settle = new SettleWorkerSandbox()
                .InsertWager("Service_TpWagerTCG_Ins", new TCGWagerData()
                {
                    betOrderNo = _inputs["WagerID"],
                    betAmount = debitAmount,
                    actualBetAmount = debitAmount,
                    username = _inputs["TpUserName"],
                    netPNL = creditAmount - debitAmount,//輸贏金額
                    winAmount = creditAmount,//派彩金額
                    settlementTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:sss"),
                    productType = "TCG_SEA",
                    orderNum = _inputs["OrderNo"],
                    details = "testing",
                })
                .ShouldBeHaveUnsettleRecord(SettleFlag.EndGame, _inputs["WagerID"])
                .DoSettleTCGWork();

            Task.Delay(1000).ConfigureAwait(false).GetAwaiter().GetResult();

            //Check SettleFlag and TpBetRecord
            settle.SettleFlagShouldBeSettled(@$"SELECT SettleFlag FROM TpWagerTCG WHERE betOrderNo='{_inputs["WagerID"]}'")
            .SettleFlagShouldBeSettled(@$"SELECT SettleFlag FROM BettransLottery_TCG WHERE ref_no='{_inputs["DebitID"]}'")
            .SettleFlagShouldBeSettled(@$"SELECT SettleFlag FROM BettransLottery_TCG WHERE ref_no='{_inputs["CreditID"]}'")
            .ShouldBeHaveTpBetRecord(Tp.TCG, _inputs["WagerID"], _inputs["TpUserName"], debitAmount, creditAmount - debitAmount);

            return settle.ActualWinlostAmount;
        }


    }
}