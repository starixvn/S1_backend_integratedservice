using System;
using System.Collections.Generic;

namespace ThirdPartyWager.Service
{
    public class WagerOne789Model
    {

    }

    public class BetOne789
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Term { get; set; }
        public int GameType { get; set; }
        public int TicketNumber { get; set; }
        public dynamic Numbers { get; set; }
        public int AreaNumber { get; set; }
        public string GameNumber { get; set; }
        public int GameRound { get; set; }
        public decimal Commission { get; set; }
        public int BetType { get; set; }
        public decimal Price { get; set; }
        public decimal Point { get; set; }
        public decimal Amount { get; set; }
        public decimal NetAmount { get; set; }
        public int? GameResult { get; set; }
        public decimal Payouts { get; set; }
        public decimal WinLose { get; set; }
        public decimal Result { get; set; }
        public string IP { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int Status { get; set; }
        public string PayoutTx { get; set; }
        public string PayoutCancelTx { get; set; }
        public string TransId { get; set; }
        public long createUnix
        {
            get
            {
                return new DateTimeOffset(CreatedAt).ToUnixTimeSeconds();
            }
        }
        public long UpdateUnix
        {
            get
            {
                return new DateTimeOffset(UpdatedAt).ToUnixTimeSeconds();
            }
        }
    }

    public class BettransDataOne789
    {
        public string Username { get; set; }
        public string Tx { get; set; }
        public decimal Amount { get; set; }
        public long CreatedAt { get; set; }
        public string Source { get; set; }
        public string Type { get; set; }
        public string RefSource { get; set; }
    }

    public class TicketDataOne789
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public IList<string> Result { get; set; }

    }

    public class SeamlessOne789Response
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string Tx { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
    }
}
