using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataSports_TFG
    {
    }

    public class ValidateReq
    {
        public string token { get; set; }
    }
    public class TFGWallet
    {
        public Decimal balance { get; set; }
    }
    public class ValidateResp
    {
        public string loginName { get; set; }
    }
    public class TFGTransferReq
    {
        public string loginName { get; set; }
        public decimal amount { get; set; }
        public bool placeBet { get; set; }
        public string ticketNum { get; set; }
        public string description { get; set; }
        public TicketDetail ticketDetail { get; set; }
    }
    public class TicketDetail
    {
        public string id { get; set; }
        public decimal odds { get; set; }
        public decimal amount { get; set; }
        public string result { get; set; }
        public string map_num { get; set; }
        public string currency { get; set; }
        public decimal? earnings { get; set; }
        public int event_id { get; set; }
        public string handicap { get; set; }
        public bool is_combo { get; set; }
        public decimal euro_odds { get; set; }
        public int market_id { get; set; }
        public string event_name { get; set; }
        public decimal? malay_odds { get; set; }
        public string member_code { get; set; }
        public decimal member_odds { get; set; }
        public string ticket_type { get; set; }
        private DateTimeOffset? _date_created { get; set; }
        public DateTimeOffset? date_created
        {
            get => _date_created;
            set
            {
                if (value != null)
                    _date_created = new DateTimeOffset(value.Value.ToUniversalTime().DateTime, TimeSpan.Zero);
            }
        }
        public int game_type_id { get; set; }
        public bool is_unsettled { get; set; }
        public string bet_selection { get; set; }
        public string bet_type_name { get; set; }
        public string market_option { get; set; }
        public string result_status { get; set; }
        private DateTimeOffset? _event_datetime { get; set; }
        public DateTimeOffset? event_datetime
        {
            get => _event_datetime;
            set
            {
                if (value != null)
                    _event_datetime = new DateTimeOffset(value.Value.ToUniversalTime().DateTime, TimeSpan.Zero);
            }
        }
        public string game_type_name { get; set; }
        public string request_source { get; set; }
        public string competition_name { get; set; }
        public string game_market_name { get; set; }
        public string member_odds_style { get; set; }
        private DateTimeOffset? _modified_datetime { get; set; }
        public DateTimeOffset? modified_datetime
        {
            get => _modified_datetime;
            set
            {
                if (value != null)
                    _modified_datetime = new DateTimeOffset(value.Value.ToUniversalTime().DateTime, TimeSpan.Zero);
            }
        }
        public string settlement_status { get; set; }
        private DateTimeOffset? _settlement_datetime { get; set; }
        public DateTimeOffset? settlement_datetime
        {
            get => _settlement_datetime;
            set
            {
                if (value != null)
                    _settlement_datetime = new DateTimeOffset(value.Value.ToUniversalTime().DateTime, TimeSpan.Zero);
            }
        }
        public IEnumerable<TicketDetail> tickets { get; set; }
    }
    public class TFGTransferResp
    {

    }

    public class PlaceBet_TFG
    {
        public string loginName { get; set; }
        public decimal amount { get; set; }
        public bool placeBet { get; set; }
        public string ticketNum { get; set; }
        public string description { get; set; }
        public string id { get; set; }
        public decimal odds { get; set; }
        public string result { get; set; }
        public string map_num { get; set; }
        public string currency { get; set; }
        public int event_id { get; set; }
        public string handicap { get; set; }
        public bool is_combo { get; set; }
        public decimal euro_odds { get; set; }
        public int market_id { get; set; }
        public string event_name { get; set; }
        public decimal? malay_odds { get; set; }
        public string member_code { get; set; }
        public decimal member_odds { get; set; }
        public string ticket_type { get; set; }
        public DateTime? date_created { get; set; }
        public int game_type_id { get; set; }
        public bool is_unsettled { get; set; }
        public string bet_selection { get; set; }
        public string bet_type_name { get; set; }
        public string market_option { get; set; }
        public string result_status { get; set; }
        public DateTime? event_datetime { get; set; }
        public string game_type_name { get; set; }
        public string request_source { get; set; }
        public string competition_name { get; set; }
        public string game_market_name { get; set; }
        public string member_odds_style { get; set; }
        public DateTime? modified_datetime { get; set; }
        public string settlement_status { get; set; }
        public DateTime? settlement_datetime { get; set; }
        public string tickets { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public GameType TpGameType { get; set; }
    }
    public class BetResult
    {
        public int result { get; set; }
    }
    public class SettleResult
    {
        public int result { get; set; }
    }
    public class Result
    {
        public ResultCode Code { get; set; }
    }
    public enum ResultCode
    {
        Success = 0,
        TransactionNotFound = 1,
        UnknownError = 2,
        DatabaseError = 3,
        TransactionDuplicated = 4,
        UserLockedOrNotFound = 5,
        CurrencyError = 6
    }
    public class Settle_TFG
    {
        public string loginName { get; set; }
        public decimal amount { get; set; }
        public bool placeBet { get; set; }
        public string ticketNum { get; set; }
        public string description { get; set; }
        public string id { get; set; }
        public decimal odds { get; set; }
        public string result { get; set; }
        public string map_num { get; set; }
        public string currency { get; set; }
        public decimal? earnings { get; set; }
        public int event_id { get; set; }
        public string handicap { get; set; }
        public bool is_combo { get; set; }
        public decimal euro_odds { get; set; }
        public int market_id { get; set; }
        public string event_name { get; set; }
        public decimal? malay_odds { get; set; }
        public string member_code { get; set; }
        public decimal member_odds { get; set; }
        public string ticket_type { get; set; }
        public DateTimeOffset? date_created { get; set; }
        public int game_type_id { get; set; }
        public bool is_unsettled { get; set; }
        public string bet_selection { get; set; }
        public string bet_type_name { get; set; }
        public string market_option { get; set; }
        public string result_status { get; set; }
        public DateTimeOffset? event_datetime { get; set; }
        public string game_type_name { get; set; }
        public string request_source { get; set; }
        public string competition_name { get; set; }
        public string game_market_name { get; set; }
        public string member_odds_style { get; set; }
        public DateTimeOffset? modified_datetime { get; set; }
        public string settlement_status { get; set; }
        public DateTimeOffset? settlement_datetime { get; set; }
        public string tickets { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public GameType TpGameType { get; set; }
    }
    public class Check_TFG
    {
        public long SQ { get; set; }
        public int TicketStatus { get; set; }
        public Decimal amount { get; set; }
    }
    public class TFGRallBackReq : TFGTransferReq
    {

    }
    public class TFGRallBackResp : TFGTransferResp
    {

    }
}
