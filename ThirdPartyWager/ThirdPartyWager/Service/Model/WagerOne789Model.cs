using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public int GameResult { get; set; }
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

}
