using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataCF_AWC
    {
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string currency { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public string betType { get; set; }
        public decimal betAmount { get; set; }
        public DateTime betTime { get; set; }
        public string roundId { get; set; }
        public GameInfo gameInfo { get; set; }
    }

    public class GameInfo
    {

    }

    public class CancelBetDataCF_AWC
    {
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameCode { get; set; }
        public string gameType { get; set; }
        public string roundId { get; set; }
        public GameInfo gameInfo { get; set; }
    }

    public class AdjustBetDataCF_AWC
    {
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        //public string betType { get; set; }
        public decimal betAmount { get; set; }
        public decimal adjustAmount { get; set; }
        public DateTime updateTime { get; set; }
        public GameInfo gameInfo { get; set; }
    }

    public class VoidBetDataCF_AWC
    {
        public long txId { get; set; }
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public decimal betAmount { get; set; }
        public DateTime updateTime { get; set; }
        public string roundId { get; set; }
        public GameInfo gameInfo { get; set; }
    }

    public class UnvoidBetDataCF_AWC
    {
        public long txId { get; set; }
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public DateTime updateTime { get; set; }
        public string roundId { get; set; }
    }

    public class SettleDataCF_AWC
    {
        public long txId { get; set; }
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public string betType { get; set; }
        public decimal betAmount { get; set; }
        public decimal winAmount { get; set; }
        public decimal turnover { get; set; }
        public DateTime betTime { get; set; }
        public DateTime updateTime { get; set; }
        public DateTime txTime { get; set; }
        public string roundId { get; set; }
        public GameInfo gameInfo { get; set; }
    }

    public class UnsettleDataCF_AWC
    {
        public long txId { get; set; }
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public decimal betAmount { get; set; }
        public DateTime updateTime { get; set; }
        public string roundId { get; set; }
        public GameInfo gameInfo { get; set; }
    }

    public class VoidSettleDataCF_AWC
    {
        public long txId { get; set; }
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public decimal betAmount { get; set; }
        public DateTime updateTime { get; set; }
        public int voidType { get; set; }
    }

    public class UnvoidSettleDataCF_AWC
    {
        public long txId { get; set; }
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public DateTime updateTime { get; set; }
        public int voidType { get; set; }
    }

    public class RefundDataCF_AWC
    {
        public long txId { get; set; }
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public string betType { get; set; }
        public decimal betAmount { get; set; }
        public decimal winAmount { get; set; }
        public decimal turnover { get; set; }
        public DateTime betTime { get; set; }
        public DateTime updateTime { get; set; }
        public string roundId { get; set; }
        public string refundPlatformTxId { get; set; }
        public GameInfo gameInfo { get; set; }
    }


}
