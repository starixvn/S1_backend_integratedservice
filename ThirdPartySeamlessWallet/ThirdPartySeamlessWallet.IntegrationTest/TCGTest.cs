using NUnit.Framework;

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
        public decimal SEA_GameFlow_Test(decimal reserveAmount, decimal creditAmount)
        {
            //Reserve and Credit 
            new TCGBLLSandbox(_inputs)
                .Reserve(_inputs, reserveAmount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGTransType.Reserve, _inputs, reserveAmount)
                .Credit(_inputs, creditAmount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGTransType.Credit, _inputs, creditAmount)
                .ConfirmReserve(_inputs, reserveAmount)
                .ResultShouldBe(TCGReturnCode.Success)
                .CheckTransactionStatus(_inputs["ReserveID"], reserveAmount)
                .StatusShouldBe(TransStatus.Success);

            //InsertWager and Settle 
            var settle = new SettleWorkerSandbox()
                .InsertWager("Service_TpWagerTCG_SEA_Ins", new TCGSEAWagerData()
                {
                    betOrderNo = _inputs["WagerID"],
                    betAmount = reserveAmount,
                    actualBetAmount = reserveAmount,
                    username = _inputs["TpUserName"],
                    netPNL = creditAmount - reserveAmount,//輸贏金額
                    winAmount = creditAmount,//派彩金額
                    settlementTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:sss"),
                    betTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:sss"),
                    productType = "TCG_SEA",
                    orderNum = _inputs["OrderNo"],
                    details = "testing",
                    betStatus = 1,
                })
                .ShouldBeHaveUnsettleRecord(SettleFlag.EndGame, _inputs["WagerID"])
                .DoSettleTCGWork(GameType.TCG_SEA_Lottery);

            Task.Delay(1000).ConfigureAwait(false).GetAwaiter().GetResult();

            //Check SettleFlag and TpBetRecord
            settle.SettleFlagShouldBeSettled(@$"SELECT SettleFlag FROM BetDetail.TpWagerTCG_SEA WHERE betOrderNo='{_inputs["WagerID"]}'")
            .SettleFlagShouldBeSettled(@$"SELECT SettleFlag FROM TSDB.BettransLottery_TCG WHERE ref_no='{_inputs["ReserveID"]}'")
            .SettleFlagShouldBeSettled(@$"SELECT SettleFlag FROM TSDB.BettransLottery_TCG WHERE ref_no='{_inputs["CreditID"]}'")
            .ShouldBeHaveTpBetRecord(Tp.TCG, _inputs["WagerID"], _inputs["TpUserName"], reserveAmount, creditAmount - reserveAmount);

            return settle.ActualWinlostAmount;
        }


        [TestCase(10, 30, ExpectedResult = 20)]
        [TestCase(20, 0, ExpectedResult = -20)]
        public decimal VN_GameFlow_Test(decimal reserveAmount, decimal creditAmount)
        {
            var exchangeRate = 1000;
            _inputs.Remove("TpUserName");
            _inputs.Add("TpUserName", "dgoal123x250024");

            //Reserve and Credit 
            new TCGBLLSandbox(_inputs)
                .Reserve(_inputs, reserveAmount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGTransType.Reserve, _inputs, reserveAmount * exchangeRate)
                .Credit(_inputs, creditAmount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGTransType.Credit, _inputs, creditAmount * exchangeRate)
                .ConfirmReserve(_inputs, reserveAmount)
                .ResultShouldBe(TCGReturnCode.Success)
                .CheckTransactionStatus(_inputs["ReserveID"], reserveAmount * exchangeRate)
                .StatusShouldBe(TransStatus.Success);

            //InsertWager and Settle 
            var settle = new SettleWorkerSandbox()
                .InsertWager("Service_TpWagerTCG_VN_Ins", new TCGVNWagerData()
                {
                    orderMasterId = _inputs["WagerID"],
                    betAmount = null,
                    playName = "test",
                    actualBetAmount = reserveAmount,
                    username = _inputs["TpUserName"],
                    winAmount = creditAmount,//派彩金額
                    drawTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:sss"),
                    bettingTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:sss"),
                    orderNum = _inputs["OrderNo"],
                    remark = "testing",
                    result = 1,
                })
                .ShouldBeHaveUnsettleRecord(SettleFlag.EndGame, _inputs["WagerID"])
                .DoSettleTCGWork(GameType.TCG_VN_Lottery);

            Task.Delay(1000).ConfigureAwait(false).GetAwaiter().GetResult();

            //Check SettleFlag and TpBetRecord
            settle.SettleFlagShouldBeSettled(@$"SELECT SettleFlag FROM BetDetail.TpWagerTCG_VN WHERE orderMasterId='{_inputs["WagerID"]}'")
            .SettleFlagShouldBeSettled(@$"SELECT SettleFlag FROM TSDB.BettransLottery_TCG WHERE ref_no='{_inputs["ReserveID"]}'")
            .SettleFlagShouldBeSettled(@$"SELECT SettleFlag FROM TSDB.BettransLottery_TCG WHERE ref_no='{_inputs["CreditID"]}'")
            .ShouldBeHaveTpBetRecord(Tp.TCG, _inputs["WagerID"], _inputs["TpUserName"], reserveAmount * exchangeRate, (creditAmount - reserveAmount) * exchangeRate);

            return settle.ActualWinlostAmount / exchangeRate;
        }

        [TestCase(10, ExpectedResult = 0)]
        [TestCase(20, ExpectedResult = 0)]
        public decimal SEA_UnsettleFlow_Test(decimal reserveAmount)
        {
            //Reserve and Credit 
            new TCGBLLSandbox(_inputs)
                .Reserve(_inputs, reserveAmount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGTransType.Reserve, _inputs, reserveAmount)
                .ConfirmReserve(_inputs, reserveAmount)
                .ResultShouldBe(TCGReturnCode.Success)
                .CheckTransactionStatus(_inputs["ReserveID"], reserveAmount)
                .StatusShouldBe(TransStatus.Success);

            //InsertWager and Settle 
            var settle = new SettleWorkerSandbox()
                .InsertWager("Service_TpWagerTCG_SEA_Ins", new TCGSEAWagerData()
                {
                    betOrderNo = _inputs["WagerID"],
                    betAmount = reserveAmount,
                    actualBetAmount = reserveAmount,
                    username = _inputs["TpUserName"],
                    netPNL = 0,//輸贏金額
                    winAmount = 0,//派彩金額
                    settlementTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:sss"),
                    betTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:sss"),
                    productType = "TCG_SEA",
                    orderNum = _inputs["OrderNo"],
                    details = "testing",
                    betStatus = 0,
                })
                .ShouldBeHaveUnsettleRecord(SettleFlag.EndGame, _inputs["WagerID"])
                .DoSettleTCGWork(GameType.TCG_SEA_Lottery);

            Task.Delay(1000).ConfigureAwait(false).GetAwaiter().GetResult();

            //Check SettleFlag and TpBetRecord
            settle.SettleFlagShouldBeEndGame(@$"SELECT SettleFlag FROM BetDetail.TpWagerTCG_SEA WHERE betOrderNo='{_inputs["WagerID"]}'")
            .ShouldBeHaveUnsettleTpBetRecord(Tp.TCG, _inputs["WagerID"], _inputs["TpUserName"], reserveAmount, 0);

            return settle.ActualWinlostAmount;
        }

        [TestCase(10, ExpectedResult = 0)]
        public decimal VN_UnSettleFlow_Test(decimal reserveAmount)
        {
            var exchangeRate = 1000;
            _inputs.Remove("TpUserName");
            _inputs.Add("TpUserName", "dgoal123x250024");

            //Reserve and Confirm 
            new TCGBLLSandbox(_inputs)
                .Reserve(_inputs, reserveAmount)
                .ResultShouldBe(TCGReturnCode.Success)
                .ShouldBeHaveRecord(TCGTransType.Reserve, _inputs, reserveAmount * exchangeRate)
                .ConfirmReserve(_inputs, reserveAmount)
                .ResultShouldBe(TCGReturnCode.Success)
                .CheckTransactionStatus(_inputs["ReserveID"], reserveAmount * exchangeRate)
                .StatusShouldBe(TransStatus.Success);

            //InsertWager and Settle 
            var settle = new SettleWorkerSandbox()
                .InsertWager("Service_TpWagerTCG_VN_Ins", new TCGVNWagerData()
                {
                    orderMasterId = _inputs["WagerID"],
                    betAmount = null,
                    playName = "test",
                    actualBetAmount = reserveAmount,
                    username = _inputs["TpUserName"],
                    winAmount = 0,//派彩金額
                    drawTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:sss"),
                    bettingTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:sss"),
                    orderNum = _inputs["OrderNo"],
                    remark = "testing",
                    result = 0,
                })
                .ShouldBeHaveUnsettleRecord(SettleFlag.EndGame, _inputs["WagerID"])
                .DoSettleTCGWork(GameType.TCG_VN_Lottery);

            Task.Delay(1000).ConfigureAwait(false).GetAwaiter().GetResult();

            //Check SettleFlag and TpBetRecord
            settle.SettleFlagShouldBeEndGame(@$"SELECT SettleFlag FROM BetDetail.TpWagerTCG_VN WHERE orderMasterId='{_inputs["WagerID"]}'")
            .ShouldBeHaveUnsettleTpBetRecord(Tp.TCG, _inputs["WagerID"], _inputs["TpUserName"], reserveAmount * exchangeRate, 0);

            return settle.ActualWinlostAmount / exchangeRate;
        }
    }
}